module WsActor

open System.Net.WebSockets
open Akka.FSharp

open Json
open HassConfiguration
open WsClient

let spawnWsActor system (configuration: Configuration) protocolActorRef =
    let wsClient = WsClient configuration
    wsClient.connectAsync() |> Async.RunSynchronously
    
    let receiverLoop () =
        backgroundTask {
            while wsClient.State = WebSocketState.Open do
                let! msg = wsClient.receiveMessageAsync()
                protocolActorRef <! msg
        }

    receiverLoop() |> ignore

    let send obj = toJson obj |> wsClient.sendMessageAsync
    
    let handleMessage msg =
        send msg |> Async.RunSynchronously
        
    spawn system "ws-actor" (actorOf handleMessage)