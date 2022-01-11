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

    let actor (mailbox: Actor<'a>) =
        let protocolActorRef = spawnProtocolActor system configuration mailbox.Self
        let rulesAref = spawnRulesActor system configuration

        let startWsActor () =
            let wsActorRef = spawnWsActor system configuration protocolActorRef
            wsActorRef <! WsActorMessages.Start
        startWsActor()

        let handleMessage mailbox msg =
            match msg with
            | Started -> 
                stateActor <! StateActorMessages.Start
                serviceActor <! ServiceActorMessages.Start
            | Stopped -> 
                stateActor <! StateActorMessages.Stop
                serviceActor <! ServiceActorMessages.Stop

        let rec loop() = actor {
            let! message = mailbox.Receive()
            handleMessage mailbox message
            return! loop()
        }
        loop()

    spawn system "coordinator" actor