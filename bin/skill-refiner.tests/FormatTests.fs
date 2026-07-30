/// The one unit-level suite, covering the log schema in Domain.fs. It earns the
/// exception on the strategy's own terms: Domain.fs calls the closed event tree
/// the reason this tool is F# rather than shell, all three bugs found reviewing
/// the repo lived here, and it is a table — pushing a dozen rows through a
/// process each would be slower and would report failures far less precisely
/// than comparing values.
module FormatTests

open System
open Expecto
open Domain

/// Rejection is the interesting half, so it gets a name: `None` means the writer
/// refuses the text and no reader will ever see it.
let private isRejected text = parseChangeEvent text = None

let tests =
    testList
        "parseChangeEvent"
        [ test "accepts every event the log is written in" {
              // Arrange
              let cases =
                  [ "retro clean", Retro Clean
                    "retro minor: footer placement guessed", Retro(Minor "footer placement guessed")
                    "retro major: the run was aborted", Retro(Major "the run was aborted")
                    "fix small: stale path corrected", Fix(Small, "stale path corrected")
                    "fix big: the ladder rung was replaced", Fix(Big, "the ladder rung was replaced")
                    "compacted: merged the retraction rules", Compacted "merged the retraction rules" ]

              // Act & Assert — one table, so the failure names the row
              for text, expected in cases do
                  Expect.equal (parseChangeEvent text) (Some expected) $"'{text}' should parse"
          }

          test "round-trips every event it can render" {
              // Arrange — one of each case; the clause is arbitrary
              let events =
                  [ Retro Clean
                    Retro(Minor "a clause")
                    Retro(Major "a clause")
                    Fix(Small, "a clause")
                    Fix(Big, "a clause")
                    Compacted "a clause" ]

              // Act & Assert — a writer the readers disagree with is the whole
              // failure this module exists to prevent
              for event in events do
                  Expect.equal (event |> renderChangeEvent |> parseChangeEvent) (Some event) $"{event} should survive"
          }

          test "rejects a clause-carrying event whose clause is empty" {
              // Arrange
              let cases =
                  [ "retro minor: "
                    "retro major: "
                    "fix small: "
                    "fix big: "
                    "compacted: "
                    "retro minor:"
                    "fix big:" ]

              // Act & Assert
              for text in cases do
                  Expect.isTrue (isRejected text) $"'{text}' carries no clause and should be rejected"
          }

          test "rejects a grade outside the vocabulary" {
              // Arrange — `friction` and `deferred` were the run log's words and
              // are not this log's; a stale one must not parse as anything
              let cases =
                  [ "friction: something"
                    "deferred: something"
                    "retro friction: something"
                    "fix medium: something" ]

              // Act & Assert
              for text in cases do
                  Expect.isTrue (isRejected text) $"'{text}' is not an event this log has"
          }

          test "rejects prose that merely mentions an event" {
              // Arrange — the shell version matched these inside unrelated text
              let cases = [ "the run was clean"; "retro clean, mostly"; "bogus" ]

              // Act & Assert
              for text in cases do
                  Expect.isTrue (isRejected text) $"'{text}' is prose, not an event"
          } ]

/// The writer's own gate. Every clause reaching the log passes through here, so
/// what it lets by is exactly what a line of the log can hold.
let clauseTests =
    testList
        "clause"
        [ test "normalises the clause it is handed" {
              // Act & Assert
              Expect.equal (clause "  a clause  ") "a clause" "surrounding space is not part of what was said"
          }

          test "refuses a clause that could not survive one line of the log" {
              // Arrange — a blank one parses back as no entry at all, and a
              // newline would write a second line no reader can parse
              let cases = [ ""; "   "; "two\nlines"; "carriage\rreturn" ]

              // Act & Assert
              for text in cases do
                  Expect.throwsT<SkillRefinerFailure> (fun () -> clause text |> ignore) $"'{text}' should be refused"
          } ]

/// `parseLine` is the other half of the schema, and its failure mode is worse
/// than a rejection: a line it cannot read aborts the whole file, so one bad
/// entry blinds every reader of that skill's log rather than being skipped.
let lineTests =
    testList
        "parseLine"
        [ test "reads all four fields of an entry" {
              // Arrange
              let line = "2026-07-25 · claude-config · 912 words · retro minor: a clause"

              // Act
              let entry = parseLine line

              // Assert
              Expect.equal
                  entry
                  { Date = DateOnly(2026, 7, 25)
                    Repo = "claude-config"
                    Words = 912
                    Event = Change(Retro(Minor "a clause")) }
                  "the fields should survive the round trip"
          }

          test "round-trips an entry through render and back" {
              // Arrange
              let entries =
                  [ { Date = DateOnly(2026, 7, 25)
                      Repo = "claude-config"
                      Words = 912
                      Event = Change(Fix(Big, "a clause")) }
                    { Date = DateOnly(2026, 7, 25)
                      Repo = "claude-config"
                      Words = 974
                      Event = Creation CreationEvent } ]

              // Act & Assert
              for entry in entries do
                  Expect.equal (entry |> renderKnownEntry |> parseLine) entry $"{entry} should survive"
          }

          test "keeps a separator inside the clause out of the field split" {
              // Arrange — the clause is free text, so a retro can write the
              // separator into it; an unbounded split made that abort the file
              let line = "2026-07-25 · claude-config · 912 words · retro major: chose reword · not removal"

              // Act
              let entry = parseLine line

              // Assert — the clause keeps the rest of the line, whole
              Expect.equal
                  entry.Event
                  (Change(Retro(Major "chose reword · not removal")))
                  "the separator belongs to the clause, not to a fifth field"
          }

          test "refuses the three-field entries of the run log this replaces" {
              // Arrange — RUNS.md lines carried no word count. Reading one as an
              // entry would leave a hole in the size series the ratio reads.
              let line = "2026-07-25 · claude-config · minor: a clause"

              // Act & Assert
              Expect.throwsT<SkillRefinerFailure>
                  (fun () -> parseLine line |> ignore)
                  "a line without a count is not an entry of this log"
          }

          test "aborts on a line it cannot read" {
              // Arrange — silently skipping these is what leaves the counters
              // wrong in the way that is hardest to notice. A count of zero or
              // less is not a size: a negative used to divide into a ratio that
              // printed as reassuring news (a1d87eb).
              let cases =
                  [ "2026-07-25 · claude-config"
                    "2026-13-99 · repo-a · 5 words · retro clean"
                    "2026-07-25 · repo-a · lots words · retro clean"
                    "2026-07-25 · repo-a · 0 words · retro clean"
                    "2026-07-25 · repo-a · -5 words · retro clean"
                    "2026-07-25 · repo-a · 5 words · sideways" ]

              // Act & Assert
              for line in cases do
                  Expect.throwsT<SkillRefinerFailure> (fun () -> parseLine line |> ignore) $"'{line}' should abort"
          } ]

/// The file as a whole: the shape rule that creation is the first line or
/// nowhere, settled once here so no fold over the changes has to re-check it.
let historyTests =
    testList
        "parseHistory"
        [ test "reads the origin baseline out of the first line" {
              // Arrange
              let text =
                  "# Skill History\n\n2026-01-01 · repo-a · 500 words · created\n2026-01-02 · repo-a · 520 words · retro clean\n"

              // Act
              let history = parseHistory text

              // Assert
              Expect.equal
                  (history.Creation |> Option.map (fun entry -> entry.Words))
                  (Some 500)
                  "the creation baseline is the size the log began at"

              Expect.equal (List.length history.Entries) 1 "and it is not one of the changes after it"
          }

          test "tolerates a log that never recorded a creation" {
              // Arrange — a skill whose log predates creation seeding
              let text = "# Skill History\n\n2026-01-01 · repo-a · 500 words · retro clean\n"

              // Act
              let history = parseHistory text

              // Assert
              Expect.isNone history.Creation "the baseline is absent, not invented"
              Expect.equal (List.length history.Entries) 1 "and the entry still reads"
          }

          test "aborts when created is not the first line" {
              // Arrange — creation is the origin baseline, so a later one is
              // either a mistake or a second origin, and neither has a meaning
              let text =
                  "# Skill History\n\n2026-01-01 · repo-a · 500 words · retro clean\n2026-01-02 · repo-a · 520 words · created\n"

              // Act & Assert
              Expect.throwsT<SkillRefinerFailure>
                  (fun () -> parseHistory text |> ignore)
                  "a creation out of first position should abort"
          }

          test "aborts on a heading it did not write" {
              // Arrange — the run log this replaces, handed over unconverted.
              // Skipping two lines blind would read its entries as this log's.
              let text = "# Run log\n\n2026-01-01 · repo-a · clean\n"

              // Act & Assert
              Expect.throwsT<SkillRefinerFailure>
                  (fun () -> parseHistory text |> ignore)
                  "a file this tool did not write is not a history"
          }

          test "skips blank lines and nothing else" {
              // Arrange — trailing newlines are ordinary; prose is not
              let blanks = "# Skill History\n\n2026-01-01 · repo-a · 500 words · created\n\n\n"

              let prose =
                  "# Skill History\n\n2026-01-01 · repo-a · 500 words · created\na note somebody added\n"

              // Act & Assert
              Expect.isSome (parseHistory blanks).Creation "blank lines are not entries"

              Expect.throwsT<SkillRefinerFailure>
                  (fun () -> parseHistory prose |> ignore)
                  "but anything else in a tool-owned file is a broken entry"
          } ]
