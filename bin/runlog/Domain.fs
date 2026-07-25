/// The run-log format, and the verdict taxonomy every reader shares.
///
/// This type is why the helper is F# and not shell. The log had three shell
/// parsers and they disagreed twice: `maturity` counted a `compacted:` baseline
/// as a run (a8531b1), and `ratio` matched that phrase inside unrelated prose.
/// Both are missing-case bugs. So the verdicts are a closed union, and every
/// match over it below lists all four cases rather than falling back to a
/// wildcard — adding a fifth verdict is then a compile error at each site that
/// has to decide what it means, which is the whole point of the port.
module Domain

open System

exception RunlogFailure of string

let fail message = raise (RunlogFailure message)

type Verdict =
    | Clean
    | Minor of clause: string
    | Friction of clause: string
    /// Not a run: /skill-compact's baseline for the growth ratio.
    | Compacted of words: int

type Entry =
    { Date: DateOnly
      Repo: string
      Verdict: Verdict }

let private separator = " · "

let private dateFormat = "yyyy-MM-dd"

let renderVerdict verdict =
    match verdict with
    | Clean -> "clean"
    | Minor clause -> $"minor: {clause}"
    | Friction clause -> $"friction: {clause}"
    | Compacted words -> $"compacted: {words} words"

let render (entry: Entry) =
    String.concat separator [ entry.Date.ToString(dateFormat); entry.Repo; renderVerdict entry.Verdict ]

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
        // Bounded at 3 so the verdict keeps the rest of the line: date and repo
        // cannot contain the separator, but the clause is free text a retro
        // writes, and an unbounded split turned one stray " · " in it into a
        // parse abort that took down every reader of that skill's whole log.
        match line.Split(separator, 3, StringSplitOptions.None) with
        | [| date; repo; verdict |] ->
            match parseDate date, parseVerdict verdict with
            | Some date, Some verdict -> Some { Date = date; Repo = repo; Verdict = verdict }
            | _, None -> fail $"run log holds a verdict no reader knows: {line}"
            | None, _ -> fail $"run log holds a malformed date: {line}"
        | _ -> fail $"run-log entry is not `date{separator}repo{separator}verdict`: {line}"

/// A run happened. The compaction baseline shares the log but is not a run —
/// counting it as one is the bug a8531b1 had to patch out of the shell version.
let isRun verdict =
    match verdict with
    | Clean
    | Minor _
    | Friction _ -> true
    | Compacted _ -> false

/// Ends the trailing streak the maturity ladder is counted from.
let isFriction verdict =
    match verdict with
    | Friction _ -> true
    | Clean
    | Minor _
    | Compacted _ -> false

let isClean verdict =
    match verdict with
    | Clean -> true
    | Minor _
    | Friction _
    | Compacted _ -> false

/// The word count /skill-compact recorded, if this entry is a baseline. Only a
/// verdict that parsed whole can be one, so prose quoting the phrase cannot
/// become a baseline the way it could when this was a regex over raw lines.
let baselineWords verdict =
    match verdict with
    | Compacted words -> Some words
    | Clean
    | Minor _
    | Friction _ -> None
