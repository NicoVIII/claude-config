/// The digest format, and the only place domain values become text.
///
/// The strings below are the vocabulary SKILL.md documents for the model that
/// reads this output — changing one here is changing that contract.
module Render

open Domain

let private ciText =
    function
    | NoBranch -> "no-branch"
    | NoRuns -> "no-runs"
    | InProgress -> "in-progress"
    | Concluded conclusion -> conclusion

let private alertsText =
    function
    | Disabled -> "disabled"
    | Clean -> "-"
    | Counts counts ->
        counts
        |> List.map (fun (severity, count) -> $"{Severity.name severity}:{count}")
        |> String.concat " "

let private turnText =
    function
    | Unanswered -> "UNANSWERED"
    | TheirsLast -> "THEIRS-LAST"
    | MineLast -> "MINE-LAST"

let private kindText =
    function
    | Review -> "review"
    | IssueThread _ -> "issue"
    | PullRequest _ -> "pr"

let private stateText =
    function
    | Review -> "REVIEW-REQUESTED"
    | IssueThread(turn, replies) -> $"{turnText turn} {replies} replies"
    | PullRequest FailingCi -> "FAILING-CI"
    | PullRequest HumanAuthored -> "human"

let private kindRank =
    function
    | Review -> 0
    | IssueThread _ -> 1
    | PullRequest _ -> 2

let private pad (width: int) (text: string) = text.PadRight width
let private padLeft (width: int) (text: string) = text.PadLeft width

let private widestBy (selector: Attention -> string) (items: Attention list) =
    items |> List.map (selector >> String.length) |> List.max

let private repoTable (reports: Report list) =
    let repoWidth = reports |> List.map (fun report -> report.row.repo.Length) |> List.fold max 4

    let line repo pr issue ci alerts pushed =
        printfn "%s  %s  %s  %s  %s  %s" (pad repoWidth repo) (padLeft 3 pr) (padLeft 5 issue) (pad 11 ci) (pad 24 alerts) pushed

    line "REPO" "PR" "ISSUE" "CI" "ALERTS" "PUSHED"

    for { row = row } in reports do
        line row.repo (string row.pulls) (string row.issues) (ciText row.ci) (alertsText row.alerts) row.pushed

let private attentionBlock (items: Attention list) =
    printfn ""
    printfn "ATTENTION (everything else is cluster-line material)"

    match items with
    | [] -> printfn "none"
    | items ->
        let targetWidth = widestBy (fun item -> item.target) items
        let whoWidth = widestBy (fun item -> item.who) items
        let stateWidth = widestBy (fun item -> stateText item.kind) items

        for item in items do
            printfn
                "%s %s  %s  %s  %s  %s"
                (pad 6 (kindText item.kind))
                (pad targetWidth item.target)
                (pad whoWidth item.who)
                (pad stateWidth (stateText item.kind))
                item.date
                item.title

let digest (reports: Report list) (reviews: Attention list) =
    let reports = reports |> List.sortBy (fun report -> report.row.repo.ToLowerInvariant())

    // Oldest first within a kind: age is what makes these urgent. The sort is
    // stable, so equal dates keep the gathering order — reviews, then each repo
    // alphabetically, PRs before issues within a repo.
    let attention =
        reviews @ (reports |> List.collect (fun report -> report.attention))
        |> List.sortBy (fun item -> kindRank item.kind, item.date)

    repoTable reports
    attentionBlock attention
