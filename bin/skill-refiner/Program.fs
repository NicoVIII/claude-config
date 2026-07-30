/// Entry point: dispatch, and turn any failure into a non-zero exit.
///
/// The vocabulary is argv, not a quoted string the writer re-parses: a typo in
/// it is then a usage error naming the forms that exist, and there is only one
/// place — Domain.fs — where what a verdict means is decided.
module Program

open Domain

let private binaryName = "skill-refiner"

let private commands = [ "log"; "maturity"; "ratio" ]

let private changeEvent (args: string list) =
    match args with
    | [ "retro"; "clean" ] -> Retro Clean
    | [ "retro"; "minor"; text ] -> Retro(Minor(clause text))
    | [ "retro"; "major"; text ] -> Retro(Major(clause text))
    | "retro" :: _ ->
        usage
            $"{binaryName} <skill> log retro <clean | minor <clause> | major <clause>>\n  e.g. {binaryName} grilling log retro minor 'footer placement guessed'"
    | [ "fix"; "small"; text ] -> Fix(Small, clause text)
    | [ "fix"; "big"; text ] -> Fix(Big, clause text)
    | "fix" :: _ ->
        usage
            $"{binaryName} <skill> log fix <small|big> <clause>\n  e.g. {binaryName} grilling log fix small 'footer placement pinned down'"
    | [ "compacted"; text ] -> Compacted(clause text)
    | "compacted" :: _ ->
        usage
            $"{binaryName} <skill> log compacted <clause>\n  e.g. {binaryName} grilling log compacted 'merged the two retraction rules'"
    | _ -> usage $"{binaryName} <skill> log <creation | retro … | fix … | compacted <clause>>"

let private log (skill: string) (args: string list) =
    match args with
    | [ "creation" ] -> Log.creation skill
    | "creation" :: _ -> usage $"{binaryName} <skill> log creation"
    | args -> changeEvent args |> Log.change skill

/// Rejects a typo'd skill name before anything else runs, so the failure names
/// the real mistake rather than surfacing as "not inside a git repo" — the
/// confusing error a misspelling used to produce.
let private withSkill (skill: string) (body: string -> unit) =
    Layout.skillDir skill |> ignore
    body skill

[<EntryPoint>]
let main argv =
    try
        match List.ofArray argv with
        | [ skill; "maturity" ] -> withSkill skill Maturity.run
        | [ skill; "ratio" ] -> withSkill skill Ratio.run
        | skill :: "log" :: rest -> withSkill skill (fun skill -> log skill rest)
        | _ :: command :: _ when not (List.contains command commands) ->
            fail $"unknown command '{command}' (expected log, maturity or ratio)"
        | _ ->
            usage $"{binaryName} <skill> <log|maturity|ratio> [args]\n  e.g. {binaryName} grilling maturity"

        0
    with
    | SkillRefinerFailure message ->
        eprintfn $"{binaryName}: {message}"
        1
    | UsageError message ->
        eprintfn $"usage: {message}"
        1
