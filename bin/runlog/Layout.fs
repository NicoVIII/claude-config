/// Where the config tree keeps what runlog reads, and the only door to the log.
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

let skillFile (skill: string) = Path.Combine(skillDir skill, "SKILL.md")

let private runsFile (skill: string) = Path.Combine(skillDir skill, "RUNS.md")

/// Both readers select their entries through here, so neither can hold its own
/// idea of what an entry is.
let entries (skill: string) : Entry list =
    let path = runsFile skill

    if File.Exists path then
        File.ReadAllLines path |> Array.toList |> List.choose parseLine
    else
        []

let appendEntry (skill: string) (entry: Entry) =
    let path = runsFile skill

    if not (File.Exists path) then
        File.WriteAllText(path, "# Run log\n\n")

    File.AppendAllText(path, render entry + "\n")

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
