module EntityActor

open Akka.FSharp

open Model
open ProtocolActor

type EntityActorMessages =
    | UpdateState of EventData

let spawnEntityActor parent (entityId: string) (eventData: EventData) =
    let mutable eventData = eventData

    let handleMessage mailbox msg =
        match msg with
        | UpdateState ed -> 
            logDebugf mailbox "%s %s -> %s" ed.entity_id ed.old_state.state ed.new_state.state
            eventData <- ed

            if entityId = "switch.aquarium_heater" then
                if ed.old_state.state = "off" && ed.new_state.state = "on" then
                    logInfo mailbox "Heater turned on"
                    let protocolAref = select "/user/protocol" mailbox.Context
                    protocolAref <! Send (CallService("switch", "turn_on", "switch.kerstboom"))

                elif ed.old_state.state = "on" && ed.new_state.state = "off" then
                    logInfo mailbox "Heater turned off"
                    let protocolAref = select "/user/protocol" mailbox.Context
                    protocolAref <! Send (CallService("switch", "turn_off", "switch.kerstboom"))

    logInfof parent "Spawning entity actor %s" entityId
    spawn parent ("entity-" + entityId) (actorOf2 handleMessage)