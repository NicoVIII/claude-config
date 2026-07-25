/// Rating a skill from its run log.
///
/// The promotion bars had been patched three times as SKILL.md prose (2e46084,
/// 9060c13, 4543bab); counting entries by verdict and repo is mechanical, so it
/// lives here. What stays in SKILL.md is the judgement a script cannot make:
/// whether a rewrite invalidated the entries the count is based on.
///
/// Demotion needs no separate rule — a friction entry ends the trailing streak,
/// which drops the log-derived rating on its own.
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
    | Experimental -> Some "🟢 Usable needs ~3 clean-or-minor entries since the last friction"
    | Usable -> Some "🛡️ Battle-tested needs ~5 strictly clean entries across 2–3 repos"
    | BattleTested -> None

type private Counts =
    { Runs: int
      /// Entries since the last friction. A `minor:` entry counts toward
      /// 🟢 Usable but not toward 🛡️ Battle-tested, and does not break the streak.
      Streak: int
      Spotless: int
      Repos: int }

let private count (runs: Entry list) =
    let trailing =
        runs |> List.rev |> List.takeWhile (fun entry -> not (isFriction entry.Verdict))

    let spotless = trailing |> List.filter (fun entry -> isClean entry.Verdict)

    { Runs = List.length runs
      Streak = List.length trailing
      Spotless = List.length spotless
      Repos = spotless |> List.map (fun entry -> entry.Repo) |> List.distinct |> List.length }

let private rate counts =
    if counts.Runs = 0 then Wip
    elif counts.Spotless >= 5 && counts.Repos >= 2 then BattleTested
    elif counts.Streak >= 3 then Usable
    else Experimental

let run (skill: string) =
    let counts =
        Layout.entries skill |> List.filter (fun entry -> isRun entry.Verdict) |> count

    let rating = rate counts
    let claimed = Layout.claimedRating skill |> Option.defaultValue "unlisted"

    printfn $"{skill}: log supports {label rating} — README says {claimed}"

    printfn
        "  %d %s; %d since the last friction (%d strictly clean, across %d repos)"
        counts.Runs
        (if counts.Runs = 1 then "entry" else "entries")
        counts.Streak
        counts.Spotless
        counts.Repos

    rating |> nextBar |> Option.iter (fun bar -> printfn $"  {bar}")
