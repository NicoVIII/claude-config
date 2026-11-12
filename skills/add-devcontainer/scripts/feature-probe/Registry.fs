/// What the registries say exists.
module Registry

open System
open System.Collections.Generic
open System.Text.Json
open System.Text.RegularExpressions
open Domain

let private mine = "NicoVIII/devcontainer-features"

/// `absent` names the statuses that are an answer — "not there" — for this
/// request. Every other refusal stops the run: a 500 or a rate limit says
/// nothing about the feature.
let private get (fetch: Fetch) (absent: int list) (request: Request) =
    match fetch request with
    | Answered body -> Ok(Some body)
    | Refused status when List.contains status absent -> Ok None
    | Refused status -> Error(UnexpectedStatus(request.Url, status))
    | Unreachable reason -> Error(NoAnswer(request.Url, reason))

let private listing (fetch: Fetch) (url: string) =
    match fetch { Url = url; Bearer = None } with
    | Answered body -> Ok body
    | Refused status -> Error(UnexpectedStatus(url, status))
    | Unreachable reason -> Error(NoAnswer(url, reason))

/// A body that parses but lacks what was asked of it is the registry having
/// changed shape, and has to stop the run rather than read as an empty answer.
let private parse (url: string) (read: JsonElement -> 'a) (body: string) =
    try
        use document = JsonDocument.Parse body
        Ok(read document.RootElement)
    with error when
        (error :? JsonException
         || error :? KeyNotFoundException
         || error :? InvalidOperationException) ->
        Error(Malformed(url, error.Message))

let private text (element: JsonElement) =
    match element.ValueKind with
    | JsonValueKind.String -> element.GetString()
    | kind -> raise (InvalidOperationException $"expected a string, found %A{kind}")

let private texts (element: JsonElement) =
    element.EnumerateArray() |> Seq.map text |> List.ofSeq

let private tagList (root: JsonElement) = root.GetProperty "tags" |> texts

// GHCR refuses the pull token (403) for an artifact that is not there, but
// either request is entitled to be the one that says so.
let private ghcrAbsent = [ 401; 403; 404 ]

/// GHCR hands out a pull token per repository, even for public ones.
let private tags (fetch: Fetch) (ref: FeatureRef) =
    let repository = $"%s{ref.Owner}/%s{ref.Repo}/%s{ref.Feature}"

    let tokenUrl =
        $"https://ghcr.io/token?scope=repository:%s{repository}:pull&service=ghcr.io"

    let tagsUrl = $"https://ghcr.io/v2/%s{repository}/tags/list"

    let published (token: string) =
        get fetch ghcrAbsent { Url = tagsUrl; Bearer = Some token }
        |> Result.bind (function
            | None -> Ok NotPublished
            | Some body -> parse tagsUrl tagList body |> Result.map Published)

    get fetch ghcrAbsent { Url = tokenUrl; Bearer = None }
    |> Result.bind (function
        | None -> Ok NotPublished
        | Some body ->
            parse tokenUrl (fun root -> root.GetProperty "token" |> text) body
            |> Result.bind published)

/// `options` may be missing — that is the finding that nothing can be pinned,
/// same as an empty one. `version` may not: the feature spec requires it.
let private readManifest (root: JsonElement) =
    let options =
        match root.TryGetProperty "options" with
        | true, found -> Some(Json.element found)
        | _ -> None

    Manifest(root.GetProperty "version" |> text, options)

/// The manifest is the only statement of what the feature can pin.
let private manifest (fetch: Fetch) (ref: FeatureRef) =
    let url =
        $"https://raw.githubusercontent.com/%s{ref.Owner}/%s{ref.Repo}/main/src/%s{ref.Feature}/devcontainer-feature.json"

    get fetch [ 404 ] { Url = url; Bearer = None }
    |> Result.bind (function
        | None -> Ok(NoManifest url)
        | Some body -> parse url readManifest body)

let probe (fetch: Fetch) (ref: FeatureRef) =
    tags fetch ref
    |> Result.bind (fun tags ->
        manifest fetch ref
        |> Result.map (fun manifest ->
            { Ref = ref
              Tags = tags
              Manifest = manifest }))

let private directories (root: JsonElement) =
    root.EnumerateArray()
    |> Seq.filter (fun entry -> text (entry.GetProperty "type") = "dir")
    |> Seq.map (fun entry -> text (entry.GetProperty "name"))
    |> List.ofSeq

let mineFeatures (fetch: Fetch) =
    let url = $"https://api.github.com/repos/%s{mine}/contents/src"

    listing fetch url
    |> Result.bind (parse url directories)
    |> Result.map (List.map (fun name -> $"ghcr.io/%s{mine.ToLowerInvariant()}/%s{name}"))

// Floating tags (`2`, `2.1-bookworm`, `dev-bookworm`) are not candidates: only
// an exact version pins anything.
let private exactTag = Regex(@"^(\d+\.\d+\.\d+)-([a-z].*)$", RegexOptions.Compiled)

/// Compared as versions, not text: 2.1.10 is newer than 2.1.8.
let private newestPerDistro (tags: string list) =
    tags
    |> List.map exactTag.Match
    |> List.filter _.Success
    |> List.groupBy _.Groups[2].Value
    |> List.sortBy fst
    |> List.map (fun (_, matches) -> (matches |> List.maxBy (fun found -> Version.Parse found.Groups[1].Value)).Value)

let baseTags (fetch: Fetch) =
    let url = "https://mcr.microsoft.com/v2/devcontainers/base/tags/list"

    listing fetch url
    |> Result.bind (parse url tagList)
    |> Result.map newestPerDistro
