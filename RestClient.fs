module RestClient

open System
open System.Net.Http
open System.Net.Http.Json

open Configuration
open Model

type RestClient(configuration: Configuration) as self =
    let httpClient = new HttpClient()
    do httpClient.BaseAddress <- Uri(configuration.HassUrl)
    let _ = httpClient.DefaultRequestHeaders.TryAddWithoutValidation("authorization", "Bearer " + configuration.AccessToken)

    member self.connectApiAsync =
        async {
            let! result = httpClient.GetAsync("/api/") |> Async.AwaitTask
            result.EnsureSuccessStatusCode() |> ignore
    
            return! result.Content.ReadFromJsonAsync<ApiMessage>() |> Async.AwaitTask
        }
    
    member self.getStatesAsync =
        async {
            let! result = httpClient.GetAsync("/api/states") |> Async.AwaitTask
            result.EnsureSuccessStatusCode() |> ignore
    
            return! result.Content.ReadFromJsonAsync<State[]>() |> Async.AwaitTask
        }