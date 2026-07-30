/// The history-log format, and the event taxonomy every reader shares.
///
/// This module is why the helper is F# and not shell. The log had three shell
/// parsers and they disagreed twice: `maturity` counted a `compacted:` baseline
/// as a run (a8531b1), and `ratio` matched that phrase inside unrelated prose.
/// Both are missing-case bugs. So the events are a closed tree, and every match
/// over it lists all of `ChangeEvent`'s cases rather than falling back to a
/// wildcard: adding a fourth event is then a compile error at each site that has
/// to decide what it means, which is the whole point of the port. A wildcard
/// *inside* a case is fine where the answer cannot depend on the payload — a
/// retro is a run whatever its grade — because extending `RetroVerdict` could
/// not change that answer.
module Domain

open System

exception SkillRefinerFailure of string

/// A caller who spelled the command wrong, as against a run that went wrong.
/// Kept apart so the message prints as usage rather than wearing the binary's
/// name like a fault in the log.
exception UsageError of string

let fail message = raise (SkillRefinerFailure message)

let usage message = raise (UsageError message)

/// How the run itself went, by the damage it did: `Minor` for issues that did
/// not much hinder a good result, `Major` for big ones up to an abort. It grades
/// the run and nothing else — whether the edits it led to invalidate the runs
/// before them is `Fix`'s question, and answering it twice is what let one mild
/// correction cost three runs of progress.
///
/// `Clean` carries no clause by construction: a run with nothing to report can
/// only fill one with filler, and filler is noise in the corpus a later retro
/// searches for recurrences.
type RetroVerdict =
    | Clean
    | Minor of clause: string
    | Major of clause: string

/// Whether the edit leaves the runs before it standing as evidence. `Big` means
/// it does not — a mechanism replaced, a step added or removed, a contract
/// changed; `Small` means the procedure is intact and only its wording moved.
/// This is the judgement `/skill-retro` used to leave to its reader ("a clean
/// entry logged against machinery a later retro replaced doesn't count"), stated
/// once at the time the edit is made, when it is actually known.
type FixSize =
    | Small
    | Big

/// What a run and its aftermath record. A retro pass that edits the skill writes
/// two of these — the retro for how the run went, the fix for what the edit did
/// — which is what makes an unfixed finding visible: a clause with no fix beside
/// it is the deferral, and a later retro matches a recurrence against it. One
/// line saying both is what used to hide it.
type ChangeEvent =
    | Retro of RetroVerdict
    | Fix of size: FixSize * clause: string
    /// Not a run: `/skill-compact`'s new baseline for the growth ratio. The
    /// clause says what the pass cut, which is the growth trace's only account
    /// of why a size fell.
    | Compacted of clause: string

/// The origin baseline: a skill's SKILL.md size when its log began. It is the
/// first line of a HISTORY.md and appears nowhere else, so it is modelled apart
/// from `ChangeEvent` — "creation is the first line, or the log is malformed" is
/// then a parse concern the reader settles once, not a case every fold over the
/// changes has to re-reject.
type CreationEvent = CreationEvent

type Event =
    | Creation of CreationEvent
    | Change of ChangeEvent

/// `Words` is the SKILL.md's size when the entry was written, so growth is a
/// recorded fact rather than something a later reader has to infer from the
/// file's shape. Every entry carries one and none is optional: the log is a size
/// time-series, and an entry opting out would be a hole in it.
type Entry<'event> =
    { Date: DateOnly
      Repo: string
      Words: int
      Event: 'event }

/// A parsed log: the optional origin baseline, then the changes after it. A log
/// started before creation seeding existed simply has `None` here.
type History =
    { Creation: Entry<CreationEvent> option
      Entries: Entry<ChangeEvent> list }

let heading = "# Skill History"

let private separator = " · "

let private dateFormat = "yyyy-MM-dd"

let private renderRetro verdict =
    match verdict with
    | Clean -> "retro clean"
    | Minor clause -> $"retro minor: {clause}"
    | Major clause -> $"retro major: {clause}"

let private renderFix size clause =
    match size with
    | Small -> $"fix small: {clause}"
    | Big -> $"fix big: {clause}"

let renderChangeEvent event =
    match event with
    | Retro verdict -> renderRetro verdict
    | Fix(size, clause) -> renderFix size clause
    | Compacted clause -> $"compacted: {clause}"

/// Mirrors the command that writes it — `log retro minor '<clause>'` lands as
/// `retro minor: <clause>` — so a line in the log names the command that made
/// it, and one delimiter convention covers the whole vocabulary.
let renderEvent event =
    match event with
    | Creation CreationEvent -> "created"
    | Change event -> renderChangeEvent event

/// None when the affix is absent or brackets nothing: a verdict whose clause is
/// blank says nothing a later retro could match a recurrence against.
let private clauseAfter (prefix: string) (text: string) =
    if text.StartsWith prefix then
        match text.Substring(prefix.Length).Trim() with
        | "" -> None
        | clause -> Some clause
    else
        None

/// The one definition of the vocabulary, mirroring `renderChangeEvent` case for
/// case. The writer validates through here too, so it cannot accept a form the
/// readers do not recognise — in the shell version those were two separate
/// regexes.
let parseChangeEvent (text: string) : ChangeEvent option =
    match text with
    | "retro clean" -> Some(Retro Clean)
    | _ when text.StartsWith "retro minor: " -> clauseAfter "retro minor: " text |> Option.map (Minor >> Retro)
    | _ when text.StartsWith "retro major: " -> clauseAfter "retro major: " text |> Option.map (Major >> Retro)
    | _ when text.StartsWith "fix small: " -> clauseAfter "fix small: " text |> Option.map (fun c -> Fix(Small, c))
    | _ when text.StartsWith "fix big: " -> clauseAfter "fix big: " text |> Option.map (fun c -> Fix(Big, c))
    | _ when text.StartsWith "compacted: " -> clauseAfter "compacted: " text |> Option.map Compacted
    | _ -> None

let private parseEvent (text: string) : Event option =
    match text with
    | "created" -> Some(Creation CreationEvent)
    | _ -> parseChangeEvent text |> Option.map Change

/// Validates and normalises a clause a log command was handed. Both refusals are
/// about surviving the round trip through one line of the log: a blank clause
/// parses back as no entry at all, and an embedded newline would write a second
/// line that aborts every reader of the file.
let clause (text: string) =
    let trimmed = text.Trim()

    if trimmed = "" then
        fail "the clause is empty — say what happened, so a later retro can match a recurrence against it"
    elif trimmed.Contains '\n' || trimmed.Contains '\r' then
        fail "the clause spans more than one line, and an entry is one line"

    trimmed

/// The word count sits before the event, not after it, so the event stays last:
/// its clause is free text a retro writes and may hold the separator, which a
/// trailing field would have to guess its way back past.
let renderEntry (renderEvent: 'event -> string) (entry: Entry<'event>) =
    [ entry.Date.ToString dateFormat; entry.Repo; $"{entry.Words} words"; renderEvent entry.Event ]
    |> String.concat separator

let renderCreationEntry (entry: Entry<CreationEvent>) = renderEntry (Creation >> renderEvent) entry

let renderChangeEntry (entry: Entry<ChangeEvent>) = renderEntry (Change >> renderEvent) entry

let renderKnownEntry (entry: Entry<Event>) = renderEntry renderEvent entry

let private parseDate (text: string) =
    match DateOnly.TryParseExact(text, dateFormat) with
    | true, date -> Some date
    | _ -> None

/// A count, and only when it is exactly one. Zero or less is not a size: a
/// negative used to reach `ratio` as a baseline and divide into a -1.0x that
/// printed as "under the trigger" — a wrong answer wearing the shape of a
/// reassuring one (a1d87eb).
let private parseWords (text: string) =
    if text.EndsWith " words" then
        match Int32.TryParse(text.Substring(0, text.Length - " words".Length)) with
        | true, words when words > 0 -> Some words
        | _ -> None
    else
        None

/// An entry, or an abort. There is no "not an entry" answer: the file is
/// tool-owned, so anything in it that is not the heading or a blank line was
/// meant to be an entry, and quietly skipping a broken one is what leaves the
/// counters wrong in the way that is hardest to notice.
let parseLine (line: string) : Entry<Event> =
    // Bounded so the event keeps the rest of the line: date, repo and the count
    // cannot contain the separator, but the clause is free text a retro writes,
    // and an unbounded split turned one stray " · " in it into a parse abort
    // that took down every reader of that skill's whole log.
    match line.Split(separator, 4, StringSplitOptions.None) with
    | [| date; repo; words; event |] ->
        match parseDate date, parseWords words, parseEvent event with
        | Some date, Some words, Some event ->
            { Date = date
              Repo = repo
              Words = words
              Event = event }
        | None, _, _ -> fail $"history holds a malformed date: {line}"
        | _, None, _ -> fail $"history holds a malformed word count: {line}"
        | _, _, None -> fail $"history holds an event no reader knows: {line}"
    | _ -> fail $"history entry is not `date{separator}repo{separator}N words{separator}event`: {line}"

let private retype (event: 'b) (entry: Entry<'a>) : Entry<'b> =
    { Date = entry.Date
      Repo = entry.Repo
      Words = entry.Words
      Event = event }

/// Every line after the first must be a change: a `created` here is one out of
/// first position, which the log's shape forbids.
let private changesOnly (entries: Entry<Event> list) =
    entries
    |> List.map (fun entry ->
        match entry.Event with
        | Change event -> retype event entry
        | Creation _ -> fail "created must be the first line of the history")

/// The heading is checked rather than skipped past: dropping two lines blind
/// would read whatever followed a wrong first line as entries.
let parseHistory (text: string) : History =
    match text.Split '\n' |> Array.toList with
    | first :: rest when first.TrimEnd() = heading ->
        let entries =
            rest |> List.filter (fun line -> line.Trim() <> "") |> List.map parseLine

        match entries with
        | [] -> { Creation = None; Entries = [] }
        | first :: rest ->
            match first.Event with
            | Creation event ->
                { Creation = Some(retype event first)
                  Entries = changesOnly rest }
            | Change _ ->
                { Creation = None
                  Entries = changesOnly entries }
    | _ -> fail $"history does not start with `{heading}`"
