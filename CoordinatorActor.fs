module CoordinatorActor

open Akka.FSharp

open HassConfiguration
open WsActor
open ProtocolActor

let spawnCoordinatorActor system (configuration: Configuration) =
    let handleMessage msg =
        ()

    let coordinatorRef = spawn system "coordinator" (actorOf handleMessage)
    let protocolActorRef = spawnProtocolActor system configuration
    let _ = spawnWsActor system configuration protocolActorRef

    coordinatorRef