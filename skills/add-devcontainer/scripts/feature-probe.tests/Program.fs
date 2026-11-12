module Program

open Expecto

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv (testList "feature-probe" [ RunTests.tests ])
