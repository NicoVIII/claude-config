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
/// the log exists — a late or repeated creation is a shape the reader would
/// reject anyway, and refusing here says so while the mistake is still one
/// command old.
let createHistory (skill: string) (entry: Entry<CreationEvent>) =
    let path = historyFile skill

    if File.Exists path then
        fail "HISTORY.md already exists — creation is logged once, before anything else"

    File.WriteAllText(path, $"{heading}\n\n{renderCreationEntry entry}\n")

let appendChange (skill: string) (entry: Entry<ChangeEvent>) =
    let path = historyFile skill

    // No `created` line was logged (a skill predating creation seeding): the log
    // simply starts without an origin baseline rather than assuming one.
    if not (File.Exists path) then
        File.WriteAllText(path, $"{heading}\n\n")

    File.AppendAllText(path, renderChangeEntry entry + "\n")

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
