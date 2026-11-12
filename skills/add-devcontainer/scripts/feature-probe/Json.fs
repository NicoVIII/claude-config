/// How JSON is written back out: on one line, and unescaped.
module Json

open System.Text.Encodings.Web
open System.Text.Json

// The default encoder writes `+` as +, which is how a version like
// 1.0.0+build would reach the reader.
let private options =
    JsonSerializerOptions(Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

let element (value: JsonElement) = JsonSerializer.Serialize(value, options)

let text (value: string) = JsonSerializer.Serialize(value, options)

let texts (values: string list) =
    JsonSerializer.Serialize(Array.ofList values, options)
