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
            let oldState =
                match ed.old_state with
                | Some s -> s.state
                | None -> "None"

            let newState =
                match ed.new_state with
                | Some s -> s.state
                | None -> "None"

            logDebugf mailbox "%s %s -> %s" ed.entity_id oldState newState
            
            eventData <- ed

            select "/user/rules" parent
            <! RulesActorMessages.UpdateState ed

    logInfof parent "Spawning entity state actor %s" entityId
    spawn parent ("entity-state-" + entityId) (actorOf2 handleMessage)