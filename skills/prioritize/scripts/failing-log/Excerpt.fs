/// What of a failed run's log is worth reading.
module Excerpt

open System.Text.RegularExpressions

let private runIdInLink = Regex @"/runs/(\d+)"

/// The PR's check list carries no run id; the failing check's link is where
/// one surfaces. An external check links to its own service and has none.
let runId (link: string) =
    match runIdInLink.Match link with
    | found when found.Success -> Some found.Groups[1].Value
    | _ -> None

let private namesAFailure = Regex("error|failed", RegexOptions.IgnoreCase)

// gh prefixes every line with job, step and an ISO timestamp. The timestamp
// answers nothing "real blocker or flake?" asks, and the step column reads
// "UNKNOWN STEP" whenever gh cannot resolve it — together a third of the bytes.
let private unresolvedStep = Regex @"\tUNKNOWN STEP\t"
let private timestamp = Regex @"\t\d{4}-\d{2}-\d{2}T[0-9:.]+Z "

let private withoutNoise (line: string) =
    timestamp.Replace(unresolvedStep.Replace(line, "\t", 1), "\t", 1)

/// The log ends in pages of git cleanup, so this filters and takes the head:
/// the tail shows the teardown instead of the failure. A step that fails
/// without saying "error" still has to report something, so the head of the
/// raw log stands in.
let ofLog (log: string list) =
    let matched =
        match log |> List.filter namesAFailure.IsMatch with
        | [] -> log
        | matched -> matched

    matched |> List.truncate 30 |> List.map withoutNoise
