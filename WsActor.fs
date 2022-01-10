module WsActor

open System.Net.WebSockets
open Akka.FSharp

open Json
open HassConfiguration
open ActorMessages
open WsClient

let spawnWsActor system (configuration: Configuration) protocolActorRef =
    let wsClient = WsClient configuration
    wsClient.connectAsync() |> Async.RunSynchronously

    let send obj = 
        toJson obj |> wsClient.sendMessageAsync

    let handleMessage (mailbox: Actor<'a>) msg =
        match msg with
        | WsActorMessages.Send msg -> send msg |> Async.RunSynchronously
        | WsActorMessages.Receive msg -> protocolActorRef <! ProtocolActor.Receive msg
        | WsActorMessages.Stop -> 
            logWarningf mailbox "Websocket receiver loop exited, state was %A. Stopping WsActor." wsClient.State
            mailbox.Context.Stop(mailbox.Self)

    let wsAref = spawn system "ws-actor" (actorOf2 handleMessage)

    let receiverLoop () =
        backgroundTask {
            while wsClient.State = WebSocketState.Open do
                let! msg = wsClient.receiveMessageAsync()
                wsAref <! WsActorMessages.Receive msg

            wsAref <! WsActorMessages.Stop
        }
    receiverLoop() |> ignore

    wsAref