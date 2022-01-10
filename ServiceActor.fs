module ServiceActor

open Akka.FSharp

open Model
open type ActorMessages.ServiceActorMessages
open ProtocolActor

let spawnServiceActor system  =
    let handleMessage mailbox msg =
        match msg with
        | Start ->
            logInfo mailbox "ServiceActor received Start"
            select "/user/protocol" system
            <! Send (GetServices())
        | Stop ->
            logInfo mailbox "ServiceActor received Stop"
        | GetServiceResponse result ->
            logInfo mailbox "Found service domains:"
            for domain in result.Keys do
                logInfof mailbox " * %s" domain

    let serviceAref = spawn system "service" (actorOf2 handleMessage)

    serviceAref