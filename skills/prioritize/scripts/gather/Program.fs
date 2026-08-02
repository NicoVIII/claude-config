/// Entry point: resolve who I am, list my repos, fan out, print the digest.
module Program

/// A dedicated thread per in-flight repo rather than thread-pool work items: the
/// signal functions block on gh, and the pool grows too slowly to keep eight
/// blocked slots busy. This is ~90 network round trips and they dominate wall
/// time, so the width matters more than the thread cost.
let private parallelism = 8

let private gatherAll (me: string) (repos: Wire.Repo list) =
    repos
    |> List.map (fun repo ->
        async {
            do! Async.SwitchToNewThread()
            return Signals.gatherRepo me repo
        })
    |> fun work -> Async.Parallel(work, maxDegreeOfParallelism = parallelism)
    |> Async.RunSynchronously
    |> Array.toList

[<EntryPoint>]
let main _ =
    try
        let user = Gh.decode<Wire.User> [ "api"; "user" ]

        let repos =
            Gh.decode<Wire.Repo list>
                [ "repo"
                  "list"
                  "--no-archived"
                  "--source"
                  "--limit"
                  string Gh.limit
                  "--json"
                  "name,owner,pushedAt,defaultBranchRef" ]
            |> Gh.guardTruncation "repo list"

        if List.isEmpty repos then
            raise (Gh.GhFailure "gh repo list returned nothing")

        Render.digest (gatherAll user.login repos) (Signals.reviewRequests ())
        0
    with Gh.GhFailure message ->
        eprintfn $"gather: {message}"
        1
