module Json

open System.Text.Json

let fromJson<'T> (text: string): 'T =
    JsonSerializer.Deserialize<'T>(text)

let toJson obj =
    JsonSerializer.Serialize(obj)