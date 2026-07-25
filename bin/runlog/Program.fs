/// Entry point: dispatch, and turn any failure into a non-zero exit.
module Program

open Domain

[<EntryPoint>]
let main argv =
    try
        match List.ofArray argv with
        | [ "log"; skill; verdict ] ->
            Log.run skill verdict
            0
        | [ "maturity"; skill ] ->
            Maturity.run skill
            0
        | [ "ratio"; skill ] ->
            Ratio.run skill
            0
        | "log" :: _ ->
            eprintfn "usage: runlog log <skill> <verdict>"
            eprintfn "  e.g. runlog log grilling 'minor: footer placement guessed'"
            1
        | ("maturity" | "ratio") :: _ ->
            eprintfn $"usage: runlog {argv[0]} <skill>"
            1
        | command :: _ -> fail $"unknown command '{command}' (expected log, maturity or ratio)"
        | [] ->
            eprintfn "usage: runlog <log|maturity|ratio> <skill> [verdict]"
            1
    with RunlogFailure message ->
        eprintfn $"runlog: {message}"
        1
