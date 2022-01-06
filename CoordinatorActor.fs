module CoordinatorActor

open Akka.FSharp

open HassConfiguration
open ActorMessages
open WsActor
open ProtocolActor
open StateActor

let spawnCoordinatorActor system (configuration: Configuration) =
    let stateActor = spawnStateActor system

    let handleMessage msg =
        match msg with
        | Started -> stateActor <! StateActorMessages.Start
        | Stopped -> stateActor <! StateActorMessages.Stop

    let coordinatorRef = spawn system "coordinator" (actorOf handleMessage)
    let protocolActorRef = spawnProtocolActor system configuration coordinatorRef
    let _ = spawnWsActor system configuration protocolActorRef

    coordinatorRef