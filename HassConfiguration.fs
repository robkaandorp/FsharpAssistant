module HassConfiguration

open System

type Configuration =
      // Home Assistant url
    { HassUrl: string
      // Home Assistant long lived access token environment var: ACCESS_TOKEN
      AccessToken: string }

let getConfiguration() =
    { HassUrl = Environment.GetEnvironmentVariable("HASS_URL")
      AccessToken = Environment.GetEnvironmentVariable("ACCESS_TOKEN") }