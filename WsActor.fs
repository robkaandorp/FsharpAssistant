module WsActor

open System.Net.WebSockets
open Akka.FSharp

open Json
open HassConfiguration
open ActorMessages
open WsClient

let spawnWsActor system (configuration: Configuration) protocolActorRef =
    let wsClient = WsClient configuration

    let send obj = toJson obj |> wsClient.sendMessageAsync

    let receiverLoop self =
        backgroundTask {
            while true do
                try
                    do! wsClient.connectAsync()

                    while wsClient.State = WebSocketState.Open do
                        let! msg = wsClient.receiveMessageAsync()
                        self <! WsActorMessages.Receive msg
                         
                    printfn "Websocket closed. Retrying..."
                         
                with
                | exc -> printfn "Websocket exception: %s. Retrying..." exc.Message

                do! Async.Sleep 1000
        }

    let handleMessage (mailbox: Actor<'a>) msg =
        match msg with
        | WsActorMessages.Start -> receiverLoop mailbox.Self |> ignore
        | WsActorMessages.Send msg -> send msg |> Async.RunSynchronously
        | WsActorMessages.Receive msg -> protocolActorRef <! ProtocolActor.Receive msg

    spawn system "ws-actor" (actorOf2 handleMessage)