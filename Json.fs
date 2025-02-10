module Json

open System.Text.Json
open System.Text.Json.Serialization

let options = JsonSerializerOptions()

let fromJson<'T> (text: string): 'T =
    JsonSerializer.Deserialize<'T>(text, options)

let toJson obj =
    JsonSerializer.Serialize(obj, options)