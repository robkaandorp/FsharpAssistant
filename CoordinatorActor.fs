module CoordinatorActor

open Akka.FSharp

open HassConfiguration
open ActorMessages
open WsActor
open ProtocolActor
open StateActor
open ServiceActor

let spawnCoordinatorActor system (configuration: Configuration) =
    let stateActor = spawnStateActor system
    let serviceActor = spawnServiceActor system

    let handleMessage msg =
        match msg with
        | Started -> 
            stateActor <! StateActorMessages.Start
            serviceActor <! ServiceActorMessages.Start
        | Stopped -> 
            stateActor <! StateActorMessages.Stop
            serviceActor <! ServiceActorMessages.Stop
        //| (t: Terminated) -> printfn "terminated"

    let coordinatorActor = actorOf handleMessage
    let coordinatorRef = spawn system "coordinator" coordinatorActor
    let protocolActorRef = spawnProtocolActor system configuration coordinatorRef
    let wsActorRef = spawnWsActor system configuration protocolActorRef

    //monitor wsActorRef coordinatorActor |> ignore

    coordinatorRef