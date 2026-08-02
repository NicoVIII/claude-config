/// Rating a skill from its history log.
///
/// The promotion bars had been patched three times as SKILL.md prose (2e46084,
/// 9060c13, 4543bab); counting entries by event and repo is mechanical, so it
/// lives here. The judgement that used to stay in SKILL.md — whether a rewrite
/// invalidated the entries the count is based on — is now recorded when the
/// rewrite happens, as a big fix, so the ladder can read it.
///
/// Demotion needs no separate rule: a major retro or a big fix ends the trailing
/// streak, which drops the log-derived rating on its own.
module Maturity

open Domain

type Rating =
    | Wip
    | Experimental
    | Usable
    | BattleTested

let private label rating =
    match rating with
    | Wip -> "🚧 WIP"
    | Experimental -> "🧪 Experimental"
    | Usable -> "🟢 Usable"
    | BattleTested -> "🛡️ Battle-tested"

let private nextBar rating =
    match rating with
    | Wip -> Some "🧪 Experimental needs one run logged"
    | Experimental -> Some "🟢 Usable needs ~3 clean-or-minor runs since the last major retro or big fix"
    | Usable -> Some "🛡️ Battle-tested needs ~5 strictly clean runs across 2–3 repos"
    | BattleTested -> None

type private Counts =
    { Runs: int
      /// Runs since the last major retro or big fix. A minor counts toward
      /// 🟢 Usable but not toward 🛡️ Battle-tested, and breaks neither.
      Streak: int
      Spotless: int
      Repos: int }

/// A run happened: the skill was used, then reviewed. A fix records an edit and
/// a compaction a baseline; both share the log without being runs, and counting
/// one as a run is the bug a8531b1 had to patch out of the shell version.
let private isRun (entry: Entry<ChangeEvent>) =
    match entry.Event with
    | Retro _ -> true
    | Fix _ -> false
    | Compacted _ -> false

/// What leaves the 🟢 Usable streak standing. A big fix or a major retro ends it
/// — the skill changed under the runs, or the run went badly, so what came
/// before no longer vouches for what runs now. A small fix leaves the procedure
/// intact, and a compaction moves no rule, so both are transparent here.
let private survivesStreak (entry: Entry<ChangeEvent>) =
    match entry.Event with
    | Retro Clean
    | Retro(Minor _) -> true
    | Retro(Major _) -> false
    | Fix(Small, _) -> true
    | Fix(Big, _) -> false
    | Compacted _ -> true

/// Stricter, and deliberately so: the top rung means five runs that went
/// perfectly against text nobody has had to touch, so *any* fix ends this streak
/// even though a small one leaves the streak above standing.
let private survivesSpotless (entry: Entry<ChangeEvent>) =
    match entry.Event with
    | Retro Clean -> true
    | Retro(Minor _)
    | Retro(Major _) -> false
    | Fix(Small, _)
    | Fix(Big, _) -> false
    | Compacted _ -> true

/// The events are read oldest-to-newest, so they are reversed once here for the
/// trailing counts. The creation baseline is a size datapoint, not a run, and
/// never reaches this: it is not a change at all.
let private count (entries: Entry<ChangeEvent> list) =
    let newestFirst = List.rev entries
    let spotless = newestFirst |> List.takeWhile survivesSpotless |> List.filter isRun

    { Runs = entries |> List.filter isRun |> List.length
      Streak = newestFirst |> List.takeWhile survivesStreak |> List.filter isRun |> List.length
      Spotless = List.length spotless
      Repos = spotless |> List.map (fun entry -> entry.Repo) |> List.distinct |> List.length }

let private rate counts =
    if counts.Runs = 0 then Wip
    elif counts.Spotless >= 5 && counts.Repos >= 2 then BattleTested
    elif counts.Streak >= 3 then Usable
    else Experimental

let private plural count singular many = if count = 1 then singular else many

/// A log with no runs in it cannot argue with the README. Both a skill nobody
/// has run yet and one whose evidence was discarded (e976e0c deleted the run
/// logs) rate 🚧 WIP, and printing that rung beside a higher claim reads as a
/// demotion the reader is meant to propose — which is how six untouched rows
/// came to generate the same rejected proposal at every retro. Absence of
/// evidence is not evidence: the claim is unbacked, not contradicted, so the
/// rung is left unsaid here and the next bar below says what would back it.
let private claimLine (skill: string) (claimed: string) (counts: Counts) rating =
    if counts.Runs = 0 then
        $"{skill}: log holds no runs — README says {claimed}; nothing backs that yet and nothing contradicts it, so leave the row alone"
    else
        $"{skill}: log supports {label rating} — README says {claimed}"

let run (skill: string) =
    let counts = (Layout.history skill).Entries |> count
    let rating = rate counts
    let claimed = Layout.claimedRating skill |> Option.defaultValue "unlisted"

    printfn "%s" (claimLine skill claimed counts rating)

    printfn
        "  %d %s; %d clean-or-minor since the last major retro or big fix (%d strictly clean, across %d %s)"
        counts.Runs
        (plural counts.Runs "run" "runs")
        counts.Streak
        counts.Spotless
        counts.Repos
        (plural counts.Repos "repo" "repos")

    rating |> nextBar |> Option.iter (fun bar -> printfn $"  {bar}")
