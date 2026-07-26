/// Appending one entry.
///
/// Date, repo and file creation are mechanical and had been patched four times
/// as SKILL.md prose (45ad1bb, 55146ed, f00f040) before moving into a helper.
/// The verdict is the retro's judgement and has to be passed in.
module Log

open System
open System.IO
open Domain

/// `basename <url> .git`: the last path segment, less a .git suffix. Handles
/// both remote forms, since ssh remotes still separate the repo with a slash.
let private repoName (url: string) =
    let last = url.TrimEnd('/').Split('/') |> Array.last

    if last.EndsWith ".git" then
        last.Substring(0, last.Length - ".git".Length)
    else
        last

/// The repo is the one the session ran in — for a cross-repo skill, still where
/// the session ran, not the repos it touched. That is what running git from the
/// working directory gives, which is why it is not derived from the skill path.
let private sessionRepo () =
    let here = Directory.GetCurrentDirectory()

    match Git.run here [ "rev-parse"; "--show-toplevel" ] with
    | None -> fail "not inside a git repo, so the run's repo cannot be named"
    | Some toplevel ->
        // The remote, not the directory: this repo is checked out as ~/.claude
        // but is named claude-config, and the log has always recorded the latter.
        match Git.run here [ "remote"; "get-url"; "origin" ] with
        | Some remote when remote <> "" -> repoName remote
        | Some _
        | None -> repoName toplevel

let run (skill: string) (verdict: string) =
    // Rejects an unknown skill before sessionRepo() can fail with "not inside a
    // git repo" — the confusing error for what is really a typo'd skill name.
    Layout.skillDir skill |> ignore

    // A shorthand resolved before parsing, not a second vocabulary: the readers
    // still see only `compacted: <n> words`. The baseline is the one number
    // every later ratio is measured against, which makes it the last one that
    // should arrive by an agent retyping what wc printed.
    let verdict =
        if verdict = "compacted" then
            renderVerdict (Compacted(Layout.skillWords skill))
        else
            verdict

    match parseVerdict verdict with
    | None ->
        fail
            $"'{verdict}' is not a verdict\n  expected: clean | minor: <clause> | friction: <clause> | deferred: <clause> | compacted"
    | Some verdict ->
        // Measured here rather than passed in: the caller is an agent that has
        // just edited the file, and a number it retypes is a number that can be
        // stale. A compaction states its own count in the verdict, so recording
        // it twice would only create two places for them to disagree.
        let words =
            match verdict with
            // A deferral changes nothing, so it has no size to record: the
            // number would only invite a later reader to read growth into it.
            | Compacted _
            | Deferred _ -> None
            | Clean
            | Minor _
            | Friction _ -> Some(Layout.skillWords skill)

        Layout.appendEntry
            skill
            { Date = DateOnly.FromDateTime DateTime.Now
              Repo = sessionRepo ()
              Words = words
              Verdict = verdict }
