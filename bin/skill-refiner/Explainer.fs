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

/// Never overwrites: a tree that already describes its skills has said it
/// better than a template can, and the seeded copy is meant to be edited. The
/// line names the file as something to commit — the seeded README is untracked
/// in the project, and a caller that commits only the skill and its log leaves
/// the explanation behind.
let seedIfForeign (skill: string) =
    match Layout.foreignSkillsDir skill with
    | None -> ()
    | Some dir ->
        let path = Path.Combine(dir, "README.md")

        if not (File.Exists path) then
            File.WriteAllText(path, explainer ())
            printfn $"  seeded {path} — commit it with the skill"
