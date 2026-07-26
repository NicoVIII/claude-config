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
///
/// Both of those measure words alone, which cannot separate a skill that grew
/// because it covers more from one that grew because it explains the same
/// coverage at greater length. The density line is that second axis: rules
/// stand for coverage, words per rule for how much prose each carries. Measured
/// across this repo, growth has been almost entirely the second kind.
module Ratio

open System
open System.IO
open Domain

/// In tenths, i.e. 1.5x.
let private trigger = 15

/// A markdown list marker: the shape that opens a rule. The trailing space
/// matters — without it a `---` frontmatter delimiter would count as one.
let private isListMarker (token: string) =
    let isNumbered =
        token.EndsWith "." && token.Length > 1 && token[.. token.Length - 2] |> Seq.forall Char.IsDigit

    token = "-" || token = "*" || token = "+" || isNumbered

/// The list items a SKILL.md states, standing in for the rules it carries.
/// Fenced code is skipped: `add-devcontainer` embeds a workflow whose YAML
/// sequence entries are bullets by shape alone, and counting them would have
/// inflated its rule count by a sixth.
let private ruleCount (text: string) =
    let opensListItem (line: string) =
        line.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.tryHead
        |> Option.exists isListMarker

    let step (count, inFence) (line: string) =
        if line.TrimStart().StartsWith "```" then count, not inFence
        elif inFence then count, inFence
        elif opensListItem line then count + 1, inFence
        else count, inFence

    text.Split '\n' |> Array.fold step (0, false) |> fst

let private firstCommittedText (dir: string) =
    Git.run dir [ "log"; "--format=%h"; "--reverse"; "--"; "SKILL.md" ]
    |> Option.bind (fun revisions -> revisions.Split '\n' |> Array.tryHead)
    |> Option.filter (fun first -> first <> "")
    |> Option.bind (fun first -> Git.run dir [ "show"; $"{first}:./SKILL.md" ])

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

let private density (text: string) =
    let words, rules = Layout.wordCount text, ruleCount text

    match rules with
    | 0 -> $"no rules across {words} words"
    | 1 -> $"1 rule of {words} words"
    | rules -> $"{rules} rules at {words / rules} words each"

/// Anchored at the first commit, unlike the floor: nothing is judged against
/// that draft here, its two counts are only compared with today's, so the
/// objection to weighing a first pass does not apply. It is also the one
/// reference a compaction cannot reset, which is what lets this line see a
/// skill the headline has gone blind to. No trigger — what rule count a skill
/// legitimately needs depends on the job it does.
let private densityLine (text: string) (firstCommit: string option) =
    match firstCommit with
    | None -> $"  {density text}"
    | Some first -> $"  {density text}; at the first commit, {density first}"

let run (skill: string) =
    let text = File.ReadAllText(Layout.skillFile skill)
    let now = Layout.wordCount text
    let firstCommit = Layout.skillDir skill |> firstCommittedText
    let baselines = recordedBaselines skill

    // The count recorded by the last /skill-compact run, or — before a skill has
    // ever been compacted — its size at first commit.
    let baseline =
        match baselines |> List.tryLast with
        | Some(words, _) -> Some(words, "last compaction")
        | None -> firstCommit |> Option.map (fun first -> Layout.wordCount first, "first commit")

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

    // Outside the match: density needs neither a baseline nor a trigger, and
    // reports on a skill that has neither.
    printfn "%s" (densityLine text firstCommit)
