/// How gh spells the outcome of a check or a workflow run.
///
/// Shared for the reason Gh.fs is: gather and survey both read conclusions, and
/// each matched "still running" as an absent field on its own. gh serialises a
/// conclusion as a plain string, so one that has not arrived yet is `""`, not
/// missing — and read as text it is neither success, neutral nor skipped, which
/// made every in-flight check a failed one.
module Conclusion

/// The text rides along because the caller prints it: "which conclusion" is
/// what the reader of a digest asks next.
type Outcome =
    | Running
    | Passed of conclusion: string
    | Failed of conclusion: string

/// Anything that concluded outside the three known-good states is a failure,
/// including conclusions GitHub adds after this was written. Compared without
/// case because gh does not agree with itself: a PR's check rollup says
/// `SUCCESS` where `gh run list` says `success`.
///
/// None is the legacy StatusContext entry, which carries `state` instead of a
/// conclusion. It reads as running rather than aborting because gather counts
/// across every repo I own; survey refuses the same entry by requiring `name`.
let classify (conclusion: string option) =
    match conclusion with
    | None
    | Some "" -> Running
    | Some text ->
        match text.ToUpperInvariant() with
        | "SUCCESS"
        | "NEUTRAL"
        | "SKIPPED" -> Passed text
        | _ -> Failed text
