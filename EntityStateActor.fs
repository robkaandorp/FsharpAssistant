module EntityStateActor

open Akka.FSharp

open Model
open RulesActor

type EntityStateActorMessages =
    | UpdateState of EventData

let spawnEntityStateActor parent (entityId: string) (eventData: EventData) =
    let mutable eventData = eventData

    let handleMessage mailbox msg =
        match msg with
        | UpdateState ed -> 
            match ed.old_state with
            | Some oldState -> logDebugf mailbox "%s %s -> %s" ed.entity_id oldState.state ed.new_state.state
            | None -> logDebugf mailbox "%s None -> %s" ed.entity_id ed.new_state.state
            
            eventData <- ed

            select "/user/rules" parent
            <! RulesActorMessages.UpdateState ed

    logInfof parent "Spawning entity state actor %s" entityId
    spawn parent ("entity-state-" + entityId) (actorOf2 handleMessage)