module RulesActor

open Akka.Actor
open Akka.FSharp

open HassConfiguration
open Model
open RuleActor

type RulesActorMessages =
    | UpdateState of EventData

let spawnRulesActor system (configuration: Configuration) =
    let mutable entityIdArefMap = Map<string, IActorRef list> []

    let init mailbox =
        for rule in configuration.Rules do
            let (ruleAref, entityIds) = spawnRuleActor mailbox rule
            for entityId in entityIds do
                if not (entityIdArefMap.ContainsKey entityId) then
                    entityIdArefMap <- entityIdArefMap.Add(entityId, [ruleAref])
                else
                    entityIdArefMap <- entityIdArefMap.Add(entityId, ruleAref :: entityIdArefMap[entityId])

    let handleMessage mailbox msg =
        match msg with
        | UpdateState eventData ->
            match entityIdArefMap.TryFind(eventData.entity_id) with
            | Some arefs -> for aref in arefs do aref <! RuleActorMessages.UpdateState eventData
            | None -> ()

    let actor (mailbox: Actor<'a>) =
        init mailbox

        let rec loop() = actor {
            let! message = mailbox.Receive()
            handleMessage mailbox message
            return! loop()
        }
        loop()

    spawn system "rules" actor