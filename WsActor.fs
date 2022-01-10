module WsActor

open System.Net.WebSockets
open Akka.FSharp

open Json
open HassConfiguration
open WsClient

let spawnWsActor system (configuration: Configuration) protocolActorRef =
    let actor (mailbox: Actor<'a>) =
        let wsClient = WsClient configuration
        wsClient.connectAsync() |> Async.RunSynchronously

        let receiverLoop () =
            backgroundTask {
                while wsClient.State = WebSocketState.Open do
                    let! msg = wsClient.receiveMessageAsync()
                    protocolActorRef <! ProtocolActor.Receive msg

                logWarningf mailbox "Websocket receiver loop exited, state was %A. Stopping WsActor." wsClient.State
                mailbox.Context.Stop(mailbox.Self)
            }

        receiverLoop() |> ignore

        let send obj = toJson obj |> wsClient.sendMessageAsync
    
        let handleMessage mailbox msg =
            send msg |> Async.RunSynchronously

        let rec loop() = actor {
            let! message = mailbox.Receive()
            handleMessage mailbox message
            return! loop()
        }
        loop()
        
    spawn system "ws-actor" actor