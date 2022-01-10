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
      Actions: Action list }

type Configuration =
      // Home Assistant url in env var HASS_URL
    { HassUrl: string
      // Home Assistant long lived access token environment var: ACCESS_TOKEN
      AccessToken: string
      Rules: Rule list }

let loadRules () =
    //let yaml = File.ReadAllText "rules.yaml"
    let rules =
        [
            { Name = "Follow light turn on"
              Condition = StateChangedTo ({ EntityId = "light.plafond_hal"; State = "on" }) 
              Actions = 
                [
                    CallService ({ Domain = "light"; Service = "turn_on"; SerivceData = Map<string, string> []; Target = "light.dimmer_hal" })
                ] }
            { Name = "Follow light turn off"
              Condition = StateChangedTo ({ EntityId = "light.plafond_hal"; State = "off" }) 
              Actions = 
                [
                    CallService ({ Domain = "light"; Service = "turn_off"; SerivceData = Map<string, string> []; Target = "light.dimmer_hal" })
                ] }

            { Name = "Turn on lights at dusk"
              Condition = StateChangedTo ({ EntityId = "sun.sun"; State = "below_horizon" }) 
              Actions = 
                [
                    CallService ({ Domain = "light"; Service = "turn_on"; SerivceData = Map<string, string> [ ("brightness_pct", "7") ]; Target = "light.kroonluchter" })
                    CallService ({ Domain = "light"; Service = "turn_on"; SerivceData = Map<string, string> []; Target = "light.portiek" })
                ] }
            { Name = "Turn off lights at dawn"
              Condition = StateChangedTo ({ EntityId = "sun.sun"; State = "above_horizon" }) 
              Actions = 
                [
                    CallService ({ Domain = "light"; Service = "turn_off"; SerivceData = Map<string, string> []; Target = "light.kroonluchter" })
                    CallService ({ Domain = "light"; Service = "turn_off"; SerivceData = Map<string, string> []; Target = "light.portiek" })
                ] }
        ]
    //File.WriteAllText("rules.yaml", toYaml rules)
    rules
    
let getConfiguration() =
    { HassUrl = Environment.GetEnvironmentVariable("HASS_URL")
      AccessToken = Environment.GetEnvironmentVariable("ACCESS_TOKEN") 
      Rules = loadRules() }