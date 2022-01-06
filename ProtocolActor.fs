module ProtocolActor

open Akka.FSharp

open HassConfiguration
open Model

type MessageDirection =
    | Send of RequestMessage
    | Receive of Message

let spawnProtocolActor system (configuration: Configuration) =
    let mutable msgCounter = 0
    let getMessageCounter () =
        msgCounter <- msgCounter + 1
        msgCounter

    let handleSend (msg: RequestMessage) =
        let wsActor = select "/user/ws-actor" system

        match msg with
        | :? RequestMessageWithId as withId ->
            withId.id <- getMessageCounter()
            wsActor <! withId
        | _ -> wsActor <! msg

    let handleReceive msg =
        match msg with
        | AuthRequired response ->
            printfn "HA version: %s" response.ha_version
            printf "Authenticating... "
            handleSend <| AuthenticationRequestMessage configuration.AccessToken
        | AuthOk response -> 
            printfn "success."
            handleSend <| SubscribeEvents "state_changed"
        | AuthInvalid response -> printfn "failed: %s" response.message
        | Result response -> if not response.success then printfn "Request %d failed; %s - %s" response.id response.error.code response.error.message
        | Event response -> printfn "%s %s -> %s" response.event.data.entity_id response.event.data.old_state.state response.event.data.new_state.state
        | Other ``type`` -> printfn "Not implemented type %s" ``type``
        | Closed reason -> printfn "Socket closed %s" reason
        | Fail (ex) -> printfn "Receiving threw an exception %A" ex.SourceException

    let handleMessage msg =
        match msg with
        | Send msg -> handleSend msg
        | Receive msg -> handleReceive msg

    spawn system "protocol" (actorOf handleMessage)
