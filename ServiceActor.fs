module ServiceActor

open Akka.FSharp

open Model
open type ActorMessages.ServiceActorMessages
open ProtocolActor

let spawnServiceActor system  =
    let handleMessage msg =
        match msg with
        | Start ->
            printfn "ServiceActor received Start"
            let protocolAref = select "/user/protocol" system
            protocolAref <! Send (GetServices())
        | Stop ->
            printfn "ServiceActor received Stop"
        | GetServiceResponse result ->
            printfn "Found service domains:"
            for domain in result.Keys do
                printfn " * %s" domain

    let serviceAref = spawn system "service" (actorOf handleMessage)

    serviceAref