/// The survey format, and the only place domain values become text.
///
/// The strings below are the vocabulary SKILL.md classifies on — changing one
/// here is changing that contract.
module Render

open Domain

let private methodText =
    function
    | Squash -> "squash"
    | Merge -> "merge"
    | Rebase -> "rebase"

let private levelText =
    function
    | Patch -> "patch"
    | Minor -> "minor"
    | Major -> "major"
    | Unclear why -> $"unclear({why})"

let private ciText =
    function
    | Green -> "green"
    | Pending -> "pending"
    | Red _ -> "red"

let private mergeText =
    function
    | Clean -> "clean"
    | Dirty -> "dirty"
    | Blocked -> "blocked"
    | Behind -> "behind"
    | Unstable -> "unstable"
    | Draft -> "draft"
    | OtherState other -> other.ToLowerInvariant()

let private filesText =
    function
    | Manifest -> "manifest"
    | LockOnly names -> $"""lock-only({String.concat " " names})"""

let private pr (item: Pr) =
    printfn ""

    printfn
        "PR #%d  LEVEL=%s  CI=%s  MERGE=%s  FILES=%s"
        item.number
        (levelText item.level)
        (ciText item.ci)
        (mergeText item.merge)
        (filesText item.files)

    printfn "  title: %s" item.title

    for bump in item.bumps do
        printfn "  bump: %s %s -> %s (%s)" bump.name bump.fromVersion bump.toVersion (levelText bump.level)

    match item.ci with
    | Red failing ->
        for name, url in failing do
            printfn "  failing: %s%s" name (url |> Option.map (fun url -> $" {url}") |> Option.defaultValue "")
    | Green
    | Pending -> ()

    match item.supersededBy with
    | Some other -> printfn "  superseded-by: #%d" other
    | None -> ()

    for link in item.notes do
        printfn "  notes: %s" link

let report (method: Method) (prs: Pr list) =
    printfn "merge-method: %s" (methodText method)
    prs |> List.sortBy (fun item -> item.number) |> List.iter pr
