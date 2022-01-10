module ProtocolActor

open Akka.Actor
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

let spawnProtocolActor (system: ActorSystem) (configuration: Configuration) coordinatorActorRef =
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

        wsActor <! WsActorMessages.Send preparedMsg

    let handleReceive mailbox msg =
        match msg with
        | AuthRequired response ->
            if response.ha_version.IsSome then
                logInfof mailbox "HA version: %s" response.ha_version.Value
            logDebug mailbox "Authenticating... "
            let wsActor = select "/user/ws-actor" system
            wsActor <! WsActorMessages.Send (new AuthenticationRequestMessage(configuration.AccessToken))
        | AuthOk response -> 
            logDebug mailbox "success."
            coordinatorActorRef <! Started
        | AuthInvalid response -> logErrorf mailbox "Authentication failed: %s" response.message.Value
        | Result response -> 
            if not response.success then 
                logWarningf mailbox "Request %d failed; %s - %s" response.id response.error.Value.code response.error.Value.message

            if oneTimeRequests.ContainsKey response.id then
                oneTimeRequests[response.id] <! GetServiceResponse response.result.Value
                oneTimeRequests <- oneTimeRequests.Remove response.id
        | Event response ->
            match subscribers.TryFind response.id with
            | Some subscriber -> subscriber <! StateActorMessages.State response
            | None -> logWarningf mailbox "Event response for %d, but there was no subscriber" response.id
        | Other ``type`` -> logWarningf mailbox "Not implemented type %s" ``type``
        | Closed reason -> 
            logInfof mailbox "Socket closed %s" reason
            coordinatorActorRef <! Stopped
        | Fail (ex) -> 
            logErrorf mailbox "Receiving threw an exception %A" ex.SourceException
            coordinatorActorRef <! Stopped

    let handleMessage (mailbox: Actor<'a>) msg =
        match msg with
        | Send msg -> handleSend mailbox msg
        | Receive msg -> handleReceive mailbox msg

    spawn system "protocol" (actorOf2 handleMessage)
