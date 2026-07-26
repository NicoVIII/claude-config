/// The run-log format, and the verdict taxonomy every reader shares.
///
/// This type is why the helper is F# and not shell. The log had three shell
/// parsers and they disagreed twice: `maturity` counted a `compacted:` baseline
/// as a run (a8531b1), and `ratio` matched that phrase inside unrelated prose.
/// Both are missing-case bugs. So the verdicts are a closed union, and every
/// match over it below lists all five cases rather than falling back to a
/// wildcard — adding a sixth verdict is then a compile error at each site that
/// has to decide what it means, which is the whole point of the port.
module Domain

open System

exception RunlogFailure of string

let fail message = raise (RunlogFailure message)

type Verdict =
    | Clean
    | Minor of clause: string
    | Friction of clause: string
    /// Not a run either: friction seen and deliberately not fixed, waiting for a
    /// second sighting to prove it is worth SKILL.md's permanent context cost.
    /// It rides alongside the run's own verdict rather than replacing it, since
    /// the run that defers one finding has usually just fixed another — forcing
    /// one line to say both is what would hide it. Name the mechanism, not the
    /// symptom: a later retro can only match a recurrence against wording it
    /// recognises. Deliberately weightless on the ladder — a deferral that cost
    /// the rating would be a deferral nobody writes.
    | Deferred of clause: string
    /// Not a run: /skill-compact's baseline for the growth ratio.
    | Compacted of words: int

/// `Words` is the SKILL.md's size when the entry was written, so growth is a
/// recorded fact rather than something a later reader has to infer from the
/// file's shape. `None` on entries written before the field existed, and on
/// compactions, whose verdict already states the number — nothing invents one.
type Entry =
    { Date: DateOnly
      Repo: string
      Words: int option
      Verdict: Verdict }

let private separator = " · "

let private dateFormat = "yyyy-MM-dd"

let renderVerdict verdict =
    match verdict with
    | Clean -> "clean"
    | Minor clause -> $"minor: {clause}"
    | Friction clause -> $"friction: {clause}"
    | Deferred clause -> $"deferred: {clause}"
    | Compacted words -> $"compacted: {words} words"

let renderWords words = $"{words} words"

/// The word count sits before the verdict, not after it, so the verdict stays
/// last: its clause is free text a retro writes and may hold the separator,
/// which a trailing field would have to guess its way back past.
let render (entry: Entry) =
    let head = [ entry.Date.ToString(dateFormat); entry.Repo ]
    let words = entry.Words |> Option.map renderWords |> Option.toList

    String.concat separator (head @ words @ [ renderVerdict entry.Verdict ])

/// None when the two affixes overlap rather than bracketing something: a
/// `StartsWith` and an `EndsWith` can both hold on a string shorter than the two
/// together ("compacted: words"), which is a negative Substring length and an
/// exception that escapes Program.fs's handler.
let private between (prefix: string) (suffix: string) (text: string) =
    match text.Length - prefix.Length - suffix.Length with
    | length when length < 0 -> None
    | length -> Some(text.Substring(prefix.Length, length))

/// The one definition of the vocabulary. `log` validates what it is given by
/// running it through here, so the writer cannot accept a form the readers do
/// not recognise — in the shell version those were two separate regexes.
let parseVerdict (text: string) : Verdict option =
    let clause prefix =
        text
        |> between prefix ""
        |> Option.map (fun clause -> clause.Trim())
        |> Option.filter (fun clause -> clause <> "")

    match text with
    | "clean" -> Some Clean
    | _ when text.StartsWith "minor: " -> clause "minor: " |> Option.map Minor
    | _ when text.StartsWith "friction: " -> clause "friction: " |> Option.map Friction
    | _ when text.StartsWith "deferred: " -> clause "deferred: " |> Option.map Deferred
    | _ when text.StartsWith "compacted: " && text.EndsWith " words" ->
        text
        |> between "compacted: " " words"
        |> Option.bind (fun words ->
            // Int32.TryParse takes a leading sign, so "compacted: -5 words" used
            // to reach Ratio as a baseline and divide into a -1.0x that printed
            // as "under the trigger" — a wrong answer wearing the shape of a
            // reassuring one. A SKILL.md of zero or fewer words is not a size.
            match Int32.TryParse words with
            | true, words when words > 0 -> Some(Compacted words)
            | _ -> None)
    | _ -> None

/// The word-count field, which is only that when it is exactly a count: a
/// three-field entry whose clause happens to contain the separator would
/// otherwise have its first fragment read as one.
let private (|Words|_|) (text: string) =
    match text |> between "" " words" with
    | Some count when text.EndsWith " words" ->
        match Int32.TryParse count with
        | true, words when words > 0 -> Some words
        | _ -> None
    | _ -> None

/// None for a line that is not an entry at all — the heading, blanks, prose.
///
/// A line that opens with a date but does not parse aborts rather than being
/// skipped: quietly dropping it would leave the counters wrong in exactly the
/// way that is hard to notice, which is the failure this file exists to prevent.
let parseLine (line: string) : Entry option =
    let parseDate (text: string) =
        match DateOnly.TryParseExact(text, dateFormat) with
        | true, date -> Some date
        | _ -> None

    if line.Length < dateFormat.Length || (parseDate (line.Substring(0, dateFormat.Length))).IsNone then
        None
    else
        // Bounded so the verdict keeps the rest of the line: date, repo and the
        // count cannot contain the separator, but the clause is free text a
        // retro writes, and an unbounded split turned one stray " · " in it into
        // a parse abort that took down every reader of that skill's whole log.
        //
        // Both arities are live: every entry written before the count existed
        // has three fields, and rewriting them to add one would be inventing a
        // measurement nobody took.
        let parts =
            match line.Split(separator, 4, StringSplitOptions.None) with
            | [| date; repo; Words words; verdict |] -> Some(date, Some words, repo, verdict)
            | _ ->
                match line.Split(separator, 3, StringSplitOptions.None) with
                | [| date; repo; verdict |] -> Some(date, None, repo, verdict)
                | _ -> None

        match parts with
        | None -> fail $"run-log entry is not `date{separator}repo{separator}verdict`: {line}"
        | Some(date, words, repo, verdict) ->
            match parseDate date, parseVerdict verdict with
            | Some date, Some verdict ->
                Some
                    { Date = date
                      Repo = repo
                      Words = words
                      Verdict = verdict }
            | _, None -> fail $"run log holds a verdict no reader knows: {line}"
            | None, _ -> fail $"run log holds a malformed date: {line}"

/// A run happened. The compaction baseline shares the log but is not a run —
/// counting it as one is the bug a8531b1 had to patch out of the shell version.
let isRun verdict =
    match verdict with
    | Clean
    | Minor _
    | Friction _ -> true
    | Deferred _
    | Compacted _ -> false

/// Ends the trailing streak the maturity ladder is counted from. Never asked of
/// a deferral or a baseline — `isRun` has already filtered both out.
let isFriction verdict =
    match verdict with
    | Friction _ -> true
    | Clean
    | Minor _
    | Deferred _
    | Compacted _ -> false

let isClean verdict =
    match verdict with
    | Clean -> true
    | Minor _
    | Friction _
    | Deferred _
    | Compacted _ -> false

/// The word count /skill-compact recorded, if this entry is a baseline. Only a
/// verdict that parsed whole can be one, so prose quoting the phrase cannot
/// become a baseline the way it could when this was a regex over raw lines.
let baselineWords verdict =
    match verdict with
    | Compacted words -> Some words
    | Clean
    | Minor _
    | Friction _
    | Deferred _ -> None
