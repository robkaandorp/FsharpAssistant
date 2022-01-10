module ProtocolActor

open Akka.FSharp

open HassConfiguration
open Model
open ActorMessages

type MessageDirection =
    | Send of RequestMessage
    | Receive of Message

type ConnectionState =
    | Started
    | Stopped

let spawnProtocolActor system (configuration: Configuration) coordinatorActorRef =
    let mutable msgCounter = 0
    let getMessageCounter () =
        msgCounter <- msgCounter + 1
        msgCounter

    let mutable subscribers = Map<int, Akka.Actor.IActorRef> []
    let mutable oneTimeRequests = Map<int, Akka.Actor.IActorRef> []

    let handleSend (mailbox: Actor<'a>) (msg: RequestMessage) =
        let wsActor = select "/user/ws-actor" system

        let preparedMsg: RequestMessage =
            match msg with
            | :? RequestMessageWithId as withId ->
                withId.id <- getMessageCounter()
                withId
            | _ -> msg

        match preparedMsg with
        | :? SubscribeEvents as msg -> subscribers <- subscribers.Add(msg.id, mailbox.Sender())
        | :? GetServices as msg -> oneTimeRequests <- oneTimeRequests.Add(msg.id, mailbox.Sender())
        | _ -> ()

        wsActor <! preparedMsg

    let handleReceive msg =
        match msg with
        | AuthRequired response ->
            printfn "HA version: %s" response.ha_version
            printf "Authenticating... "
            let wsActor = select "/user/ws-actor" system
            wsActor <! AuthenticationRequestMessage configuration.AccessToken
        | AuthOk response -> 
            printfn "success."
            coordinatorActorRef <! Started
        | AuthInvalid response -> printfn "failed: %s" response.message.Value
        | Result response -> 
            if not response.success then 
                printfn "Request %d failed; %s - %s" response.id response.error.Value.code response.error.Value.message

            if oneTimeRequests.ContainsKey response.id then
                oneTimeRequests[response.id] <! GetServiceResponse response.result.Value
                oneTimeRequests <- oneTimeRequests.Remove response.id
        | Event response ->
            match subscribers.TryFind response.id with
            | Some subscriber -> subscriber <! StateActorMessages.State response
            | None -> printfn "Event response for %d, but there was no subscriber" response.id
        | Other ``type`` -> printfn "Not implemented type %s" ``type``
        | Closed reason -> 
            printfn "Socket closed %s" reason
            coordinatorActorRef <! Stopped
        | Fail (ex) -> 
            printfn "Receiving threw an exception %A" ex.SourceException
            coordinatorActorRef <! Stopped

    let handleMessage (mailbox: Actor<'a>) msg =
        match msg with
        | Send msg -> handleSend mailbox msg
        | Receive msg -> handleReceive msg

    spawn system "protocol" (actorOf2 handleMessage)
