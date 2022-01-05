module WsClient

open System
open System.Net.WebSockets
open FSharp.Control.Websockets

open HassConfiguration
open Model
open Json

type WsClient(configuration: Configuration) =
    let ws = new ClientWebSocket()
    let mutable websocket: ThreadSafeWebSocket.ThreadSafeWebSocket option = None

    member this.State with get (): WebSocketState =
        ws.State

    member this.connectAsync () =
        async {
            let hassUri = UriBuilder(configuration.HassUrl)
            if hassUri.Scheme = "http" then hassUri.Scheme <- "ws" else hassUri.Scheme <- "wss"
            hassUri.Path <- hassUri.Path + "api/websocket"

            let! ct = Async.CancellationToken
            do! ws.ConnectAsync(hassUri.Uri, ct) |> Async.AwaitTask
        
            websocket <- Some (ThreadSafeWebSocket.createFromWebSocket ws)
        }

    member this.receiveMessageAsync () =
        async {
            let (Some ws) = websocket
            let! result = ThreadSafeWebSocket.receiveMessageAsUTF8 ws

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

    member this.sendMessageAsync message =
        async {
            let (Some ws) = websocket
            let! result = message |> ThreadSafeWebSocket.sendMessageAsUTF8 ws

            match result with
            | Ok _ -> ignore ()
            | Error (ex) -> printfn "Sending threw an exception %A" ex.SourceException
        }