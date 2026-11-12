/// What the arguments ask for.
module Cli

open Domain

let private featureRef (text: string) =
    let unprefixed =
        if text.StartsWith "ghcr.io/" then
            text.Substring "ghcr.io/".Length
        else
            text

    let untagged =
        match unprefixed.LastIndexOf ':' with
        | -1 -> unprefixed
        | colon -> unprefixed.Substring(0, colon)

    match untagged.Split '/' with
    | [| owner; repo; feature |] when owner <> "" && repo <> "" && feature <> "" ->
        Ok
            { Owner = owner
              Repo = repo
              Feature = feature }
    | _ -> Error(NotAFeatureRef text)

let private featureRefs (texts: string list) =
    List.foldBack
        (fun text refs ->
            match featureRef text, refs with
            | Ok ref, Ok rest -> Ok(ref :: rest)
            | Error error, _ -> Error error
            | _, Error error -> Error error)
        texts
        (Ok [])

let parse (argv: string list) =
    match argv with
    | [] -> Error NoArguments
    | [ "--mine" ] -> Ok Mine
    | [ "--base" ] -> Ok Base
    | refs -> featureRefs refs |> Result.map Probe
