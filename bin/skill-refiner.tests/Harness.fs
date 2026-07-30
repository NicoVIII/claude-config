/// Setup shared by the CLI tests, so each test body reads as arrange/act/assert
/// over skill-refiner's behaviour rather than over temp-directory plumbing.
///
/// skill-refiner resolves its config root by walking up from its own binary
/// until it finds a directory holding both skills/ and README.md. A test
/// therefore cannot point it at a scratch directory from outside — the binary
/// has to sit inside one. Each test gets a throwaway root with the tool deployed
/// into it, which has the side benefit of exercising the root walk rather than
/// bypassing it.
module Harness

open System
open System.Diagnostics
open System.IO

type Result =
    { ExitCode: int
      Stdout: string
      Stderr: string }

/// `SessionRepo` is not incidental: `log` names the run's repo by running git in
/// the working directory, so without a repo to invoke it from, every log test
/// would fail on that instead of on what it means to test.
type Root = { Dir: string; SessionRepo: string }

/// Everything skill-refiner needs once detached from its build output. The
/// apphost is deliberately left out and the assembly invoked through `dotnet`,
/// which keeps the harness clear of executable-bit and `.exe`-naming concerns.
let private deployment =
    [ "skill-refiner.dll"
      "skill-refiner.deps.json"
      "skill-refiner.runtimeconfig.json"
      "FSharp.Core.dll" ]

/// What `log` will record: the basename of a repo with no origin remote.
let sessionRepoName = "demo-repo"

let private exec (fileName: string) (workingDir: string) (args: string list) =
    let startInfo =
        ProcessStartInfo(
            fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDir
        )

    args |> List.iter startInfo.ArgumentList.Add

    use proc = Process.Start startInfo

    // Drained concurrently: filling one pipe while reading the other deadlocks.
    let stdout = proc.StandardOutput.ReadToEndAsync()
    let stderr = proc.StandardError.ReadToEndAsync()
    proc.WaitForExit()

    { ExitCode = proc.ExitCode
      Stdout = stdout.Result
      Stderr = stderr.Result }

let private create () =
    let dir = Path.Combine(Path.GetTempPath(), $"skill-refiner-tests-{Guid.NewGuid():N}")
    let bin = Path.Combine(dir, "bin")
    Directory.CreateDirectory bin |> ignore
    Directory.CreateDirectory(Path.Combine(dir, "skills")) |> ignore

    for file in deployment do
        File.Copy(Path.Combine(AppContext.BaseDirectory, file), Path.Combine(bin, file))

    // A README is half of what marks the root. The table starts empty, so
    // `claimedRating` reports "unlisted" until a test chooses to list a skill.
    File.WriteAllText(
        Path.Combine(dir, "README.md"),
        "# Test root\n\n| Skill | Summary | Suggested model | Maturity |\n| --- | --- | --- | --- |\n"
    )

    let sessionRepo = Path.Combine(dir, sessionRepoName)
    Directory.CreateDirectory sessionRepo |> ignore
    exec "git" sessionRepo [ "init"; "--quiet" ] |> ignore

    { Dir = dir; SessionRepo = sessionRepo }

/// Runs `body` against a fresh root and removes it afterwards. Roots are never
/// shared, so the suite stays safe to run in parallel.
let withRoot (body: Root -> unit) =
    let root = create ()

    try
        body root
    finally
        try
            Directory.Delete(root.Dir, true)
        with _ ->
            ()

let private skillDir (root: Root) (name: string) =
    let dir = Path.Combine(root.Dir, "skills", name)
    Directory.CreateDirectory dir |> ignore
    dir

/// A SKILL.md of exactly `count` words: the ratio tests assert on arithmetic,
/// not on prose.
let words (count: int) = String.replicate count "word "

let skill (name: string) (content: string) (root: Root) =
    File.WriteAllText(Path.Combine(skillDir root name, "SKILL.md"), content)

/// A skill directory with no SKILL.md — the shape `ratio` used to crash on.
let bareSkill (name: string) (root: Root) = skillDir root name |> ignore

/// Writes HISTORY.md from (repo, words, event) triples, dating them a day apart
/// so the order the readers see is the order given.
let history (name: string) (entries: (string * int * string) list) (root: Root) =
    let start = DateOnly(2026, 1, 1)

    let line index (repo, words, event) =
        let date = start.AddDays(index).ToString "yyyy-MM-dd"
        $"{date} · {repo} · {words} words · {event}"

    let body = entries |> List.mapi line |> String.concat "\n"
    File.WriteAllText(Path.Combine(skillDir root name, "HISTORY.md"), $"# Skill History\n\n{body}\n")

/// The maturity case: entries differing only in event, all one repo and one
/// size, since neither figures in the ladder.
let logged (name: string) (events: string list) (root: Root) =
    root |> history name (events |> List.map (fun event -> "repo-a", 100, event))

/// The 🛡️ Battle-tested case, where which repo a run happened in is the point.
let loggedIn (name: string) (entries: (string * string) list) (root: Root) =
    root |> history name (entries |> List.map (fun (repo, event) -> repo, 100, event))

/// The ratio case, where the recorded sizes are the point.
let loggedSized (name: string) (entries: (int * string) list) (root: Root) =
    root |> history name (entries |> List.map (fun (words, event) -> "repo-a", words, event))

/// Adds the skill's row to the README maturity table — the claim `maturity`
/// compares its log-derived rating against.
let listed (name: string) (maturity: string) (root: Root) =
    File.AppendAllText(
        Path.Combine(root.Dir, "README.md"),
        $"| [`{name}`](skills/{name}/SKILL.md) | summary | Sonnet | {maturity} |\n"
    )

/// Invokes the CLI the way a skill does: from inside the session repo.
let skillRefiner (args: string list) (root: Root) =
    exec "dotnet" root.SessionRepo (Path.Combine(root.Dir, "bin", "skill-refiner.dll") :: args)

/// The log as it stands, for assertions about what a run appended — or did not.
let historyFile (name: string) (root: Root) =
    let path = Path.Combine(root.Dir, "skills", name, "HISTORY.md")

    if File.Exists path then Some(File.ReadAllText path) else None
