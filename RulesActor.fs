module RulesActor

open Akka.Actor
open Akka.FSharp

open HassConfiguration
open RuleActor

let spawnRulesActor system (configuration: Configuration) =
    let handleMessage mailbox msg =
        ()

    let actor (mailbox: Actor<'a>) =
        for rule in configuration.Rules do
            spawnRuleActor mailbox rule |> ignore

        let rec loop() = actor {
            let! message = mailbox.Receive()
            handleMessage mailbox message
            return! loop()
        }
        loop()

    spawn system "rules" actor