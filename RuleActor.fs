module RuleActor

open Akka.FSharp

open HassConfiguration
open Model

let spawnRuleActor system (rule: Rule) =
    let handleMessage mailbox msg =
        ()

    let rec matchCondition (condition: Condition) (eventData: EventData) =
        match condition with
        | And (head :: tail) -> (matchCondition head eventData) && matchCondition (And tail) eventData
        | And ([]) -> true
        | Or (head :: tail) -> (matchCondition head eventData) || matchCondition (Or tail) eventData
        | Or ([]) -> false
        | StateChangedTo entityCondition -> 
            entityCondition.EntityId = eventData.entity_id &&
            entityCondition.State <> eventData.old_state.state &&
            entityCondition.State = eventData.new_state.state
        | StateEquals entityCondition -> 
            entityCondition.EntityId = eventData.entity_id &&
            entityCondition.State = eventData.new_state.state

    spawn system ("rule-" + rule.Name) (actorOf2 handleMessage)