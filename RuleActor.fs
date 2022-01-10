module RuleActor

open Akka.FSharp

open HassConfiguration
open Model
open ProtocolActor

type RuleActorMessages =
    | UpdateState of EventData

let spawnRuleActor system (rule: Rule) =
    let rec getEntityIds (condition: Condition) =
        match condition with
        | And (head :: tail) -> getEntityIds head @ getEntityIds (And tail)
        | And ([]) -> []
        | Or (head :: tail) -> getEntityIds head @ getEntityIds (And tail)
        | Or ([]) -> []
        | StateChangedTo entityCondition -> [entityCondition.EntityId]
        | StateEquals entityCondition -> [entityCondition.EntityId]

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

    let executeAction action =
        match action with
        | CallService callServiceAction ->
            logInfof system "Executing action CallService %s.%s(%s)" callServiceAction.Domain callServiceAction.Service callServiceAction.Target
            select "/user/protocol" system
            <! Send (CallService(callServiceAction.Domain, callServiceAction.Service, callServiceAction.Target))
            // TODO add ServiceData to CallService

    let handleMessage mailbox msg =
        match msg with
        | UpdateState eventData ->
            if matchCondition rule.Condition eventData then
                logInfof system "Rule '%s' matched" rule.Name
                executeAction rule.Action

    let ruleAref = spawn system ("rule-" + rule.Name.Replace(' ', '_')) (actorOf2 handleMessage)
    (ruleAref, getEntityIds rule.Condition)