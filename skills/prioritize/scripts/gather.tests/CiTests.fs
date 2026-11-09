/// The cases are the JSON shapes gh was observed to send, not ones derived
/// from the code under test.
module CiTests

open Expecto

let private check (conclusion: string) : Wire.Check = { conclusion = Some conclusion }

let tests =
    testList
        "Ci"
        [ test "a PR whose checks are still running is not failing" {
              // Arrange — gh sends an in-flight CheckRun as `"conclusion": ""`,
              // which used to put the PR in the ATTENTION block as FAILING-CI
              let rollup = Some [ check "SUCCESS"; check "" ]

              // Act
              let failing = Ci.pullFailing rollup

              // Assert
              Expect.isFalse failing "an empty conclusion has not concluded"
          }

          test "one concluded failure makes the PR failing" {
              // Act
              let failing = Ci.pullFailing (Some [ check "SUCCESS"; check ""; check "FAILURE" ])

              // Assert
              Expect.isTrue failing "the failure decides, whatever else is running"
          }

          test "a PR with no checks is not failing" {
              // Act & Assert — gh sends null for one, an empty list for another
              Expect.isFalse (Ci.pullFailing None) "no rollup"
              Expect.isFalse (Ci.pullFailing (Some [])) "empty rollup"
          } ]
