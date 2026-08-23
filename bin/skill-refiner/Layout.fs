/// Where the config tree keeps what skill-refiner reads, and the only door to
/// the log.
module Layout

open System
open System.IO
open Domain

/// Walked up from the binary rather than assumed to be ~/.claude: the README
/// invites forking this repo, and a fork may sit anywhere.
let root () =
    let isRoot (dir: DirectoryInfo) =
        Directory.Exists(Path.Combine(dir.FullName, "skills"))
        && File.Exists(Path.Combine(dir.FullName, "README.md"))

    let rec search (dir: DirectoryInfo option) =
        match dir with
        | Some dir when isRoot dir -> dir.FullName
        | Some dir -> search (Option.ofObj dir.Parent)
        | None -> fail "no ancestor of this binary holds both skills/ and README.md, so the config root is unknown"

    search (Some(DirectoryInfo AppContext.BaseDirectory))

let skillDir (skill: string) =
    let dir = Path.Combine(root (), "skills", skill)

    if not (Directory.Exists dir) then
        fail $"no skill named '{skill}'"

    dir

/// Checked like `skillDir` above, and for the same reason: an unreadable path
/// returned as if it were fine surfaces as a FileNotFoundException in the
/// caller, which is not a SkillRefinerFailure and so escapes Program.fs's
/// handler.
let skillFile (skill: string) =
    let path = Path.Combine(skillDir skill, "SKILL.md")

    if not (File.Exists path) then
        fail $"skill '{skill}' has no SKILL.md"

    path

/// Agrees with `wc -w`: runs of non-whitespace, split on POSIX whitespace. The
/// baselines already recorded in the logs were produced by wc, so a different
/// notion of a word would silently shift every one of them.
let wordCount (text: string) =
    text.Split([| ' '; '\t'; '\n'; '\r'; '\f'; '\v' |], StringSplitOptions.RemoveEmptyEntries)
    |> Array.length

let skillWords (skill: string) = skillFile skill |> File.ReadAllText |> wordCount

let private historyFile (skill: string) =
    Path.Combine(skillDir skill, "HISTORY.md")

/// Both readers select their entries through here, so neither can hold its own
/// idea of what an entry is. A skill with no log yet reads as an empty history
/// rather than an error — that is 🚧 WIP, not a fault.
let history (skill: string) : History =
    let path = historyFile skill

    if File.Exists path then
        File.ReadAllText path |> parseHistory
    else
        { Creation = None; Entries = [] }

/// Seeding the origin baseline. It must be the first line, so this refuses once
/// a change has been logged against it — a late creation is a shape the reader
/// would reject anyway. Re-seeding a log that is still nothing but a creation
/// line is safe and deliberately allowed: no retro or fix has read that
/// baseline yet, and the alternative is a number frozen before the author
/// settled on the text (12e09b7's interrupt, and again in the run that seeded
/// `groom`).
/// The caller is an agent reading through dotnet's first-run banner, where a
/// silent success is indistinguishable from a swallowed failure. Echoing the
/// written line is the confirmation it would otherwise spend a round trip on —
/// and it puts the derived repo in view, which is the field a caller who ran
/// this after a `cd` gets wrong without ever seeing it.
let private announce (path: string) (line: string) = printfn $"logged to {path}:\n  {line}"

let createHistory (skill: string) (entry: Entry<CreationEvent>) =
    let path = historyFile skill

    if File.Exists path && not (history skill).Entries.IsEmpty then
        fail "HISTORY.md already has changes logged — the origin baseline is fixed"

    let line = renderCreationEntry entry
    File.WriteAllText(path, $"{heading}\n\n{line}\n")
    announce path line

let appendChange (skill: string) (entry: Entry<ChangeEvent>) =
    let path = historyFile skill

    // No `created` line was logged (a skill predating creation seeding): the log
    // simply starts without an origin baseline rather than assuming one.
    if not (File.Exists path) then
        File.WriteAllText(path, $"{heading}\n\n")

    let line = renderChangeEntry entry
    File.AppendAllText(path, line + "\n")
    announce path line

/// The README's rating, for comparison only. It is a claim, not evidence: where
/// the two disagree, the log wins. The skill is matched as the literal link
/// text, so one skill's row cannot match another whose name extends it.
let claimedRating (skill: string) : string option =
    let link = $"[`{skill}`]("

    Path.Combine(root (), "README.md")
    |> File.ReadAllLines
    |> Array.tryPick (fun line ->
        let cells = line.Split '|'

        if cells.Length >= 5 && cells[1].Contains link then
            Some(cells[4].Trim())
        else
            None)
