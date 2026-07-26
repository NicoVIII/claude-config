/// The one unit-level suite, covering the log schema in Domain.fs. It earns the
/// exception on the strategy's own terms: Domain.fs calls the closed verdict
/// union the reason this tool is F# rather than shell, all three bugs found
/// reviewing the repo lived here, and it is a table — pushing a dozen rows
/// through a process each would be slower and would report failures far less
/// precisely than comparing values.
module VerdictTests

open System
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

/// `parseLine` is the other half of the schema, and its failure mode is worse
/// than a rejection: a line it cannot read aborts the whole file, so one bad
/// entry blinds every reader of that skill's log rather than being skipped.
let lineTests =
    testList
        "parseLine"
        [ test "reads the three-field entries written before the count existed" {
              // Arrange — every entry logged up to 2026-07-26 has this shape;
              // rewriting them to add a field would invent a measurement
              // nobody took, so both arities stay readable forever
              let line = "2026-07-25 · claude-config · minor: a clause"

              // Act
              let entry = parseLine line

              // Assert
              Expect.equal
                  entry
                  (Some
                      { Date = DateOnly(2026, 7, 25)
                        Repo = "claude-config"
                        Words = None
                        Verdict = Minor "a clause" })
                  "the fields should survive the round trip, with no count claimed"
          }

          test "reads the recorded word count when an entry carries one" {
              // Arrange
              let line = "2026-07-25 · claude-config · 912 words · minor: a clause"

              // Act
              let entry = parseLine line

              // Assert
              Expect.equal
                  entry
                  (Some
                      { Date = DateOnly(2026, 7, 25)
                        Repo = "claude-config"
                        Words = Some 912
                        Verdict = Minor "a clause" })
                  "the count is a field of its own, not part of the clause"
          }

          test "round-trips an entry through render and back" {
              // Arrange — a writer the readers disagree with is the failure this
              // module exists to prevent, and the count doubled the ways to
              // disagree
              let entries =
                  [ { Date = DateOnly(2026, 7, 25)
                      Repo = "claude-config"
                      Words = Some 912
                      Verdict = Friction "a clause" }
                    { Date = DateOnly(2026, 7, 25)
                      Repo = "claude-config"
                      Words = None
                      Verdict = Compacted 974 } ]

              // Act & Assert
              for entry in entries do
                  Expect.equal (entry |> render |> parseLine) (Some entry) $"{entry} should survive"
          }

          test "keeps a separator inside the clause out of the field split" {
              // Arrange — the clause is free text, so a retro can write the
              // separator into it; an unbounded split made that abort the file
              let line = "2026-07-25 · claude-config · friction: chose reword · not removal"

              // Act
              let entry = parseLine line

              // Assert — the clause keeps the rest of the line, whole
              Expect.equal
                  (entry |> Option.map (fun entry -> entry.Verdict))
                  (Some(Friction "chose reword · not removal"))
                  "the separator belongs to the clause, not to a fourth field"
          }

          test "ignores the lines a log holds that are not entries" {
              // Arrange
              let cases = [ "# Run log"; ""; "some prose about a clean run" ]

              // Act & Assert
              for line in cases do
                  Expect.equal (parseLine line) None $"'{line}' is not an entry"
          }

          test "aborts on a line that opens like an entry but is not one" {
              // Arrange — silently skipping these is what leaves the counters
              // wrong in the way that is hardest to notice
              let cases =
                  [ "2026-07-25 · claude-config"
                    "2026-07-25 · claude-config · bogus"
                    "2026-07-25 · claude-config · compacted: 0 words" ]

              // Act & Assert
              for line in cases do
                  Expect.throwsT<RunlogFailure> (fun () -> parseLine line |> ignore) $"'{line}' should abort"
          } ]
