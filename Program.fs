open System

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

Console.ReadLine() |> ignore

system.Terminate() |> Async.AwaitTask |> ignore
printfn "Actor system terminated."