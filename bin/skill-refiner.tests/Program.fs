/// Entry point. Integration tests dominate the suite deliberately: skill-refiner
/// is a thin tool over files and process boundaries, so its defects live at the
/// seams rather than inside algorithms. Unit tests are reserved for the one path
/// complex enough to earn them — see FormatTests.
module Program

open Expecto

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs
        []
        argv
        (testList
            "skill-refiner"
            [ FormatTests.tests
              FormatTests.clauseTests
              FormatTests.lineTests
              FormatTests.historyTests
              CliTests.tests ])
