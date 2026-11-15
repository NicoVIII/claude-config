/// Entry point. Only Check is covered, fed YAML text and a hand-built schema:
/// Schema.fetch is the one module that reaches the network, and it takes no
/// handler to fake it with.
module Program

open Expecto

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv (testList "dependabot-schema" [ CheckTests.tests ])
