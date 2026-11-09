/// Entry point. Only the pure modules are covered — Bumps and Checks:
/// everything else in survey is a thin layer over gh, and Gh.run hardcodes
/// `Process.Start "gh"` with no seam to fake.
module Program

open Expecto

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv (testList "survey" [ BumpsTests.tests; ChecksTests.tests ])
