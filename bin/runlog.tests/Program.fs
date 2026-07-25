/// Entry point. Integration tests dominate the suite deliberately: runlog is a
/// thin tool over files and process boundaries, so its defects live at the
/// seams rather than inside algorithms. Unit tests are reserved for the one
/// path complex enough to earn them — see VerdictTests.
module Program

open Expecto

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv (testList "runlog" [ VerdictTests.tests; CliTests.tests ])
