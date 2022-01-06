module StateActor

open Akka.FSharp

open Model
open ActorMessages
open ProtocolActor

let spawnStateActor system =
    let mutable states = Map<string, EventData> []

    let handleMessage mailbox msg =
        match msg with
        | Start -> 
            printfn "StateActor received Start"
            let protocolActerRef = select "/user/protocol" system
            protocolActerRef <! Send (SubscribeEvents "state_changed")
        | Stop -> 
            printfn "StateActor received Stop"
            states <- Map<string, EventData> []
        | State eventMsg -> 
            printfn "%s %s -> %s" eventMsg.event.data.entity_id eventMsg.event.data.old_state.state eventMsg.event.data.new_state.state
            states <- states.Add(eventMsg.event.data.entity_id, eventMsg.event.data)

    spawn system "state" (actorOf2 handleMessage)