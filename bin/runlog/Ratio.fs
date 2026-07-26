/// How far a skill's SKILL.md has grown past its last compaction baseline, and
/// how far that baseline has itself drifted up.
///
/// Growth rather than absolute size: a skill born complex should not be
/// penalised for it, and a universal word limit would be an arbitrary number
/// applied to skills of very different jobs. Only accretion since a skill's own
/// last cleanup counts, which makes the threshold meaningful without having to
/// pick one.
///
/// But measuring against the *last* cleanup hides the ratchet it exists to
/// catch: each compaction becomes the new reference, so a skill that ends every
/// cycle larger than the one before still reports "under the trigger" forever.
/// The floor line is the second datapoint — the same growth measured against
/// the smallest the skill has ever been compacted to. It carries no trigger,
/// because whether a risen floor was earned is a judgement from the run log,
/// not from arithmetic.
///
/// The floor comes only from recorded baselines, never from the first commit:
/// an initial draft is a number nobody weighed, and anchoring to it would
/// penalise a skill for the shape of its first pass forever.
module Ratio

open System
open System.IO
open Domain

/// In tenths, i.e. 1.5x.
let private trigger = 15

/// Agrees with `wc -w`: runs of non-whitespace, split on POSIX whitespace. The
/// baselines already recorded in the logs were produced by wc, so a different
/// notion of a word would silently shift every one of them.
let private wordCount (text: string) =
    text.Split([| ' '; '\t'; '\n'; '\r'; '\f'; '\v' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.length

let private firstCommittedSize (dir: string) =
    Git.run dir [ "log"; "--format=%h"; "--reverse"; "--"; "SKILL.md" ]
    |> Option.bind (fun revisions -> revisions.Split '\n' |> Array.tryHead)
    |> Option.filter (fun first -> first <> "")
    |> Option.bind (fun first -> Git.run dir [ "show"; $"{first}:./SKILL.md" ])
    |> Option.map wordCount

/// Truncated rather than rounded, so the printed figure and the trigger can
/// never disagree: "1.5x" appears exactly when the trigger fires.
let private tenthsOf (now: int) (reference: int) = now * 10 / reference

let private formatTenths tenths = $"{tenths / 10}.{tenths % 10}x"

/// Every compaction the log records, oldest first.
let private recordedBaselines skill =
    Layout.entries skill
    |> List.choose (fun entry -> baselineWords entry.Verdict |> Option.map (fun words -> words, entry.Date))

/// The drift the headline ratio cannot show: how far the reference point itself
/// has moved since the smallest compaction on record. Silent until there is a
/// rise to report, so it appears exactly when there is a ratchet to see.
let private floorLine (now: int) (baselines: (int * DateOnly) list) =
    match baselines with
    | [] -> None
    | baselines ->
        let last, _ = List.last baselines
        let lowest, lowestDate = baselines |> List.minBy fst

        if lowest >= last then
            None
        else
            let cycles = List.length baselines - 1
            // Spelled out rather than left to the current culture, so the date
            // matches the run log it was read from.
            let since = lowestDate.ToString "yyyy-MM-dd"

            Some
                $"  {formatTenths (tenthsOf now lowest)} its lowest baseline of {lowest} ({since}) — the floor has risen {last - lowest} words over {cycles} compactions since"

let run (skill: string) =
    let now = wordCount (File.ReadAllText(Layout.skillFile skill))
    let baselines = recordedBaselines skill

    // The count recorded by the last /skill-compact run, or — before a skill has
    // ever been compacted — its size at first commit.
    let baseline =
        match baselines |> List.tryLast with
        | Some(words, _) -> Some(words, "last compaction")
        | None ->
            Layout.skillDir skill
            |> firstCommittedSize
            |> Option.map (fun words -> words, "first commit")

    match baseline with
    | None -> printfn $"{skill}: {now} words, never committed — no baseline to compare against"
    | Some(words, _) when words = 0 -> fail "baseline is zero words, cannot compare"
    | Some(words, origin) ->
        let tenths = tenthsOf now words

        let verdict =
            if tenths >= trigger then
                "over the 1.5x trigger — run /skill-compact before this grows further"
            else
                "under the 1.5x trigger"

        printfn $"{skill}: {now} words, {formatTenths tenths} its baseline of {words} ({origin}) — {verdict}"
        floorLine now baselines |> Option.iter (printfn "%s")
