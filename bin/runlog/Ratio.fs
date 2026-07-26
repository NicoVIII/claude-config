/// How far a skill's SKILL.md has grown past its last compaction baseline.
///
/// Growth rather than absolute size: a skill born complex should not be
/// penalised for it, and a universal word limit would be an arbitrary number
/// applied to skills of very different jobs. Only accretion since a skill's own
/// last cleanup counts, which makes the threshold meaningful without having to
/// pick one.
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

let run (skill: string) =
    let now = wordCount (File.ReadAllText(Layout.skillFile skill))

    // The count recorded by the last /skill-compact run, or — before a skill has
    // ever been compacted — its size at first commit.
    let baseline =
        match Layout.entries skill |> List.rev |> List.tryPick (fun entry -> baselineWords entry.Verdict) with
        | Some words -> Some(words, "last compaction")
        | None ->
            Layout.skillDir skill
            |> firstCommittedSize
            |> Option.map (fun words -> words, "first commit")

    match baseline with
    | None -> printfn $"{skill}: {now} words, never committed — no baseline to compare against"
    | Some(words, _) when words = 0 -> fail "baseline is zero words, cannot compare"
    | Some(words, origin) ->
        // Truncated rather than rounded, so the printed figure and the trigger
        // can never disagree: "1.5x" appears exactly when the trigger fires.
        let tenths = now * 10 / words

        let verdict =
            if tenths >= trigger then
                "over the 1.5x trigger — run /skill-compact before this grows further"
            else
                "under the 1.5x trigger"

        printfn $"{skill}: {now} words, {tenths / 10}.{tenths % 10}x its baseline of {words} ({origin}) — {verdict}"
