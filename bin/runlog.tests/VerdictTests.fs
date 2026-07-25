/// The one unit-level suite. `parseVerdict` earns it on the strategy's own
/// terms: Domain.fs calls the closed verdict union the reason this tool is F#
/// rather than shell, all three bugs found reviewing the repo lived here, and
/// it is a table — pushing a dozen rows through a process each would be slower
/// and would report failures far less precisely than comparing values.
module VerdictTests

open Expecto
open Domain

/// Rejection is the interesting half, so it gets a name: `None` means the
/// writer refuses the text and no reader will ever see it.
let private isRejected text = parseVerdict text = None

let tests =
    testList
        "parseVerdict"
        [ test "accepts the four verdicts the log is written in" {
              // Arrange
              let cases =
                  [ "clean", Clean
                    "minor: footer placement guessed", Minor "footer placement guessed"
                    "friction: step needed a workaround", Friction "step needed a workaround"
                    "compacted: 974 words", Compacted 974 ]

              // Act & Assert — one table, so the failure names the row
              for text, expected in cases do
                  Expect.equal (parseVerdict text) (Some expected) $"'{text}' should parse"
          }

          test "round-trips every verdict it can render" {
              // Arrange — one of each case; the clause and count are arbitrary
              let verdicts = [ Clean; Minor "a clause"; Friction "a clause"; Compacted 974 ]

              // Act & Assert — a writer the readers disagree with is the whole
              // failure this module exists to prevent
              for verdict in verdicts do
                  Expect.equal (verdict |> renderVerdict |> parseVerdict) (Some verdict) $"{verdict} should survive"
          }

          test "rejects a clause-carrying verdict whose clause is empty" {
              // Arrange
              let cases = [ "minor: "; "friction: "; "minor:"; "friction:" ]

              // Act & Assert
              for text in cases do
                  Expect.isTrue (isRejected text) $"'{text}' carries no clause and should be rejected"
          }

          test "rejects affixes that overlap instead of bracketing a count" {
              // Arrange — starts with "compacted: " and ends with " words", yet
              // is shorter than the two together (cee2377)
              let text = "compacted: words"

              // Act & Assert
              Expect.isTrue (isRejected text) "overlapping affixes should be rejected, not crash"
          }

          test "rejects a word count that could not be a baseline" {
              // Arrange — a negative parsed fine and divided into a nonsense
              // ratio that printed as reassuring news (a1d87eb)
              let cases = [ "compacted: -5 words"; "compacted: 0 words"; "compacted: abc words" ]

              // Act & Assert
              for text in cases do
                  Expect.isTrue (isRejected text) $"'{text}' is not a usable baseline"
          }

          test "rejects prose that merely mentions a verdict" {
              // Arrange — the shell version matched these inside unrelated text
              let cases = [ "the run was clean"; "we compacted: 900 words today"; "bogus" ]

              // Act & Assert
              for text in cases do
                  Expect.isTrue (isRejected text) $"'{text}' is prose, not a verdict"
          } ]
