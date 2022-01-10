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

    let handleMessage mailbox msg =
        match msg with
        | Started -> 
            stateActor <! StateActorMessages.Start
            serviceActor <! ServiceActorMessages.Start
        | Stopped -> 
            stateActor <! StateActorMessages.Stop
            serviceActor <! ServiceActorMessages.Stop
        //| (t: Terminated) -> printfn "terminated"

    let actor (mailbox: Actor<'a>) =
        let protocolActorRef = spawnProtocolActor system configuration mailbox.Self
        let wsActorRef = spawnWsActor system configuration protocolActorRef
        let rulesAref = spawnRulesActor system configuration

        monitor wsActorRef mailbox |> ignore

        let rec loop() = actor {
            let! message = mailbox.Receive()
            handleMessage mailbox message
            return! loop()
        }
        loop()

    let coordinatorRef = spawn system "coordinator" actor

    coordinatorRef