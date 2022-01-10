module StateActor

open Akka.Actor
open Akka.FSharp

open Model
open type ActorMessages.StateActorMessages
open ProtocolActor
open EntityStateActor

let spawnStateActor system =
    let mutable states = Map<string, IActorRef> []

    let handleMessage (mailbox: Actor<'a>) msg =
        match msg with
        | Start -> 
            printfn "StateActor received Start"
            let protocolActerRef = select "/user/protocol" system
            protocolActerRef <! Send (SubscribeEvents "state_changed")
        | Stop -> 
            printfn "StateActor received Stop"
            for actorRef in states.Values do
                mailbox.Context.Stop actorRef
            states <- Map<string, IActorRef> []
        | State eventMsg -> 
            let eventData = eventMsg.event.data
            if not (states.ContainsKey eventData.entity_id) then
                states <- states.Add(eventData.entity_id, spawnEntityStateActor mailbox eventMsg.event.data.entity_id eventMsg.event.data)
            states[eventData.entity_id] <! UpdateState eventData

    spawn system "state" (actorOf2 handleMessage)