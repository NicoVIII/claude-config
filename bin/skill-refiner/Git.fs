/// Shelling out to git, the only external tool skill-refiner needs — and it
/// needs it for one field: the repo a run happened in. Nothing that reads the
/// log consults git, because the log records every size it needs.
module Git

open System.Diagnostics
open Domain

/// stdout on success, None when git exits non-zero. The caller decides whether
/// that is fatal — "not a repo" and "no origin remote" are both ordinary
/// answers here, so this does not abort on its own.
let run (workingDir: string) (args: string list) : string option =
    let startInfo =
        ProcessStartInfo(
            "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDir
        )

    args |> List.iter startInfo.ArgumentList.Add

    use proc =
        try
            Process.Start startInfo
        with e ->
            fail $"could not run git: {e.Message}"

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
