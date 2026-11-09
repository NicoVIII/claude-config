/// Entry point. Only Ci is covered: everything else in gather is a thin layer
/// over gh, and Gh.run hardcodes `Process.Start "gh"` with no seam to fake.
module Program

open Expecto

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv (testList "gather" [ CiTests.tests ])
