open System
open System.Net.WebSockets

open Akka.FSharp

open HassConfiguration
open Model
open CoordinatorActor

let configuration = getConfiguration()

let system = System.create "fsharp-assistant" (Configuration.load())
let _ = spawnCoordinatorActor system configuration

//let restClient = RestClient configuration

//let message = restClient.connectApiAsync |> Async.RunSynchronously
//printfn "Message: %s" message.message

//let states = restClient.getStatesAsync |> Async.RunSynchronously

let recentlyUpdated (state: State) =
    state.last_updated > DateTime.Now.AddHours(-1)

//for state in Array.filter recentlyUpdated states do
//    printfn "States: %s: %s (changed %A) | # attributes = %d" state.entity_id state.state state.last_changed state.attributes.Count

//    for attribute in state.attributes do
//        printfn " - %A" attribute


let messageLoop (wsClient: WsClient) =
    let mutable msgCounter = 0
    let getMessageCounter () =
        msgCounter <- msgCounter + 1
        msgCounter
        
    async {
        while wsClient.State = WebSocketState.Open do
            let! msg = wsClient.receiveMessageAsync()

            match msg with
            | AuthRequired response ->
                printfn "HA version: %s" response.ha_version
                printf "Authenticating... "
                do! { ``type`` = "auth"; access_token = configuration.AccessToken } |> send
            | AuthOk response -> 
                printfn "success."

                do! { id = getMessageCounter(); ``type`` = "subscribe_events"; event_type = "state_changed" } 
                    |> send
            | AuthInvalid response -> printfn "failed: %s" response.message
            | Result response -> if not response.success then printfn "Request %d failed; %s - %s" response.id response.error.code response.error.message
            | Event response -> printfn "%s %s -> %s" response.event.data.entity_id response.event.data.old_state.state response.event.data.new_state.state
            | Other ``type`` -> printfn "Not implemented type %s" ``type``
            | Closed reason -> printfn "Socket closed %s" reason
            | Fail (ex) -> printfn "Receiving threw an exception %A" ex.SourceException

        printfn "Message loop exited."
    }

messageLoop wsClient |> Async.RunSynchronously
