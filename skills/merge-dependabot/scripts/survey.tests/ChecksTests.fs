/// The rollup verdict is a table over gh's own spellings, so the cases below
/// are the JSON shapes gh was observed to send, not ones derived from the code.
module ChecksTests

open Expecto
open Domain

let private check (name: string) (conclusion: string) : Wire.Check =
    { name = name
      conclusion = Some conclusion
      detailsUrl = Some $"https://example.test/{name}" }

let tests =
    testList
        "Checks"
        [ test "a check still running is pending, not red" {
              // Arrange — gh sends an in-flight CheckRun as `"conclusion": ""`;
              // read as an ordinary conclusion that made every such PR CI=red
              let rollup = Some [ check "build" "SUCCESS"; check "test" "" ]

              // Act
              let verdict = Checks.verdict rollup

              // Assert
              Expect.equal verdict Pending "an empty conclusion has not concluded"
          }

          test "a concluded failure outranks whatever is still running" {
              // Arrange
              let rollup = Some [ check "lint" ""; check "test" "FAILURE" ]

              // Act
              let verdict = Checks.verdict rollup

              // Assert
              Expect.equal
                  verdict
                  (Red [ "test", Some "https://example.test/test" ])
                  "only the concluded failure is named as failing"
          }

          test "neutral and skipped beside a success are green" {
              // Arrange
              let rollup = Some [ check "a" "SUCCESS"; check "b" "NEUTRAL"; check "c" "SKIPPED" ]

              // Act
              let verdict = Checks.verdict rollup

              // Assert
              Expect.equal verdict Green "none of the three is a failure"
          }

          test "only neutral and skipped checks ran nothing" {
              // Act
              let verdict = Checks.verdict (Some [ check "a" "NEUTRAL"; check "b" "SKIPPED" ])

              // Assert
              Expect.equal verdict NoChecks "no success, so no evidence CI ran"
          }

          test "a conclusion this does not know is red" {
              // Act
              let verdict = Checks.verdict (Some [ check "test" "SOMETHING_NEW" ])

              // Assert
              Expect.equal
                  verdict
                  (Red [ "test", Some "https://example.test/test" ])
                  "an unknown conclusion must not read as a pass"
          }

          test "a PR with no checks at all has none, not green" {
              // Act & Assert — gh sends null for one, an empty list for another
              Expect.equal (Checks.verdict None) NoChecks "no rollup"
              Expect.equal (Checks.verdict (Some [])) NoChecks "empty rollup"
          }

          test "run conclusions classify in gh's lowercase spelling too" {
              // Act & Assert — `gh run list` says `success` where the rollup
              // says `SUCCESS`
              Expect.equal (Conclusion.classify (Some "success")) (Conclusion.Passed "success") "lowercase pass"
              Expect.equal (Conclusion.classify (Some "failure")) (Conclusion.Failed "failure") "lowercase failure"
              Expect.equal (Conclusion.classify None) Conclusion.Running "an absent conclusion"
          } ]
