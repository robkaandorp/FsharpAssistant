module ProtocolActor

open Akka.FSharp

open HassConfiguration
open Model

let spawnProtocolActor system (configuration: Configuration) =
    let handleMessage msg =
        match msg with
        | AuthRequired response ->
            printfn "HA version: %s" response.ha_version
            printf "Authenticating... "
            { ``type`` = "auth"; access_token = configuration.AccessToken } |> send
        | AuthOk response -> 
            printfn "success."

            { id = getMessageCounter(); ``type`` = "subscribe_events"; event_type = "state_changed" } 
                |> send
        | AuthInvalid response -> printfn "failed: %s" response.message
        | Result response -> if not response.success then printfn "Request %d failed; %s - %s" response.id response.error.code response.error.message
        | Event response -> printfn "%s %s -> %s" response.event.data.entity_id response.event.data.old_state.state response.event.data.new_state.state
        | Other ``type`` -> printfn "Not implemented type %s" ``type``
        | Closed reason -> printfn "Socket closed %s" reason
        | Fail (ex) -> printfn "Receiving threw an exception %A" ex.SourceException

    spawn system "protocol" (actorOf handleMessage)
