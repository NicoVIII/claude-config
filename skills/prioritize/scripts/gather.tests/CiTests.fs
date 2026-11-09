/// The cases are the JSON shapes gh was observed to send, not ones derived
/// from the code under test.
module CiTests

open Expecto

let private check (conclusion: string) : Wire.Check = { conclusion = Some conclusion }

let private run (workflow: string) (conclusion: string) : Wire.Run =
    { workflowName = workflow
      conclusion = Some conclusion }

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
          }

          test "a red check suite is not hidden by a newer run of another workflow" {
              // Arrange — newest first, as gh lists them. The shape seen on a
              // real default branch: weekly `Dependabot Updates` runs landing
              // on top of the last push's check run.
              let runs =
                  [ run "Dependabot Updates" "success"
                    run "Dependabot Updates" "success"
                    run "check" "failure"
                    run "check" "success" ]

              // Act
              let status = Ci.ofRuns runs

              // Assert
              Expect.equal status (Domain.Concluded "failure") "the latest check run is what the branch's CI says"
          }

          test "a failure a later run of the same workflow fixed is history" {
              // Act
              let status = Ci.ofRuns [ run "check" "success"; run "check" "failure" ]

              // Assert
              Expect.equal status (Domain.Concluded "success") "only each workflow's latest run counts"
          }

          test "a run still in flight defers to the one before it" {
              // Arrange — `gh run list` sends a running workflow as `""`
              let runs = [ run "check" ""; run "check" "failure" ]

              // Act
              let status = Ci.ofRuns runs

              // Assert
              Expect.equal status (Domain.Concluded "failure") "the last completed run is the last thing known"
          }

          test "nothing completed yet is in progress, and no runs is its own answer" {
              // Act & Assert
              Expect.equal (Ci.ofRuns [ run "check" "" ]) Domain.InProgress "only a running run"
              Expect.equal (Ci.ofRuns []) Domain.NoRuns "an empty list"
          } ]
