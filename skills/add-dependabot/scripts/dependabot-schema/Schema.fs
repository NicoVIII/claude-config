/// What the published schema says a dependabot.yml may contain.
module Schema

open System.Net.Http
open System.Text.Json

let private url = "https://json.schemastore.org/dependabot-2.0.json"

type Constraints =
    { RootKeys: Set<string>
      UpdateKeys: Set<string>
      RequiredUpdateKeys: string list
      GroupKeys: Set<string>
      Ecosystems: string list }

/// Navigating by name states the path it wanted: a schema that reorganised its
/// definitions has to stop the run, not leave a check quietly matching nothing.
let private child (path: string) (name: string) (element: JsonElement) =
    match element.TryGetProperty name with
    | true, found -> found
    | _ -> failwith $"schema has no %s{path}/%s{name} — the schema has moved"

let private names (element: JsonElement) =
    element.EnumerateObject() |> Seq.map _.Name |> Set.ofSeq

let private strings (element: JsonElement) =
    element.EnumerateArray() |> Seq.map _.GetString() |> List.ofSeq

let fetch () =
    use client = new HttpClient()

    let json =
        try
            client.GetStringAsync(url) |> Async.AwaitTask |> Async.RunSynchronously
        with error ->
            failwith $"cannot reach %s{url}: %s{error.Message}"

    use document = JsonDocument.Parse(json)
    let root = document.RootElement
    let definitions = root |> child "" "definitions"
    let update = definitions |> child "definitions" "update"
    let updateKeys = update |> child "definitions/update" "properties"

    let groupKeys =
        updateKeys
        |> child "definitions/update/properties" "groups"
        |> child "definitions/update/properties/groups" "additionalProperties"
        |> child "definitions/update/properties/groups/additionalProperties" "properties"

    { RootKeys = root |> child "" "properties" |> names
      UpdateKeys = names updateKeys
      RequiredUpdateKeys = update |> child "definitions/update" "required" |> strings
      GroupKeys = names groupKeys
      Ecosystems =
        definitions
        |> child "definitions" "package-ecosystem-values"
        |> child "definitions/package-ecosystem-values" "enum"
        |> strings }
