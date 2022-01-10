module EntityStateActor

open Akka.FSharp

open Model
open ProtocolActor

type EntityStateActorMessages =
    | UpdateState of EventData

let spawnEntityStateActor parent (entityId: string) (eventData: EventData) =
    let mutable eventData = eventData

    let handleMessage mailbox msg =
        match msg with
        | UpdateState ed -> 
            logDebugf mailbox "%s %s -> %s" ed.entity_id ed.old_state.state ed.new_state.state
            eventData <- ed

            if entityId = "light.plafond_hal" then
                if ed.old_state.state <> "on" && ed.new_state.state = "on" then
                    logInfo mailbox "Light turned on"
                    let protocolAref = select "/user/protocol" mailbox.Context
                    protocolAref <! Send (CallService("light", "turn_on", "light.dimmer_hal"))

                elif ed.old_state.state <> "off" && ed.new_state.state = "off" then
                    logInfo mailbox "Light turned off"
                    let protocolAref = select "/user/protocol" mailbox.Context
                    protocolAref <! Send (CallService("light", "turn_off", "light.dimmer_hal"))

    logInfof parent "Spawning entity state actor %s" entityId
    spawn parent ("entity-state-" + entityId) (actorOf2 handleMessage)