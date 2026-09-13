/// The README planted beside a project's own skills, so the log there can be
/// read by someone who has never seen this config.
///
/// A project skill's HISTORY.md commits into the project and ships with it,
/// where everything that makes it usable — the line format, the append-only
/// rule, the tooling that writes it — is invisible: the files arrive without
/// their manual. So the manual is planted once, by the write that creates the
/// situation, rather than by SKILL.md prose that would reload every run and
/// could be skipped in the one run that mattered.
///
/// The text is an embedded markdown file rather than a string literal so it
/// stays a document to edit and review, and embedded rather than read out of
/// `references/` so seeding cannot fail on a path outside the binary.
module Explainer

open System.IO
open System.Reflection
open Domain

/// Pinned in the fsproj as a LogicalName, because the default is derived from
/// the project name and this one has a hyphen in it.
let private resourceName = "ProjectSkillsReadme.md"

let private explainer () =
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream resourceName

    if isNull stream then
        fail $"'{resourceName}' was not embedded in this binary"

    use reader = new StreamReader(stream)
    reader.ReadToEnd()

/// The skills already sitting beside the one being logged. A seeded table has
/// no rows at all, so these are unlisted by construction rather than by a
/// lookup — and seeding is the only moment that can produce a backlog of them:
/// a skill added afterwards reaches its row through its own maturity run.
let private otherSkillsIn (holder: string) (skill: string) =
    let current = DirectoryInfo(Layout.skillDir skill).Name

    Directory.GetDirectories holder
    |> Array.map DirectoryInfo
    |> Array.filter (fun dir -> dir.Name <> current && File.Exists(Path.Combine(dir.FullName, "SKILL.md")))
    |> Array.map (fun dir -> dir.Name)
    |> Array.sortBy (fun name -> name.ToLowerInvariant())
    |> List.ofArray

/// Never overwrites: a tree that already describes its skills has said it
/// better than a template can, and the seeded copy is meant to be edited. The
/// line names the file as something to commit — the seeded README is untracked
/// in the project, and a caller that commits only the skill and its log leaves
/// the explanation behind.
///
/// The backlog is named here rather than in the seeded README, which is written
/// for a reader who has neither this binary nor a reason to maintain the table
/// (9056afe). `maturity` is pointed at rather than paraphrased: it supplies the
/// rung and nothing else, and the caller that mistakes it for the whole row
/// writes the two judged columns from a default.
let seedIfForeign (skill: string) =
    match Layout.foreignSkillsDir skill with
    | None -> ()
    | Some dir ->
        let path = Path.Combine(dir, "README.md")

        if not (File.Exists path) then
            File.WriteAllText(path, explainer ())
            printfn $"  seeded {path} — commit it with the skill"

            match otherSkillsIn dir skill with
            | [] -> ()
            | others ->
                let noun = if others.Length = 1 then "skill" else "skills"

                printfn $"""  {others.Length} other {noun} beside it, with no row yet: {String.concat ", " others}"""
                printfn "    `skill-refiner <name> maturity` rates each; its summary and suggested model are yours to judge"
