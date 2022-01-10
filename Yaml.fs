module Yaml

open System.IO

open YamlDotNet.Serialization

let toYaml obj =
    let serializer = Serializer()
    serializer.Serialize obj