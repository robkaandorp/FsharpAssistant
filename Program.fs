open System
open System.Net.Http
open System.Net.Http.Json
open System.Net.WebSockets
open FSharp.Control.Websockets

open Json

// Home Assistant url
let hassUrl = Environment.GetEnvironmentVariable("HASS_URL")
// Home Assistant long lived access token environment var: ACCESS_TOKEN
let accessToken = Environment.GetEnvironmentVariable("ACCESS_TOKEN")

let httpClient = new HttpClient()
httpClient.BaseAddress <- Uri(hassUrl)

if not (httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", "Bearer " + accessToken)) then
    failwith "Missing ACCESS_TOKEN"

type ApiMessage = { message: string }

type State = 
    { entity_id: string
      last_changed: DateTime
      last_updated: DateTime
      state: string
      attributes: Map<string, Object> }

let connectApiAsync =
    async {
        let! result = httpClient.GetAsync("/api/") |> Async.AwaitTask
        result.EnsureSuccessStatusCode() |> ignore

        return! result.Content.ReadFromJsonAsync<ApiMessage>() |> Async.AwaitTask
    }

let getStatesAsync =
    async {
        let! result = httpClient.GetAsync("/api/states") |> Async.AwaitTask
        result.EnsureSuccessStatusCode() |> ignore

        return! result.Content.ReadFromJsonAsync<State[]>() |> Async.AwaitTask
    }

let message = connectApiAsync |> Async.RunSynchronously
printfn "Message: %s" message.message

let states = getStatesAsync |> Async.RunSynchronously

let recentlyUpdated state =
    state.last_updated > DateTime.Now.AddHours(-1)

for state in Array.filter recentlyUpdated states do
    printfn "States: %s: %s (changed %A) | # attributes = %d" state.entity_id state.state state.last_changed state.attributes.Count

    for attribute in state.attributes do
        printfn " - %A" attribute

let getWebSocketAsync =
    async {
        let websocket = new ClientWebSocket()
        
        let hassUri = UriBuilder(hassUrl)
        if hassUri.Scheme = "http" then hassUri.Scheme <- "ws" else hassUri.Scheme <- "wss"
        hassUri.Path <- hassUri.Path + "api/websocket"

        let! ct = Async.CancellationToken
        do! websocket.ConnectAsync(hassUri.Uri, ct) |> Async.AwaitTask
        
        return ThreadSafeWebSocket.createFromWebSocket websocket
    }

type ResponseMessage =
    { ``type``: string }

type AuthenticationResponseMessage =
    { ha_version: string
      access_token: string
      message: string }

type AuthenticationRequestMessage =
    { ``type``: string
      access_token: string }

type ErrorResponse =
    { code: string
      message: string }

type CommandResponseMessage =
    { id: int
      ``type``: string
      success: bool
      error: ErrorResponse }

type SubscribeEvents =
    { id: int
      ``type``: string
      event_type: string }

type EventData =
    { entity_id: string
      new_state: State
      old_state: State }

type Event =
    { data: EventData
      event_type: string
      time_fired: DateTime
      origin: string }

type EventMessage =
    { id: int
      event: Event }

type Message =
    | AuthRequired of AuthenticationResponseMessage
    | AuthOk of AuthenticationResponseMessage
    | AuthInvalid of AuthenticationResponseMessage
    | Result of CommandResponseMessage
    | Event of EventMessage
    | Other of string
    | Closed of string
    | Fail of Runtime.ExceptionServices.ExceptionDispatchInfo

let receiveMessageAsync websocket =
    async {
        let! result = websocket |> ThreadSafeWebSocket.receiveMessageAsUTF8

        return
            match result with
            | Ok (WebSocket.ReceiveUTF8Result.String text) ->
                let responseMessage = fromJson<ResponseMessage> text
                match responseMessage.``type`` with
                | "auth_required" -> AuthRequired (fromJson<AuthenticationResponseMessage> text)
                | "auth_ok" -> AuthOk (fromJson<AuthenticationResponseMessage> text)
                | "auth_invalid" -> AuthInvalid (fromJson<AuthenticationResponseMessage> text)
                | "result" -> Result (fromJson<CommandResponseMessage> text)
                | "event" -> Event (fromJson<EventMessage> text)
                | t -> Other t
            | Ok (WebSocket.ReceiveUTF8Result.Closed (status, reason)) -> 
                printfn "Socket closed %A - %s" status reason
                Closed reason
            | Error (ex) -> 
                printfn "Receiving threw an exception %A" ex.SourceException
                Fail ex
    }

let sendMessageAsync websocket message =
    async {
        let! result = message |> ThreadSafeWebSocket.sendMessageAsUTF8 websocket

        match result with
        | Ok _ -> ignore ()
        | Error (ex) -> printfn "Sending threw an exception %A" ex.SourceException
    }

let messageLoop (websocket: ThreadSafeWebSocket.ThreadSafeWebSocket) =
    let mutable msgCounter = 0
    let getMessageCounter () =
        msgCounter <- msgCounter + 1
        msgCounter
    let send obj = toJson obj |> sendMessageAsync websocket
        
    async {

        while websocket.State = WebSocketState.Open do
            let! msg = receiveMessageAsync websocket

            match msg with
            | AuthRequired response ->
                printfn "HA version: %s" response.ha_version
                printf "Authenticating... "
                do! { ``type`` = "auth"; access_token = accessToken } |> send
            | AuthOk response -> 
                printfn "success."

                do! { id = getMessageCounter(); ``type`` = "subscribe_events"; event_type = "state_changed" } 
                    |> send
            | AuthInvalid response -> printfn "failed: %s" response.message
            | Result response -> if not response.success then printfn "Request %d failed; %s - %s" response.id response.error.code response.error.message
            | Event response -> printfn "%s %s -> %s" response.event.data.entity_id response.event.data.old_state.state response.event.data.new_state.state
            | Other ``type`` -> printfn "Not implemented type %s" ``type``
            | Closed reason -> printfn "Socket closed %s" reason
            | Fail (ex) -> printfn "Receiving threw an exception %A" ex.SourceException

        printfn "Message loop exited."
    }

let websocket = getWebSocketAsync |> Async.RunSynchronously
messageLoop websocket |> Async.RunSynchronously
