/// The five queries that make up the digest.
///
/// Each decides what is worth a human's attention and what only needs counting.
/// Rows that only feed a count (self-authored issues, green bot PRs) are counted
/// here and never surfaced individually.
module Signals

open Domain

let private titleWidth = 64

let private day (timestamp: string) = timestamp.Substring(0, 10)

let private clip (width: int) (text: string) =
    if text.Length <= width then
        text
    else
        text.Substring(0, width - 1) + "…"

/// Counts every open PR but surfaces only those needing a decision: failing CI,
/// or a human author. Green bot PRs are cluster-line material.
let private pulls (repo: string) =
    let rows =
        Gh.decode<Wire.Pull list>
            [ "pr"
              "list"
              "-R"
              repo
              "--limit"
              string Gh.limit
              "--json"
              "number,title,author,statusCheckRollup,updatedAt" ]
        |> Gh.guardTruncation $"{repo} PRs"

    let attention =
        rows
        |> List.choose (fun pull ->
            let failing =
                pull.statusCheckRollup
                |> Option.defaultValue []
                |> List.exists (fun check ->
                    match check.conclusion with
                    // Absent = still running. Anything not known-good is a
                    // decision for a human, including conclusions GitHub
                    // adds after this was written.
                    | None -> false
                    | Some("SUCCESS" | "NEUTRAL" | "SKIPPED") -> false
                    | Some _ -> true)

            match failing, pull.author.isBot with
            | false, true -> None
            | _ ->
                Some
                    { kind = PullRequest(if failing then FailingCi else HumanAuthored)
                      target = $"{repo}#{pull.number}"
                      who = pull.author.login
                      date = day pull.updatedAt
                      title = clip titleWidth pull.title })

    List.length rows, attention

/// Only an issue someone else opened can put me on the hook, and who spoke last
/// needs the thread — updatedAt does not show it, since a label edit bumps that
/// too. That costs one call per external issue, which is a handful.
let private issues (me: string) (repo: string) =
    let rows =
        Gh.decodeTolerating<Wire.Issue list>
            "has disabled issues"
            [ "issue"; "list"; "-R"; repo; "--limit"; string Gh.limit; "--json"; "number,author" ]
        |> Option.defaultValue []
        |> Gh.guardTruncation $"{repo} issues"

    let attention =
        rows
        |> List.filter (fun issue -> issue.author.login <> me)
        |> List.map (fun issue ->
            let thread =
                Gh.decode<Wire.IssueDetail>
                    [ "issue"; "view"; string issue.number; "-R"; repo; "--json"; "title,author,createdAt,comments" ]

            let turn =
                match List.tryLast thread.comments with
                | None -> Unanswered
                | Some last when last.author.login = me -> MineLast
                | Some _ -> TheirsLast

            { kind = IssueThread(turn, List.length thread.comments)
              target = $"{repo}#{issue.number}"
              who = thread.author.login
              date = day thread.createdAt
              title = clip titleWidth thread.title })

    List.length rows, attention

/// The branch is resolved per repo because `gh run list --limit 1` without one
/// returns the newest run on any branch, which reads healthy while main is red.
let private ci (repo: string) (branch: Wire.BranchRef option) =
    match branch with
    | None -> NoBranch
    | Some branch ->
        let runs =
            Gh.decode<Wire.Run list>
                [ "run"; "list"; "-R"; repo; "--branch"; branch.name; "--limit"; "1"; "--json"; "conclusion" ]

        match runs with
        | [] -> NoRuns
        | run :: _ ->
            // gh reports a running workflow as an empty conclusion rather than a
            // null one, so matching None alone leaves the column blank and the
            // reader unable to tell "still running" from "no data".
            match run.conclusion with
            | None
            | Some "" -> InProgress
            | Some conclusion -> Concluded conclusion

/// Severities rather than a bare count: one critical outranks a pile of lows. A
/// 403 carrying "Dependabot alerts are disabled" is that repo's setting. gh
/// appends a boilerplate hint about admin:repo_hook scope to any 403 — noise,
/// disproved by the other repos in the same run answering with data.
let private alerts (repo: string) =
    let path = $"repos/{repo}/dependabot/alerts?state=open"

    match Gh.decodeTolerating<Wire.Alert list> "Dependabot alerts are disabled" [ "api"; path ] with
    | None -> Disabled
    | Some [] -> Clean
    | Some rows ->
        rows
        |> List.countBy (fun alert -> Severity.parse alert.securityAdvisory.severity)
        |> List.sortBy (fun (severity, _) -> Severity.rank severity, Severity.name severity)
        |> Counts

let gatherRepo (me: string) (repo: Wire.Repo) =
    let fullName = $"{repo.owner.login}/{repo.name}"
    let pullCount, pullAttention = pulls fullName
    let issueCount, issueAttention = issues me fullName

    { row =
        { repo = fullName
          pulls = pullCount
          issues = issueCount
          ci = ci fullName repo.defaultBranchRef
          alerts = alerts fullName
          pushed = day repo.pushedAt }
      attention = pullAttention @ issueAttention }

/// `gh repo list` only enumerates repos I own, so a review request in someone
/// else's or an org's repo never reaches gatherRepo. Weight 1 by definition.
let reviewRequests () =
    Gh.decode<Wire.SearchPull list>
        [ "search"
          "prs"
          "--review-requested=@me"
          "--state=open"
          "--limit"
          string Gh.limit
          "--json"
          "number,title,repository,author,updatedAt" ]
    |> Gh.guardTruncation "review requests"
    |> List.map (fun pull ->
        { kind = Review
          target = $"{pull.repository.nameWithOwner}#{pull.number}"
          who = pull.author.login
          date = day pull.updatedAt
          title = clip titleWidth pull.title })
