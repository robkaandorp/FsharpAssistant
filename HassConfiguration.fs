module HassConfiguration

open System
open System.IO

type EntityCondition =
    { EntityId: string
      State: string }

type Condition =
    | And of Condition list
    | Or of Condition list
    | StateChangedTo of EntityCondition
    | StateEquals of EntityCondition

type CallServiceAction =
    { Domain: string
      Service: string
      SerivceData: Map<string, string>
      Target: string }

type Action =
    | CallService of CallServiceAction

type Rule =
    { Name: string
      Condition: Condition 
      Action: Action }

type Configuration =
      // Home Assistant url in env var HASS_URL
    { HassUrl: string
      // Home Assistant long lived access token environment var: ACCESS_TOKEN
      AccessToken: string
      Rules: Rule list }

let loadRules () =
    //let yaml = File.ReadAllText "rules.yaml"
    [
        { Name = "Follow light turn on"
          Condition = StateChangedTo ({ EntityId = "light.plafond_hal"; State = "on" }) 
          Action = CallService ({ Domain = "light"; Service = "turn_on"; SerivceData = Map<string, string> []; Target = "light.dimmer_hal" }) }
        { Name = "Follow light turn off"
          Condition = StateChangedTo ({ EntityId = "light.plafond_hal"; State = "off" }) 
          Action = CallService ({ Domain = "light"; Service = "turn_off"; SerivceData = Map<string, string> []; Target = "light.dimmer_hal" }) }
    ]
    
let getConfiguration() =
    { HassUrl = Environment.GetEnvironmentVariable("HASS_URL")
      AccessToken = Environment.GetEnvironmentVariable("ACCESS_TOKEN") 
      Rules = loadRules() }