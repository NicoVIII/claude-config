/// Shelling out to git and stat, the only external tools skill-refiner needs.
/// git names the repo a run happened in and tells checkouts of the config
/// from projects; stat backs that second question where one repository is
/// mounted at two paths. Nothing that reads the log consults either, because
/// the log records every size it needs.
module Git

open System.Diagnostics
open Domain

/// stdout on success, None when the program exits non-zero. The caller
/// decides whether that is fatal — "not a repo" and "no origin remote" are
/// both ordinary answers here, so this does not abort on its own.
let private exec (program: string) (workingDir: string) (args: string list) : string option =
    let startInfo =
        ProcessStartInfo(
            program,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDir
        )

    args |> List.iter startInfo.ArgumentList.Add

    use proc =
        try
            Process.Start startInfo
        with e ->
            fail $"could not run {program}: {e.Message}"

    // Both pipes must be drained concurrently: reading one to the end while the
    // other fills its buffer deadlocks the child.
    let stdout = proc.StandardOutput.ReadToEndAsync()
    let stderr = proc.StandardError.ReadToEndAsync()
    proc.WaitForExit()

    if proc.ExitCode = 0 then
        Some(stdout.Result.TrimEnd '\n')
    else
        ignore stderr.Result
        None

let run (workingDir: string) (args: string list) : string option = exec "git" workingDir args

/// The device and inode of a path, which name the file itself rather than the
/// way to reach it: a bind mount gives one directory two paths that no
/// normalisation reconciles. GNU stat takes `-c`; BSD stat rejects it and
/// takes `-f`, which GNU would read as a filesystem query, so the order
/// matters. None when neither answers.
let fileId (path: string) : string option =
    exec "stat" "." [ "-c"; "%d:%i"; path ]
    |> Option.orElseWith (fun () -> exec "stat" "." [ "-f"; "%d:%i"; path ])
