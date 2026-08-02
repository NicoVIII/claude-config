/// Entry point: list the current repo's open Dependabot PRs, fan out, print the
/// survey.
module Program

/// A dedicated thread per in-flight PR rather than thread-pool work items: the
/// survey blocks on gh twice per PR and the pool grows too slowly to keep the
/// blocked slots busy. Narrower than gather's because a repo has PRs where the
/// account has repos.
let private parallelism = 4

let private surveyAll (pulls: Wire.Pull list) =
    pulls
    |> List.map (fun pull ->
        async {
            do! Async.SwitchToNewThread()
            return Signals.survey pull
        })
    |> fun work -> Async.Parallel(work, maxDegreeOfParallelism = parallelism)
    |> Async.RunSynchronously
    |> Array.toList

[<EntryPoint>]
let main _ =
    try
        match Signals.openPulls () with
        | [] ->
            printfn "no open dependabot PRs"
            0
        | pulls ->
            // After the PR list, so a repo with nothing waiting says so rather
            // than failing on a merge-method setting no one is about to use.
            let method = Signals.mergeMethod ()
            Render.report method (Signals.resolveSupersedes (surveyAll pulls))
            0
    with Gh.GhFailure message ->
        eprintfn $"survey: {message}"
        1
