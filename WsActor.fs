module WsActor

open Akka.FSharp

open Json
open HassConfiguration
open WsClient

let spawnWsActor system (configuration: Configuration) =
    let wsClient = WsClient configuration
    wsClient.connectAsync() |> Async.RunSynchronously

    let send obj = toJson obj |> wsClient.sendMessageAsync
    
    let handleMessage msg =
        send msg |> Async.RunSynchronously
        
    spawn system "ws-actor" (actorOf handleMessage)