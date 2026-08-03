/// The dependabot.yml as a tree the checks can walk.
module Config

open System.Collections
open YamlDotNet.Serialization

/// YamlDotNet hands back an untyped object graph. Converting it once, here, is
/// what keeps every `:? IDictionary` test out of the checks themselves.
type Node =
    | Mapping of Map<string, Node>
    | Sequence of Node list
    | Scalar of string
    | Empty

let rec private node (value: obj) =
    match value with
    | null -> Empty
    | :? string as text -> Scalar text
    // By key rather than by DictionaryEntry: YamlDotNet returns a generic
    // Dictionary, whose IEnumerable is of KeyValuePair and will not cast.
    | :? IDictionary as mapping ->
        mapping.Keys
        |> Seq.cast<obj>
        |> Seq.map (fun key -> string key, node mapping[key])
        |> Map.ofSeq
        |> Mapping
    | :? IEnumerable as items -> items |> Seq.cast<obj> |> Seq.map node |> List.ofSeq |> Sequence
    | other -> Scalar(string other)

/// Only the parse is guarded, so a bug in the conversion above keeps its own
/// stack trace instead of being reported as the user's file being malformed.
let load (path: string) =
    let text = System.IO.File.ReadAllText path

    let parsed =
        try
            DeserializerBuilder().Build().Deserialize<obj>(text)
        with error ->
            failwith $"%s{path} is not valid YAML: %s{error.Message}"

    node parsed

let tryGet name =
    function
    | Mapping entries -> Map.tryFind name entries
    | _ -> None

let tryScalar =
    function
    | Scalar text -> Some text
    | _ -> None

let keys =
    function
    | Mapping entries -> Map.keys entries |> Set.ofSeq
    | _ -> Set.empty
