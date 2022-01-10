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
            logDebugf mailbox "%s %s -> %s" ed.entity_id ed.old_state.state ed.new_state.state
            eventData <- ed

            select "/user/rules" parent
            <! RulesActorMessages.UpdateState ed

    logInfof parent "Spawning entity state actor %s" entityId
    spawn parent ("entity-state-" + entityId) (actorOf2 handleMessage)