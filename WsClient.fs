module WsClient

open System
open System.Net.WebSockets
open FSharp.Control.Websockets

open HassConfiguration
open Model
open Json

type WsClient(configuration: Configuration) =
    let mutable websocket: ThreadSafeWebSocket.ThreadSafeWebSocket option = None

    member this.State with get (): WebSocketState =
        match websocket with
        | Some ws -> ws.State
        | None -> WebSocketState.None

    member this.connectAsync () =
        async {
            let hassUri = UriBuilder(configuration.HassUrl)
            if hassUri.Scheme = "http" then hassUri.Scheme <- "ws" else hassUri.Scheme <- "wss"
            hassUri.Path <- hassUri.Path + "api/websocket"

            let! ct = Async.CancellationToken
            let ws = new ClientWebSocket()
            do! ws.ConnectAsync(hassUri.Uri, ct) |> Async.AwaitTask
        
            websocket <- Some (ThreadSafeWebSocket.createFromWebSocket ws)
        }

    member this.receiveMessageAsync () =
        async {
            let! result = ThreadSafeWebSocket.receiveMessageAsUTF8 websocket.Value

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
                    websocket <- None
                    Closed reason
                | Error (ex) -> 
                    printfn "Receiving threw an exception %A" ex.SourceException
                    websocket <- None
                    Fail ex
        }

    member this.sendMessageAsync message =
        async {
            match websocket with
            | Some ws ->
                let! result = message |> ThreadSafeWebSocket.sendMessageAsUTF8 ws
                match result with
                | Ok _ -> ignore ()
                | Error (ex) -> printfn "Sending threw an exception %A" ex.SourceException

            | None ->
                printfn "Websocket not connected."
        }