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
