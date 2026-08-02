/// Bumps earns a unit suite on the strategy's own terms: it is the only part of
/// survey with no gh call to fake around, both regressions the run log records
/// live here, and each case is a table row — pushing them through a process
/// would be slower and would report failures far less precisely.
module BumpsTests

open Expecto
open Domain

let private npmBody =
    """Updates `lodash` from 4.17.20 to 4.17.21
- [Release notes](https://github.com/lodash/lodash/releases)"""

let private nugetBody =
    """Updated [FSharp.Core](https://github.com/dotnet/fsharp) from 9.0.100 to 9.0.101"""

let private groupedBody =
    """Bumps the npm group with 2 updates:

Updates `jest` from 29.6.0 to 29.7.0
Updates `axios` from 0.27.2 to 1.0.0"""

let tests =
    testList
        "Bumps"
        [ test "reads both phrasings Dependabot writes dependency lines in" {
              // Arrange — nuget's bracket form is what the npm-only pattern
              // missed (fd9763a)
              let cases =
                  [ npmBody, "lodash", "4.17.20", "4.17.21"
                    nugetBody, "FSharp.Core", "9.0.100", "9.0.101" ]

              // Act & Assert
              for body, name, fromVersion, toVersion in cases do
                  let parsed = Bumps.parse body "irrelevant title"

                  Expect.equal
                      (parsed |> List.map (fun bump -> bump.name, bump.fromVersion, bump.toVersion))
                      [ name, fromVersion, toVersion ]
                      $"'{body}' should yield one dependency line"
          }

          test "keeps the project link off a markdown-style line" {
              // Act
              let parsed = Bumps.parse nugetBody "irrelevant title"

              // Assert — the only fallback when a body links no release notes
              Expect.equal (parsed |> List.map (fun bump -> bump.home)) [ Some "https://github.com/dotnet/fsharp" ] "home"
          }

          test "reads every member of a grouped PR, not just the title" {
              // Act
              let parsed = Bumps.parse groupedBody "Bump the npm group with 2 updates"

              // Assert
              Expect.equal (parsed |> List.map (fun bump -> bump.name)) [ "jest"; "axios" ] "both members"
          }

          test "falls back to the title when the body carries no dependency line" {
              // Arrange — a body that is release notes only
              let body = "## What's changed\nLots of things."

              // Act
              let parsed = Bumps.parse body "Bump lodash from 4.17.20 to 4.17.21"

              // Assert
              Expect.equal (parsed |> List.map (fun bump -> bump.name)) [ "lodash" ] "title fallback"
          }

          test "grades a bump by what can actually break" {
              // Arrange — the pre-1.0 minor is the row that matters: semver
              // lets 0.27 -> 0.28 break as freely as 1.0 -> 2.0
              let cases =
                  [ "4.17.20", "4.17.21", Patch
                    "29.6.0", "29.7.0", Minor
                    "0.27.2", "1.0.0", Major
                    "0.27.2", "0.28.0", Major
                    "0.27.2", "0.27.3", Patch
                    "1.2.3", "2.0.0", Major ]

              // Act & Assert
              for fromVersion, toVersion, expected in cases do
                  Expect.equal (Bumps.level fromVersion toVersion) expected $"{fromVersion} -> {toVersion}"
          }

          test "tolerates the version spellings that are not bare semver" {
              // Arrange
              let cases =
                  [ "v1.2.3", "v1.3.0", Minor
                    "6.0.0-preview.1", "6.0.0-preview.2", Patch
                    "5", "6", Major ]

              // Act & Assert
              for fromVersion, toVersion, expected in cases do
                  Expect.equal (Bumps.level fromVersion toVersion) expected $"{fromVersion} -> {toVersion}"
          }

          test "refuses to grade a version pair it cannot compare" {
              // Act
              let graded = Bumps.level "2024.1" "latest"

              // Assert — Unclear ranks with Major, so an exotic version gets
              // looked at rather than waved through as a patch
              Expect.isTrue
                  (match graded with
                   | Unclear _ -> true
                   | _ -> false)
                  "should be Unclear"
          }

          test "calls a PR nothing parsed out of Unclear rather than safe" {
              // Act
              let level = Bumps.worst []

              // Assert
              Expect.isTrue
                  (match level with
                   | Unclear _ -> true
                   | _ -> false)
                  "empty should be Unclear"
          }

          test "lets the riskiest member decide a grouped PR" {
              // Arrange
              let bumps = Bumps.parse groupedBody "irrelevant title"

              // Act & Assert — axios 0.27 -> 1.0 outranks jest's minor
              Expect.equal (Bumps.worst bumps) Major "worst member wins"
          }

          test "supersedes the PR whose target version is behind" {
              // Arrange — two open PRs on one dependency
              let older = Bumps.parse "Updates `axios` from 0.27.2 to 1.0.0" ""
              let newer = Bumps.parse "Updates `axios` from 0.27.2 to 1.1.0" ""

              // Act
              let behind = Bumps.supersededBy [ 131, newer ] older
              let ahead = Bumps.supersededBy [ 124, older ] newer

              // Assert
              Expect.equal behind (Some 131) "the lower target is superseded"
              Expect.equal ahead None "the survivor is not"
          }

          test "leaves PRs on unrelated dependencies alone" {
              // Arrange
              let ours = Bumps.parse "Updates `lodash` from 4.17.20 to 4.17.21" ""
              let theirs = Bumps.parse "Updates `axios` from 0.27.2 to 1.0.0" ""

              // Act & Assert
              Expect.equal (Bumps.supersededBy [ 131, theirs ] ours) None "different dependency"
          }]
