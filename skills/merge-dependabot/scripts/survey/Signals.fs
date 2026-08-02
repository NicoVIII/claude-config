/// The queries that turn the current repo's open Dependabot PRs into facts.
module Signals

open System.IO
open System.Text.RegularExpressions
open Domain

let private repoQuery =
    [ "repo"; "view"; "--json"; "squashMergeAllowed,mergeCommitAllowed,rebaseMergeAllowed" ]

/// Squash, then merge, then rebase — first one the repo allows. A repo allowing
/// none would leave every safe PR unmergeable, which is a setup problem worth
/// stopping on rather than reporting per PR.
let mergeMethod () =
    let allowed = Gh.decode<Wire.MergeMethods> repoQuery

    if allowed.squashMergeAllowed then Squash
    elif allowed.mergeCommitAllowed then Merge
    elif allowed.rebaseMergeAllowed then Rebase
    else Gh.fail "this repo allows no merge method; nothing here can be merged"

/// Deliberately without `body`: Dependabot bodies embed whole release notes, and
/// one repo's bulk list came back at 106KB of them (fd9763a). Bodies are fetched
/// one PR at a time instead.
let openPulls () =
    Gh.decode<Wire.Pull list>
        [ "pr"
          "list"
          "--author"
          "app/dependabot"
          "--state"
          "open"
          "--limit"
          string Gh.limit
          "--json"
          "number,title,statusCheckRollup" ]
    |> Gh.guardTruncation "dependabot PRs"

let private ci (rollup: Wire.Check list option) =
    let checks = Option.defaultValue [] rollup

    let failing =
        checks
        |> List.filter (fun check ->
            match check.conclusion with
            | None -> false
            | Some("SUCCESS" | "NEUTRAL" | "SKIPPED") -> false
            | Some _ -> true)

    if not (List.isEmpty failing) then
        Red(failing |> List.map (fun check -> check.name, check.detailsUrl))
    elif checks |> List.exists (fun check -> check.conclusion.IsNone) then
        Pending
    else
        Green

/// Named rather than derived: every ecosystem spells its manifest differently
/// (a github-actions bump edits a workflow, a docker bump a Dockerfile), so a
/// manifest allowlist would report half the ecosystems as lockfile-only. A lock
/// file this does not know lands in Manifest, and Render prints the file names
/// alongside the verdict so the misreading is visible rather than silent.
let private lockfiles =
    set
        [ "package-lock.json"
          "npm-shrinkwrap.json"
          "yarn.lock"
          "pnpm-lock.yaml"
          "bun.lockb"
          "Cargo.lock"
          "poetry.lock"
          "uv.lock"
          "Pipfile.lock"
          "composer.lock"
          "Gemfile.lock"
          "go.sum"
          "packages.lock.json"
          "paket.lock"
          "gradle.lockfile"
          "mix.lock"
          "pubspec.lock"
          "flake.lock" ]

let private files (number: int) =
    match Gh.lines [ "pr"; "diff"; string number; "--name-only" ] with
    | [] -> Gh.fail $"PR #{number} has an empty diff"
    | changed ->
        if changed |> List.forall (fun path -> Set.contains (Path.GetFileName path) lockfiles) then
            LockOnly(changed |> List.map Path.GetFileName)
        else
            Manifest

/// Dependabot bodies are markdown with raw HTML mixed in, so a URL ends at a
/// quote or angle bracket as readily as at whitespace — without those, the
/// first real run captured `.../releases">actions/setup-dotnet's`.
let private notesPattern =
    Regex("""https://[^\s)\]"'<>]*(?:releases|changelog|CHANGELOG)[^\s)\]"'<>]*""", RegexOptions.IgnoreCase)

/// Release notes if the body links any, else the project homepages off the
/// dependency lines — the plain link grep only ever surfaced those homepages on
/// one repo's PRs (6a89d40), so both are worth carrying.
let private notes (body: string) (bumps: Bump list) =
    match notesPattern.Matches body |> Seq.map (fun hit -> hit.Value) |> Seq.distinct |> Seq.truncate 3 |> Seq.toList with
    | [] -> bumps |> List.choose (fun bump -> bump.home) |> List.distinct |> List.truncate 3
    | links -> links

/// GitHub computes mergeability lazily: the first request for it only schedules
/// the work and answers UNKNOWN. Asking once per PR is what SKILL.md used to
/// prescribe, and it reported UNKNOWN for every PR in the first repo this was
/// run against — so ask again until GitHub has an answer.
let private mergeStateAttempts = 4
let private mergeStateWait = 1500

let rec private mergeState attempt (number: int) =
    let answer =
        (Gh.decode<Wire.Mergeability> [ "pr"; "view"; string number; "--json"; "mergeStateStatus" ])
            .mergeStateStatus

    if answer = "UNKNOWN" && attempt < mergeStateAttempts then
        System.Threading.Thread.Sleep mergeStateWait
        mergeState (attempt + 1) number
    else
        MergeState.parse answer

/// One PR's facts, minus supersession — that one needs every other PR's bumps
/// and so is resolved once they are all in.
let survey (pull: Wire.Pull) =
    let body =
        (Gh.decode<Wire.Body> [ "pr"; "view"; string pull.number; "--json"; "body" ]).body

    let bumps = Bumps.parse body pull.title

    { number = pull.number
      title = pull.title
      level = Bumps.worst bumps
      bumps = bumps
      merge = mergeState 1 pull.number
      ci = ci pull.statusCheckRollup
      files = files pull.number
      supersededBy = None
      notes = notes body bumps }

let resolveSupersedes (prs: Pr list) =
    let bumpsByPr = prs |> List.map (fun pr -> pr.number, pr.bumps)

    prs
    |> List.map (fun pr ->
        let others = bumpsByPr |> List.filter (fun (number, _) -> number <> pr.number)

        { pr with
            supersededBy = Bumps.supersededBy others pr.bumps })
