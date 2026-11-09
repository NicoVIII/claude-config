/// What gh's check and run lists say about CI, read as the digest's values.
///
/// Pure, and the part of gather with a suite: the queries around it are thin
/// layers over gh, but this is where "still running" was read as "failed".
module Ci

/// A PR needs a decision once a check has concluded badly. One still running
/// has said nothing yet, and surfacing it would put every freshly pushed PR in
/// the ATTENTION block until its checks finish.
let pullFailing (rollup: Wire.Check list option) =
    rollup
    |> Option.defaultValue []
    |> List.exists (fun check ->
        match Conclusion.classify check.conclusion with
        | Conclusion.Failed _ -> true
        | Conclusion.Running
        | Conclusion.Passed _ -> false)

/// The default branch's state: the latest *completed* run of each workflow,
/// worst one wins. Not the newest run outright — a branch carries workflows
/// that have nothing to do with the code (`Dependabot Updates` runs there
/// weekly), and one green run of those hid a red check suite behind it.
///
/// Relies on gh listing runs newest first, which is what makes the first run
/// seen per workflow its latest. A run still in flight is skipped rather than
/// reported: the one before it is the last thing known about that workflow.
let ofRuns (runs: Wire.Run list) =
    let latestCompleted =
        runs
        |> List.choose (fun run ->
            match Conclusion.classify run.conclusion with
            | Conclusion.Running -> None
            | outcome -> Some(run.workflowName, outcome))
        |> List.distinctBy fst
        |> List.map snd

    let failed =
        latestCompleted
        |> List.tryPick (fun outcome ->
            match outcome with
            | Conclusion.Failed conclusion -> Some conclusion
            | Conclusion.Running
            | Conclusion.Passed _ -> None)

    match runs, latestCompleted, failed with
    | [], _, _ -> Domain.NoRuns
    | _, [], _ -> Domain.InProgress
    | _, _, Some conclusion -> Domain.Concluded conclusion
    | _, Conclusion.Passed conclusion :: _, None -> Domain.Concluded conclusion
    // Unreachable: a non-empty list with no failure in it starts with a pass,
    // and Running was filtered out above. Loud rather than a default.
    | _, _ :: _, None -> failwith "Ci.ofRuns: a completed run that neither passed nor failed"
