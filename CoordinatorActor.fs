module CoordinatorActor

open Akka.FSharp

open HassConfiguration
open WsActor

let spawnCoordinatorActor system (configuration: Configuration) =
    let handleMessage msg =
        ()

    let coordinatorRef = spawn system "coordinator" (actorOf handleMessage)
    let _ = spawnWsActor system configuration

    coordinatorRef