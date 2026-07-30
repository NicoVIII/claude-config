/// How far a skill's SKILL.md has grown, read straight from its HISTORY.md.
///
/// Every entry carries the word count at the moment it was written, so the log
/// is itself a size time-series and nothing here consults git. That is the whole
/// difference from the version this replaces, which had to reconstruct a first
/// commit to say anything about the long horizon.
///
/// Growth rather than absolute size: a skill born complex should not be
/// penalised for it, and a universal word limit would be an arbitrary number
/// applied to skills of very different jobs. Only accretion since a skill's own
/// last deliberate size counts, which makes the threshold meaningful without
/// having to pick one.
///
/// But measuring against the *last* one hides the ratchet the trigger exists to
/// catch: each compaction becomes the new reference, so a skill that ends every
/// cycle larger than the one before still reports "under the trigger" forever.
/// Two lines answer that. The growth trace prints every recorded size with its
/// delta and the clause that explains it, so the shape of the curve can be read
/// rather than one number trusted. The floor line measures the same growth
/// against the smallest deliberate size on record; it carries no trigger,
/// because whether a risen floor was earned is a judgement from the runs, not
/// from arithmetic.
///
/// Creation counts as a deliberate size here, unlike the first commit the old
/// floor refused to anchor to: it is logged by an author who has just sized the
/// draft against the other skills, not whatever a first pass happened to weigh.
///
/// All of that measures words alone, which cannot separate a skill that grew
/// because it covers more from one that grew because it explains the same
/// coverage at greater length. The density line is that second axis: rules stand
/// for coverage, words per rule for how much prose each carries.
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

/// Truncated rather than rounded, so the printed figure and the trigger can
/// never disagree: "1.5x" appears exactly when the trigger fires.
let private tenthsOf (now: int) (reference: int) = now * 10 / reference

let private formatTenths tenths = $"{tenths / 10}.{tenths % 10}x"

/// What a datapoint says about the skill's size, as against what the trace
/// merely prints. `Created` and `Compaction` are sizes somebody deliberately
/// settled on — the only ones the headline baseline and the floor line may be
/// drawn from; `Changed` is whatever size the skill happened to have when a
/// retro or a fix was logged. Classify by this, never by the label, which exists
/// only to be printed.
type private Kind =
    | Created
    | Compaction
    | Changed

/// A datapoint on the growth curve, oldest first. The clause is what the entry
/// said it did — the trace's only account of *why* a size moved.
type private Point =
    { Date: DateOnly
      Label: string
      Clause: string
      Words: int
      Kind: Kind }

/// Listed case by case rather than with a wildcard, per the convention Domain.fs
/// documents: a new event must fail to compile here rather than silently render
/// blank and count as a non-anchor.
let private changePoint (entry: Entry<ChangeEvent>) =
    let label, clause, kind =
        match entry.Event with
        | Retro Clean -> "retro clean", "", Changed
        | Retro(Minor clause) -> "retro minor", clause, Changed
        | Retro(Major clause) -> "retro major", clause, Changed
        | Fix(Small, clause) -> "fix small", clause, Changed
        | Fix(Big, clause) -> "fix big", clause, Changed
        | Compacted clause -> "compacted", clause, Compaction

    { Date = entry.Date
      Label = label
      Clause = clause
      Words = entry.Words
      Kind = kind }

/// Oldest first, creation ahead of every change since — the order the deltas and
/// the last-compaction lookup both rely on.
let private allPoints (history: History) =
    let creation =
        history.Creation
        |> Option.map (fun entry ->
            { Date = entry.Date
              Label = "created"
              Clause = ""
              Words = entry.Words
              Kind = Created })
        |> Option.toList

    creation @ (history.Entries |> List.map changePoint)

let private renderPoint (previous: int option) (point: Point) =
    let delta =
        match previous with
        | None -> ""
        | Some before ->
            match point.Words - before with
            | diff when diff >= 0 -> $" (+{diff})"
            | diff -> $" ({diff})"

    let reason =
        match point.Clause with
        | "" -> ""
        | clause -> $" — {clause}"

    let date = point.Date.ToString "yyyy-MM-dd"

    $"    {date} · {point.Label} · {point.Words} words{delta}{reason}"

let private renderTrace (points: Point list) =
    let rec walk previous points =
        match points with
        | [] -> []
        | point :: rest -> renderPoint previous point :: walk (Some point.Words) rest

    walk None points

/// The last deliberate size: the most recent compaction, else the logged
/// creation baseline, else — for a log that has neither — the earliest entry on
/// record, which is at least a size this skill really had.
let private selectBaseline (points: Point list) =
    match points |> List.filter (fun point -> point.Kind = Compaction) |> List.tryLast with
    | Some point -> Some(point.Words, "last compaction")
    | None ->
        match points with
        | [] -> None
        | first :: _ ->
            match first.Kind with
            | Created -> Some(first.Words, "creation")
            | Compaction
            | Changed -> Some(first.Words, "earliest logged entry")

/// The deliberate sizes — creation plus every compaction — oldest first. Retro
/// and fix entries do not count: they record that the skill changed, not that
/// its size was settled on.
let private anchorPoints (points: Point list) =
    points
    |> List.filter (fun point ->
        match point.Kind with
        | Created
        | Compaction -> true
        | Changed -> false)

/// The anchors recorded after the lowest one. Split on the *earliest* anchor
/// holding the lowest count, so a tie counts from the first time that size was
/// reached.
let private cyclesSince (anchors: Point list) (lowest: Point) =
    anchors
    |> List.skipWhile (fun point -> point.Words <> lowest.Words)
    |> List.skip 1
    |> List.length

let private plural count singular many = if count = 1 then singular else many

/// The drift the headline ratio cannot show: how far the reference point itself
/// has moved since the smallest deliberate size on record. Silent until there is
/// a rise to report, so it appears exactly when there is a ratchet to see.
let private floorLine (now: int) (anchors: Point list) =
    match anchors with
    | []
    | [ _ ] -> None
    | _ ->
        let latest = List.last anchors
        let lowest = anchors |> List.minBy (fun point -> point.Words)

        if lowest.Words >= latest.Words then
            None
        else
            let cycles = cyclesSince anchors lowest
            let cyclesWord = plural cycles "cycle" "cycles"
            let risen = latest.Words - lowest.Words
            // Spelled out rather than left to the current culture, so the date
            // matches the log it was read from.
            let since = lowest.Date.ToString "yyyy-MM-dd"

            Some
                $"  {formatTenths (tenthsOf now lowest.Words)} its lowest baseline of {lowest.Words} ({since}) — the floor has risen {risen} words over {cycles} {cyclesWord} since"

/// No trigger — what rule count a skill legitimately needs depends on the job it
/// does. It also needs no baseline, so it reports on a skill that has none.
let private densityLine (text: string) =
    let words, rules = Layout.wordCount text, ruleCount text

    match rules with
    | 0 -> $"  no rules across {words} words"
    | 1 -> $"  1 rule of {words} words"
    | rules -> $"  {rules} rules at {words / rules} words each"

let run (skill: string) =
    let text = File.ReadAllText(Layout.skillFile skill)
    let now = Layout.wordCount text
    let points = Layout.history skill |> allPoints

    match selectBaseline points with
    | None -> printfn $"{skill}: {now} words, never logged — no baseline to compare against"
    | Some(words, origin) ->
        let tenths = tenthsOf now words

        let verdict =
            if tenths >= trigger then
                "over the 1.5x trigger — run /skill-compact before this grows further"
            else
                "under the 1.5x trigger"

        printfn $"{skill}: {now} words, {formatTenths tenths} its baseline of {words} ({origin}) — {verdict}"

    if not (List.isEmpty points) then
        printfn "  growth trace:"
        renderTrace points |> List.iter (printfn "%s")

    floorLine now (anchorPoints points) |> Option.iter (printfn "%s")
    printfn "%s" (densityLine text)
