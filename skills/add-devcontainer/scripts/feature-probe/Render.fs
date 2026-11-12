/// The only mapping from what was found back to text.
module Render

open Domain

let private tags =
    function
    | Published tags -> Json.texts tags
    | NotPublished -> "NOT PUBLISHED — no such artifact on ghcr.io"

let private manifest =
    function
    | Manifest(version, options) ->
        let options = options |> Option.defaultValue "null"
        $"""{{"version":%s{Json.text version},"options":%s{options}}}"""
    | NoManifest url -> $"NO MANIFEST at %s{url} — wrong path, or not a src/<feature> layout"

let probed (probed: Probed) =
    [ $"== %s{probed.Ref.Owner}/%s{probed.Ref.Repo}/%s{probed.Ref.Feature}"
      $"tags     %s{tags probed.Tags}"
      $"manifest %s{manifest probed.Manifest}" ]

let usageError =
    function
    | NoArguments -> "usage: feature-probe.sh <feature-ref>... | --mine | --base"
    | NotAFeatureRef text -> $"not a feature ref: %s{text}"

let failure =
    function
    | NoAnswer(url, reason) -> $"%s{url} unreachable: %s{reason}"
    | UnexpectedStatus(url, status) -> $"%s{url} answered HTTP %d{status}"
    | Malformed(url, problem) -> $"%s{url} answered something unreadable: %s{problem}"
