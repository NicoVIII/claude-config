/// Integration: every test here drives the real binary against a throwaway
/// config root. These cover the seams — dispatch, file creation, the abort
/// paths, the counting the README quotes — which is where this tool's bugs have
/// actually been. Where a test names a commit, that commit is the regression it
/// pins.
module CliTests

open System
open Expecto
open Harness

let private today = DateTime.Now.ToString "yyyy-MM-dd"

let private logTests =
    testList
        "log"
        [ test "opens the history with its pinned heading and the origin baseline" {
              withRoot (fun root ->
                  // Arrange — a skill that has just been written (45ad1bb)
                  root |> skill "demo" (words 42)

                  // Act
                  let result = root |> skillRefiner [ "demo"; "log"; "creation" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  let log = root |> historyFile "demo" |> Option.defaultValue ""

                  Expect.stringStarts
                      log
                      "# Skill History\n\n"
                      "the heading is pinned so a created log matches a hand-written one"

                  Expect.stringContains
                      log
                      $"{today} · {sessionRepoName} · 42 words · created"
                      "and the first line is the size the log begins at")
          }

          test "refuses a second creation rather than writing a second origin" {
              withRoot (fun root ->
                  // Arrange — creation is the first line or nowhere, so a later
                  // one is a shape the reader would reject anyway
                  root |> skill "demo" (words 42)
                  root |> skillRefiner [ "demo"; "log"; "creation" ] |> ignore
                  let before = root |> historyFile "demo"

                  // Act
                  let result = root |> skillRefiner [ "demo"; "log"; "creation" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "the second creation should be refused"
                  Expect.stringContains result.Stderr "already exists" "the message says why"
                  Expect.equal (root |> historyFile "demo") before "and the log is untouched")
          }

          test "records the repo the session ran in, not the skill's directory" {
              withRoot (fun root ->
                  // Arrange
                  root |> skill "demo" "some prose"

                  // Act
                  let result = root |> skillRefiner [ "demo"; "log"; "retro"; "clean" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"

                  Expect.stringContains
                      (root |> historyFile "demo" |> Option.defaultValue "")
                      $"{today} · {sessionRepoName} · 2 words · retro clean"
                      "the entry carries today's date, the session repo and the file's size")
          }

          test "appends without disturbing the entries already there" {
              withRoot (fun root ->
                  // Arrange
                  root |> skill "demo" "some prose"
                  root |> logged "demo" [ "retro major: something went wrong" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "log"; "fix"; "big"; "replaced the ladder" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  let log = root |> historyFile "demo" |> Option.defaultValue ""
                  Expect.stringContains log "retro major: something went wrong" "the existing entry survives"
                  Expect.stringContains log "· 2 words · fix big: replaced the ladder" "the new entry is appended")
          }

          test "writes the retro and the fix of one pass as two entries" {
              withRoot (fun root ->
                  // Arrange — the pairing is what makes an unfixed finding
                  // visible: a clause with no fix beside it is the deferral
                  root |> skill "demo" (words 10)

                  // Act — the retro is logged against the size the run ran at,
                  // the fix against the size the edit left behind
                  root
                  |> skillRefiner [ "demo"; "log"; "retro"; "minor"; "footer placement guessed" ]
                  |> ignore

                  root |> skill "demo" (words 14)

                  let result =
                      root |> skillRefiner [ "demo"; "log"; "fix"; "small"; "footer placement pinned" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  let log = root |> historyFile "demo" |> Option.defaultValue ""
                  Expect.stringContains log "· 10 words · retro minor: footer placement guessed" "the run's own size"
                  Expect.stringContains log "· 14 words · fix small: footer placement pinned" "and the edit's")
          }

          test "measures the SKILL.md itself rather than trusting a number it was handed" {
              withRoot (fun root ->
                  // Arrange — the caller is an agent that has just edited the
                  // file, so a count it retypes is a count that can be stale.
                  // The baseline every later ratio is read against is the last
                  // number that should arrive that way.
                  root |> skill "demo" (words 37)

                  // Act
                  let result = root |> skillRefiner [ "demo"; "log"; "compacted"; "cut the ladder" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"

                  Expect.stringContains
                      (root |> historyFile "demo" |> Option.defaultValue "")
                      "· 37 words · compacted: cut the ladder"
                      "the size is recorded when it is observed, not retyped")
          }

          test "refuses to log a run for a skill with no SKILL.md to measure" {
              withRoot (fun root ->
                  // Arrange
                  root |> bareSkill "demo"

                  // Act
                  let result = root |> skillRefiner [ "demo"; "log"; "retro"; "clean" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "should fail cleanly rather than record an unmeasured run"
                  Expect.stringContains result.Stderr "no SKILL.md" "the message says what is missing")
          }

          test "rejects a grade the vocabulary does not have, and names the ones it does" {
              withRoot (fun root ->
                  // Arrange — `friction` was the run log's word for it, and a
                  // stale caller must be told so rather than silently logged
                  root |> skill "demo" "some prose"

                  // Act
                  let result = root |> skillRefiner [ "demo"; "log"; "retro"; "friction"; "a clause" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "an unknown grade is refused"
                  Expect.stringContains result.Stderr "usage:" "the message spells out the accepted forms"
                  Expect.stringContains result.Stderr "minor" "naming the grades that exist"
                  Expect.isNone (root |> historyFile "demo") "and nothing is written")
          }

          test "rejects an empty clause and leaves the log untouched" {
              withRoot (fun root ->
                  // Arrange — an entry saying nothing is one a later retro
                  // cannot match a recurrence against
                  root |> skill "demo" "some prose"

                  // Act
                  let result = root |> skillRefiner [ "demo"; "log"; "retro"; "minor"; "   " ]

                  // Assert
                  Expect.equal result.ExitCode 1 "a blank clause is refused"
                  Expect.stringContains result.Stderr "clause is empty" "the message names the problem"
                  Expect.isNone (root |> historyFile "demo") "and nothing is written")
          }

          test "rejects an unknown skill before anything else" {
              withRoot (fun root ->
                  // Arrange — no skill created

                  // Act
                  let result = root |> skillRefiner [ "ghost"; "log"; "retro"; "clean" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "should fail"
                  Expect.stringContains result.Stderr "no skill named 'ghost'" "the message names the skill")
          } ]

let private maturityTests =
    testList
        "maturity"
        [ test "counts neither a creation, an edit nor a compaction as a run" {
              withRoot (fun root ->
                  // Arrange — all three share the log without being runs.
                  // Counting one as a run is the bug a8531b1 had to patch out of
                  // the shell version.
                  root |> skill "demo" (words 100)

                  root
                  |> logged "demo" [ "created"; "fix big: replaced the ladder"; "compacted: cut it back" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "log holds no runs" "none of the three is one"
                  Expect.stringContains result.Stdout "0 runs" "and the count agrees"

                  Expect.stringContains
                      result.Stdout
                      "🧪 Experimental needs one run logged"
                      "the bar to clear is still named")
          }

          test "counts only the runs since the last major retro" {
              withRoot (fun root ->
                  // Arrange — a major retro in the middle ends the streak
                  root |> skill "demo" (words 100)

                  root
                  |> logged "demo" [ "retro clean"; "retro major: broke"; "retro minor: tweak"; "retro clean" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert
                  Expect.stringContains result.Stdout "4 runs" "every run counts toward the total"
                  Expect.stringContains result.Stdout "2 clean-or-minor" "the streak restarts at the major"
                  Expect.stringContains result.Stdout "1 strictly clean" "the minor is not strictly clean")
          }

          test "a big fix ends the streak the runs before it built" {
              withRoot (fun root ->
                  // Arrange — the edit replaced a mechanism, so the runs before
                  // it no longer vouch for what runs now. This is the judgement
                  // /skill-retro used to leave to its reader.
                  root |> skill "demo" (words 100)

                  root
                  |> logged "demo" [ "retro clean"; "retro clean"; "fix big: replaced the ladder"; "retro clean" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert
                  Expect.stringContains result.Stdout "3 runs" "the runs themselves are still on record"
                  Expect.stringContains result.Stdout "1 clean-or-minor" "but only the one since the fix counts")
          }

          test "a small fix leaves the streak standing and still ends the spotless one" {
              withRoot (fun root ->
                  // Arrange — the procedure is intact, so earlier runs still
                  // vouch for it; the top rung is stricter and wants five clean
                  // runs against text nobody has had to touch
                  root |> skill "demo" (words 100)

                  root
                  |> logged "demo" [ "retro clean"; "fix small: stale path corrected"; "retro clean" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert
                  Expect.stringContains result.Stdout "2 clean-or-minor" "the small fix is transparent to this streak"
                  Expect.stringContains result.Stdout "1 strictly clean" "but not to the one above it")
          }

          test "reaches Usable at three clean-or-minor runs since the last reset" {
              withRoot (fun root ->
                  // Arrange — a minor does not break the streak
                  root |> skill "demo" (words 100)

                  root
                  |> logged "demo" [ "retro major: broke"; "retro clean"; "retro minor: tweak"; "retro clean" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert
                  Expect.stringContains result.Stdout "🟢 Usable" "three since the major clears the bar")
          }

          test "reaches Battle-tested only on strictly clean runs across repos" {
              withRoot (fun root ->
                  // Arrange — five clean runs spread over two repos
                  root |> skill "demo" (words 100)

                  root
                  |> loggedIn
                      "demo"
                      [ "repo-a", "retro clean"
                        "repo-a", "retro clean"
                        "repo-a", "retro clean"
                        "repo-b", "retro clean"
                        "repo-b", "retro clean" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert
                  Expect.stringContains result.Stdout "🛡️ Battle-tested" "five clean across two repos is the top rung"
                  Expect.stringContains result.Stdout "across 2 repos" "the repo count comes from the clean runs")
          }

          test "reports the README's claim beside the rating the log supports" {
              withRoot (fun root ->
                  // Arrange — the README overclaims against runs that back a
                  // lower rung
                  root |> skill "demo" (words 100)
                  root |> listed "demo" "🟢 Usable"
                  root |> logged "demo" [ "retro major: aborted" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert — the log wins, but the disagreement is surfaced
                  Expect.stringContains result.Stdout "log supports 🧪 Experimental" "the log is the evidence"
                  Expect.stringContains result.Stdout "README says 🟢 Usable" "the claim is quoted back")
          }

          test "does not read a claim as contradicted by a log with no runs" {
              withRoot (fun root ->
                  // Arrange — the shape e976e0c left behind: a row nobody
                  // demoted, above a log whose run evidence was discarded. The
                  // rung is absent from the output on purpose — printing it here
                  // is what made every retro re-propose the same demotion.
                  root |> skill "demo" (words 100)
                  root |> listed "demo" "🧪 Experimental"
                  root |> logged "demo" [ "created" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert
                  Expect.stringContains result.Stdout "README says 🧪 Experimental" "the claim is still quoted back"
                  Expect.stringContains result.Stdout "leave the row alone" "with no runs it is unbacked, not wrong"
                  Expect.isFalse (result.Stdout.Contains "log supports") "nothing is claimed against it")
          }

          test "says unlisted when the README has no row for the skill" {
              withRoot (fun root ->
                  // Arrange — skill exists, README table left empty
                  root |> skill "demo" (words 100)

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert
                  Expect.stringContains result.Stdout "README says unlisted" "a missing row is not an error")
          }

          test "aborts on a log line no reader can parse" {
              withRoot (fun root ->
                  // Arrange — a dated line with an unknown event. Skipping it
                  // would leave the counters quietly wrong.
                  root |> skill "demo" (words 100)
                  root |> logged "demo" [ "sideways" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "maturity" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "a log it cannot read is fatal, not skippable"
                  Expect.stringContains result.Stderr "event no reader knows" "the message names the line")
          } ]

let private ratioTests =
    testList
        "ratio"
        [ test "reports growth against the last compaction baseline" {
              withRoot (fun root ->
                  // Arrange — 100 words against a recorded baseline of 50
                  root |> skill "demo" (words 100)
                  root |> loggedSized "demo" [ 50, "compacted: cut it back" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "100 words" "the current size"
                  Expect.stringContains result.Stdout "2.0x" "doubled since the baseline"
                  Expect.stringContains result.Stdout "last compaction" "and says where the baseline came from")
          }

          test "falls back to the creation baseline for a skill never compacted" {
              withRoot (fun root ->
                  // Arrange — the size its author settled on, which is what
                  // makes a first draft an anchor here where a first commit was
                  // not one
                  root |> skill "demo" (words 100)
                  root |> loggedSized "demo" [ 50, "created"; 80, "retro clean" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.stringContains result.Stdout "2.0x its baseline of 50 (creation)" "creation is the anchor")
          }

          test "flags a skill over the growth trigger" {
              withRoot (fun root ->
                  // Arrange — 1.6x, just past the 1.5x trigger
                  root |> skill "demo" (words 80)
                  root |> loggedSized "demo" [ 50, "compacted: cut it back" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.stringContains result.Stdout "over the 1.5x trigger" "1.6x is over"
                  Expect.stringContains result.Stdout "/skill-compact" "and names the pass that fixes it")
          }

          test "prints the growth trace with each move and the clause explaining it" {
              withRoot (fun root ->
                  // Arrange — the curve the headline compresses into one number
                  root |> skill "demo" (words 120)

                  root
                  |> loggedSized
                      "demo"
                      [ 100, "created"
                        120, "retro minor: footer placement guessed"
                        90, "compacted: merged the retraction rules" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.stringContains result.Stdout "growth trace:" "the trace is labelled"
                  Expect.stringContains result.Stdout "created · 100 words" "the origin carries no delta"

                  Expect.stringContains
                      result.Stdout
                      "retro minor · 120 words (+20) — footer placement guessed"
                      "a rise is attributed to the entry whose clause explains it"

                  Expect.stringContains
                      result.Stdout
                      "compacted · 90 words (-30) — merged the retraction rules"
                      "and so is a fall")
          }

          test "reports the risen floor a headline against the last compaction hides" {
              withRoot (fun root ->
                  // Arrange — three cycles ending higher each time, so the
                  // headline reads a reassuring 1.2x while the file is 3x the
                  // smallest it was ever compacted to
                  root |> skill "demo" (words 120)

                  root
                  |> loggedSized
                      "demo"
                      [ 40, "compacted: first pass"
                        60, "retro major: something"
                        70, "compacted: second pass"
                        90, "retro major: something else"
                        100, "compacted: third pass" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "1.2x its baseline of 100" "the headline is unchanged"
                  Expect.stringContains result.Stdout "3.0x its lowest baseline of 40" "and the floor is measured too"
                  Expect.stringContains result.Stdout "2026-01-01" "naming the cycle it dates from"
                  Expect.stringContains result.Stdout "risen 60 words over 2 cycles" "with the drift spelled out")
          }

          test "says nothing about the floor while the last deliberate size is the lowest" {
              withRoot (fun root ->
                  // Arrange — the second cycle landed below the first, so there
                  // is no ratchet and the extra line would be noise
                  root |> skill "demo" (words 120)

                  root
                  |> loggedSized "demo" [ 90, "compacted: first pass"; 80, "compacted: second pass" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "1.5x its baseline of 80" "the headline still reports"
                  Expect.isFalse (result.Stdout.Contains "lowest baseline") "but the floor line stays silent")
          }

          test "stays quiet about growth when it has no baseline to compare against" {
              withRoot (fun root ->
                  // Arrange — never logged, and nothing outside the log is
                  // consulted to invent one
                  root |> skill "demo" $"- {words 4}\n"

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.equal result.ExitCode 0 "a missing baseline is a fact, not a failure"
                  Expect.stringContains result.Stdout "no baseline" "and it says so rather than inventing one"
                  Expect.stringContains result.Stdout "1 rule of 5 words" "density needs no baseline to be measurable")
          }

          test "counts the rules a skill states, not only the words it spends" {
              withRoot (fun root ->
                  // Arrange — two rules carrying ten words each
                  root |> skill "demo" $"- {words 9}\n- {words 9}\n"
                  root |> loggedSized "demo" [ 20, "compacted: cut it back" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "2 rules at 10 words each" "the second axis, alongside the words")
          }

          test "skips fenced code, whose lines are list items by shape alone" {
              withRoot (fun root ->
                  // Arrange — one rule, plus a workflow snippet whose YAML
                  // sequence entries are indistinguishable from bullets
                  // (add-devcontainer embeds exactly this)
                  root
                  |> skill
                      "demo"
                      "- the only rule\n\n```yaml\nsteps:\n  - uses: actions/checkout@v7\n  - uses: devcontainers/ci@v0.3\n```\n"

                  root |> loggedSized "demo" [ 10, "compacted: cut it back" ]

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.stringContains result.Stdout "1 rule of" "the fenced YAML is not three more rules")
          }

          test "aborts when the skill has no SKILL.md to measure" {
              withRoot (fun root ->
                  // Arrange — a skill directory and nothing in it (a1d87eb)
                  root |> bareSkill "demo"

                  // Act
                  let result = root |> skillRefiner [ "demo"; "ratio" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "should fail cleanly rather than crash"
                  Expect.stringContains result.Stderr "no SKILL.md" "the message says what is missing")
          } ]

let private dispatchTests =
    testList
        "dispatch"
        [ test "rejects an unknown command" {
              withRoot (fun root ->
                  // Arrange
                  root |> skill "demo" "some prose"

                  // Act
                  let result = root |> skillRefiner [ "demo"; "summarise" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "should fail"
                  Expect.stringContains result.Stderr "unknown command" "the message names the problem")
          }

          test "prints usage when a command is missing its arguments" {
              withRoot (fun root ->
                  // Arrange
                  root |> skill "demo" "some prose"

                  let cases =
                      [ []
                        [ "demo" ]
                        [ "demo"; "log" ]
                        [ "demo"; "log"; "retro" ]
                        [ "demo"; "log"; "retro"; "minor" ]
                        [ "demo"; "log"; "fix"; "small" ]
                        [ "demo"; "log"; "compacted" ]
                        [ "demo"; "maturity"; "extra" ] ]

                  // Act & Assert
                  for args in cases do
                      let result = root |> skillRefiner args
                      let described = if List.isEmpty args then "no arguments" else String.concat " " args
                      Expect.equal result.ExitCode 1 $"'{described}' should fail"
                      Expect.stringContains result.Stderr "usage:" $"'{described}' should print usage")
          } ]

let tests =
    testList "cli" [ logTests; maturityTests; ratioTests; dispatchTests ]
