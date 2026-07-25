/// Running `gh`, and turning its output into typed values or aborting.
///
/// A repo missing from the digest reads exactly like a repo with nothing wrong,
/// so a partial run must never reach stdout. Every unexpected condition here
/// raises GatherFailure, which Program.fs catches in one place and turns into a
/// non-zero exit.
module Gh

open System.Diagnostics
open System.Text.Json

/// gh's list commands default to 30 rows and truncate in silence.
let limit = 200

exception GatherFailure of string

let private fail message = raise (GatherFailure message)

/// Requires every field of every record to be present. A field gh renames or
/// drops then becomes a loud parse error naming it, rather than a null that
/// reads downstream as "nothing open here".
let private jsonOptions = JsonSerializerOptions(RespectRequiredConstructorParameters = true)

let private describe (args: string list) = String.concat " " args

/// Runs a gh invocation, yielding its stdout. A failure whose stderr contains
/// `tolerated` yields None instead — a per-repo feature being switched off is a
/// fact, not an error. Every other failure aborts.
let private run (tolerated: string option) (args: string list) : string option =
    let startInfo =
        ProcessStartInfo("gh", RedirectStandardOutput = true, RedirectStandardError = true)

    args |> List.iter startInfo.ArgumentList.Add

    use proc =
        try
            Process.Start startInfo
        with e ->
            fail $"could not run gh: {e.Message}"

    // Both pipes must be drained concurrently: reading one to the end while the
    // other fills its buffer deadlocks the child.
    let stdout = proc.StandardOutput.ReadToEndAsync()
    let stderr = proc.StandardError.ReadToEndAsync()
    proc.WaitForExit()

    match proc.ExitCode, tolerated with
    | 0, _ -> Some stdout.Result
    | _, Some pattern when stderr.Result.Contains pattern -> None
    | _ -> fail $"gh {describe args} failed: {stderr.Result.Trim()}"

let private parse<'T> (args: string list) (raw: string) : 'T =
    try
        JsonSerializer.Deserialize<'T>(raw, jsonOptions)
    with :? JsonException as e ->
        fail
            $"gh {describe args} returned JSON this script cannot read ({e.Message}); the fields it asks for have probably changed"

/// For queries where no failure is expected and any failure is fatal.
let decode<'T> (args: string list) : 'T =
    run None args |> Option.get |> parse<'T> args

/// For queries where one specific stderr means "this repo has the feature off".
let decodeTolerating<'T> (tolerated: string) (args: string list) : 'T option =
    run (Some tolerated) args |> Option.map (parse<'T> args)

/// A result arriving at the ceiling means the query truncated and the digest is
/// incomplete — which this script exists to prevent.
let guardTruncation (what: string) (rows: 'T list) : 'T list =
    let count = List.length rows

    if count >= limit then
        fail $"{what} returned {count} rows at the {limit} limit; raise limit"

    rows
