module CoordinatorActor

open Akka.Actor
open Akka.FSharp

open HassConfiguration
open ActorMessages
open WsActor
open ProtocolActor
open StateActor
open ServiceActor
open RulesActor

let spawnCoordinatorActor (system: ActorSystem) (configuration: Configuration) =
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

    let coordinatorRef = spawn system "coordinator" (actorOf handleMessage)
    let protocolActorRef = spawnProtocolActor system configuration coordinatorRef
    let wsActorRef = spawnWsActor system configuration protocolActorRef
    let rulesAref = spawnRulesActor system configuration

    //monitor wsActorRef coordinatorActor |> ignore

    coordinatorRef