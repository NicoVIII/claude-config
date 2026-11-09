/// A PR's check rollup read as the one CI verdict SKILL.md classifies on.
///
/// Pure, so it carries tests alongside Bumps: `CI=pending` was unreachable for
/// as long as this sat inside Signals, where nothing could exercise it without
/// a PR that happened to be mid-run.
module Checks

open Domain

/// Red before pending: one concluded failure decides the PR whatever is still
/// running beside it. Green needs at least one success: a rollup that is empty
/// or only NEUTRAL and SKIPPED ran nothing, and is NoChecks (#11).
let verdict (rollup: Wire.Check list option) =
    let outcomes =
        rollup
        |> Option.defaultValue []
        |> List.map (fun check -> check, Conclusion.classify check.conclusion)

    let failing =
        outcomes
        |> List.choose (fun (check, outcome) ->
            match outcome with
            | Conclusion.Failed _ -> Some(check.name, check.detailsUrl)
            | Conclusion.Running
            | Conclusion.Passed _ -> None)

    let running =
        outcomes |> List.exists (fun (_, outcome) -> outcome = Conclusion.Running)

    let succeeded =
        outcomes
        |> List.exists (fun (_, outcome) ->
            match outcome with
            | Conclusion.Passed text -> text.ToUpperInvariant() = "SUCCESS"
            | Conclusion.Running
            | Conclusion.Failed _ -> false)

    if not (List.isEmpty failing) then Red failing
    elif running then Pending
    elif succeeded then Green
    else NoChecks
