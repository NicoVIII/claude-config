/// Entry point. Only Bumps is covered: everything else in survey is a thin
/// layer over gh, and Gh.run hardcodes `Process.Start "gh"` with no seam to
/// fake — the same reason gather has no suite.
module Program

open Expecto

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv (testList "survey" [ BumpsTests.tests ])
