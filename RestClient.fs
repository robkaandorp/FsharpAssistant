module RestClient

open System
open System.Net.Http
open System.Net.Http.Json

open HassConfiguration
open Model

type RestClient(configuration: Configuration) =
    let httpClient = new HttpClient()
    do httpClient.BaseAddress <- Uri(configuration.HassUrl)
    let _ = httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", "Bearer " + configuration.AccessToken)

    member this.connectApiAsync =
        async {
            let! result = httpClient.GetAsync("/api/") |> Async.AwaitTask
            result.EnsureSuccessStatusCode() |> ignore
    
            return! result.Content.ReadFromJsonAsync<ApiMessage>() |> Async.AwaitTask
        }
    
    member this.getStatesAsync =
        async {
            let! result = httpClient.GetAsync("/api/states") |> Async.AwaitTask
            result.EnsureSuccessStatusCode() |> ignore
    
            return! result.Content.ReadFromJsonAsync<State[]>() |> Async.AwaitTask
        }