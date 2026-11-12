/// Entry point: prints why one PR's CI is red, for the drill-down in SKILL.md.
///
/// The lookup is fixed mechanics — failing check to run id to filtered log — so
/// it lives here rather than as prose a model re-derives, and mis-derives, every
/// run (eec9963 for that argument in full).
module Program

let private failingCheckLink (repo: string) (pr: string) =
    Gh.outputDespiteExit
        [ "pr"
          "checks"
          pr
          "-R"
          repo
          "--json"
          "bucket,link"
          "--jq"
          """first(.[] | select(.bucket == "fail") | .link)""" ]

let private failedLog (repo: string) (run: string) =
    Gh.lines [ "run"; "view"; "-R"; repo; run; "--log-failed" ]

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | [ repo; pr ] ->
        try
            match failingCheckLink repo pr with
            | "" ->
                eprintfn $"no failing check with a run link on %s{repo}#%s{pr}"
                1
            | link ->
                match Excerpt.runId link with
                | None ->
                    eprintfn $"no run id in the failing check's link on %s{repo}#%s{pr}: %s{link}"
                    1
                | Some run ->
                    failedLog repo run |> Excerpt.ofLog |> List.iter (printfn "%s")
                    0
        with Gh.GhFailure message ->
            eprintfn $"failing-log: %s{message}"
            1
    | _ ->
        eprintfn "usage: failing-log.sh <owner/repo> <pr-number>"
        2
