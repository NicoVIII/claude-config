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
        [ test "creates the run log with its pinned heading when a skill has none" {
              withRoot (fun root ->
                  // Arrange — a skill that has never been run (45ad1bb)
                  root |> skill "demo" "some prose"

                  // Act
                  let result = root |> runlog [ "log"; "demo"; "clean" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"

                  Expect.stringStarts
                      (root |> runsFile "demo" |> Option.defaultValue "")
                      "# Run log\n\n"
                      "the heading is pinned so a created log matches a hand-written one")
          }

          test "records the repo the session ran in, not the skill's directory" {
              withRoot (fun root ->
                  // Arrange
                  root |> skill "demo" "some prose"

                  // Act
                  let result = root |> runlog [ "log"; "demo"; "clean" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"

                  Expect.stringContains
                      (root |> runsFile "demo" |> Option.defaultValue "")
                      $"{today} · {sessionRepoName} · 2 words · clean"
                      "the entry carries today's date, the session repo and the file's size")
          }

          test "appends without disturbing the entries already there" {
              withRoot (fun root ->
                  // Arrange
                  root |> skill "demo" "some prose"
                  root |> logged "demo" [ "friction: something went wrong" ]

                  // Act
                  let result = root |> runlog [ "log"; "demo"; "clean" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  let log = root |> runsFile "demo" |> Option.defaultValue ""
                  Expect.stringContains log "friction: something went wrong" "the existing entry survives"

                  Expect.stringContains
                      log
                      $"{today} · {sessionRepoName} · 2 words · clean"
                      "the new entry is appended")
          }

          test "measures the SKILL.md itself rather than trusting a number it was handed" {
              withRoot (fun root ->
                  // Arrange — the caller is an agent that has just edited the
                  // file, so a count it retypes is a count that can be stale
                  root |> skill "demo" (words 42)

                  // Act
                  let result = root |> runlog [ "log"; "demo"; "friction: something" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"

                  Expect.stringContains
                      (root |> runsFile "demo" |> Option.defaultValue "")
                      "· 42 words · friction: something"
                      "the size is recorded when it is observed, not inferred later")
          }

          test "leaves a compaction's count where its verdict already states it" {
              withRoot (fun root ->
                  // Arrange — recording it twice would only create two places
                  // for the numbers to disagree
                  root |> skill "demo" (words 42)

                  // Act
                  let result = root |> runlog [ "log"; "demo"; "compacted: 42 words" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  let log = root |> runsFile "demo" |> Option.defaultValue ""
                  Expect.stringContains log "· compacted: 42 words" "the baseline reads as it always has"
                  Expect.isFalse (log.Contains "42 words · compacted") "and the count is not restated beside it")
          }

          test "refuses to log a run for a skill with no SKILL.md to measure" {
              withRoot (fun root ->
                  // Arrange
                  root |> bareSkill "demo"

                  // Act
                  let result = root |> runlog [ "log"; "demo"; "clean" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "should fail cleanly rather than record an unmeasured run"
                  Expect.stringContains result.Stderr "no SKILL.md" "the message says what is missing")
          }

          test "rejects a malformed verdict cleanly and leaves the log untouched" {
              withRoot (fun root ->
                  // Arrange — the input that used to abort with a stack trace
                  // and exit 134 instead of being refused (cee2377)
                  root |> skill "demo" "some prose"
                  root |> logged "demo" [ "clean" ]
                  let before = root |> runsFile "demo"

                  // Act
                  let result = root |> runlog [ "log"; "demo"; "compacted: words" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "a refused verdict is an ordinary failure, not a crash"
                  Expect.stringContains result.Stderr "is not a verdict" "the message names the problem"
                  Expect.stringContains result.Stderr "expected:" "and spells out the accepted forms"
                  Expect.equal (root |> runsFile "demo") before "a rejected verdict must not write")
          }

          test "rejects a word count that could not be a baseline" {
              withRoot (fun root ->
                  // Arrange (a1d87eb)
                  root |> skill "demo" "some prose"

                  // Act & Assert
                  for verdict in [ "compacted: -5 words"; "compacted: 0 words" ] do
                      let result = root |> runlog [ "log"; "demo"; verdict ]
                      Expect.equal result.ExitCode 1 $"'{verdict}' should be refused"
                      Expect.isNone (root |> runsFile "demo") "nothing should have been written")
          }

          test "rejects an unknown skill before anything else" {
              withRoot (fun root ->
                  // Arrange — no skill created

                  // Act
                  let result = root |> runlog [ "log"; "ghost"; "clean" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "should fail"
                  Expect.stringContains result.Stderr "no skill named 'ghost'" "the message names the skill")
          } ]

let private maturityTests =
    testList
        "maturity"
        [ test "does not count a compaction baseline as a run" {
              withRoot (fun root ->
                  // Arrange — a log holding only /skill-compact's baseline. It
                  // shares the file but is not a run (a8531b1).
                  root |> skill "demo" (words 100)
                  root |> logged "demo" [ "compacted: 90 words" ]

                  // Act
                  let result = root |> runlog [ "maturity"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "🚧 WIP" "no runs means the lowest rung"
                  Expect.stringContains result.Stdout "0 entries" "the baseline is not an entry")
          }

          test "counts only the entries since the last friction" {
              withRoot (fun root ->
                  // Arrange — friction in the middle ends the streak
                  root |> skill "demo" (words 100)

                  root
                  |> logged "demo" [ "clean"; "friction: broke"; "minor: tweak"; "clean" ]

                  // Act
                  let result = root |> runlog [ "maturity"; "demo" ]

                  // Assert
                  Expect.stringContains result.Stdout "4 entries" "every run counts toward the total"
                  Expect.stringContains result.Stdout "2 since the last friction" "the streak restarts at the friction"
                  Expect.stringContains result.Stdout "1 strictly clean" "the minor is not strictly clean")
          }

          test "reaches Usable at three clean-or-minor entries since the last friction" {
              withRoot (fun root ->
                  // Arrange — a minor does not break the streak
                  root |> skill "demo" (words 100)

                  root
                  |> logged "demo" [ "friction: broke"; "clean"; "minor: tweak"; "clean" ]

                  // Act
                  let result = root |> runlog [ "maturity"; "demo" ]

                  // Assert
                  Expect.stringContains result.Stdout "🟢 Usable" "three since the friction clears the bar")
          }

          test "reaches Battle-tested only on strictly clean entries across repos" {
              withRoot (fun root ->
                  // Arrange — five clean runs spread over two repos
                  root |> skill "demo" (words 100)

                  root
                  |> loggedIn
                      "demo"
                      [ "repo-a", "clean"
                        "repo-a", "clean"
                        "repo-a", "clean"
                        "repo-b", "clean"
                        "repo-b", "clean" ]

                  // Act
                  let result = root |> runlog [ "maturity"; "demo" ]

                  // Assert
                  Expect.stringContains result.Stdout "🛡️ Battle-tested" "five clean across two repos is the top rung"
                  Expect.stringContains result.Stdout "across 2 repos" "the repo count comes from the clean entries")
          }

          test "reports the README's claim beside the rating the log supports" {
              withRoot (fun root ->
                  // Arrange — the README overclaims against an empty log
                  root |> skill "demo" (words 100)
                  root |> listed "demo" "🟢 Usable"

                  // Act
                  let result = root |> runlog [ "maturity"; "demo" ]

                  // Assert — the log wins, but the disagreement is surfaced
                  Expect.stringContains result.Stdout "log supports 🚧 WIP" "the log is the evidence"
                  Expect.stringContains result.Stdout "README says 🟢 Usable" "the claim is quoted back")
          }

          test "says unlisted when the README has no row for the skill" {
              withRoot (fun root ->
                  // Arrange — skill exists, README table left empty
                  root |> skill "demo" (words 100)

                  // Act
                  let result = root |> runlog [ "maturity"; "demo" ]

                  // Assert
                  Expect.stringContains result.Stdout "README says unlisted" "a missing row is not an error")
          }

          test "aborts on a log line no reader can parse" {
              withRoot (fun root ->
                  // Arrange — a dated line with an unknown verdict. Skipping it
                  // would leave the counters quietly wrong.
                  root |> skill "demo" (words 100)
                  root |> logged "demo" [ "sideways" ]

                  // Act
                  let result = root |> runlog [ "maturity"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "a log it cannot read is fatal, not skippable"
                  Expect.stringContains result.Stderr "verdict no reader knows" "the message names the line")
          } ]

let private ratioTests =
    testList
        "ratio"
        [ test "reports growth against the last compaction baseline" {
              withRoot (fun root ->
                  // Arrange — 100 words against a recorded baseline of 50
                  root |> skill "demo" (words 100)
                  root |> logged "demo" [ "compacted: 50 words" ]

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "100 words" "the current size"
                  Expect.stringContains result.Stdout "2.0x" "doubled since the baseline"
                  Expect.stringContains result.Stdout "last compaction" "and says where the baseline came from")
          }

          test "flags a skill over the growth trigger" {
              withRoot (fun root ->
                  // Arrange — 1.6x, just past the 1.5x trigger
                  root |> skill "demo" (words 80)
                  root |> logged "demo" [ "compacted: 50 words" ]

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.stringContains result.Stdout "over the 1.5x trigger" "1.6x is over"
                  Expect.stringContains result.Stdout "/skill-compact" "and names the pass that fixes it")
          }

          test "stays quiet about growth when it has no baseline to compare against" {
              withRoot (fun root ->
                  // Arrange — never compacted, and the root is not a git repo,
                  // so there is no first commit to fall back to either
                  root |> skill "demo" (words 100)

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 0 "a missing baseline is a fact, not a failure"
                  Expect.stringContains result.Stdout "no baseline" "and it says so rather than inventing one")
          }

          test "aborts when the skill has no SKILL.md to measure" {
              withRoot (fun root ->
                  // Arrange — a skill directory and nothing in it (a1d87eb)
                  root |> bareSkill "demo"

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "should fail cleanly rather than crash"
                  Expect.stringContains result.Stderr "no SKILL.md" "the message says what is missing")
          }

          test "reports the risen floor a headline ratio against the last compaction hides" {
              withRoot (fun root ->
                  // Arrange — three cycles ending higher each time, so the
                  // headline reads a reassuring 1.2x while the file is 3x the
                  // smallest it was ever compacted to
                  root |> skill "demo" (words 120)

                  root
                  |> logged
                      "demo"
                      [ "compacted: 40 words"
                        "friction: something"
                        "compacted: 70 words"
                        "friction: something else"
                        "compacted: 100 words" ]

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "1.2x its baseline of 100" "the headline is unchanged"
                  Expect.stringContains result.Stdout "3.0x its lowest baseline of 40" "and the floor is measured too"
                  Expect.stringContains result.Stdout "2026-01-01" "naming the compaction it dates from"
                  Expect.stringContains result.Stdout "risen 60 words over 2 compactions" "with the drift spelled out")
          }

          test "says nothing about the floor while the last compaction is the lowest" {
              withRoot (fun root ->
                  // Arrange — the second cycle landed below the first, so there
                  // is no ratchet and the extra line would be noise
                  root |> skill "demo" (words 120)

                  root
                  |> logged "demo" [ "compacted: 90 words"; "friction: something"; "compacted: 80 words" ]

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "1.5x its baseline of 80" "the headline still reports"
                  Expect.isFalse (result.Stdout.Contains "lowest baseline") "but the floor line stays silent")
          }

          test "measures the floor from compactions only, never from the first draft" {
              withRoot (fun root ->
                  // Arrange — one compaction, so the only smaller number
                  // available is the uncompacted first commit. A first draft is
                  // a size nobody weighed; anchoring to it would hold a skill to
                  // the shape of its first pass forever.
                  root |> skill "demo" (words 120)
                  root |> logged "demo" [ "compacted: 100 words" ]

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.isFalse (result.Stdout.Contains "lowest baseline") "one compaction is no ratchet to report")
          }

          test "counts the rules a skill states, not only the words it spends" {
              withRoot (fun root ->
                  // Arrange — two rules carrying ten words each
                  root |> skill "demo" $"- {words 9}\n- {words 9}\n"
                  root |> logged "demo" [ "compacted: 20 words" ]

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

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
                  |> skill "demo" "- the only rule\n\n```yaml\nsteps:\n  - uses: actions/checkout@v7\n  - uses: devcontainers/ci@v0.3\n```\n"

                  root |> logged "demo" [ "compacted: 10 words" ]

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.stringContains result.Stdout "1 rule of" "the fenced YAML is not three more rules")
          }

          test "separates growth in coverage from growth in prose, against the first commit" {
              withRoot (fun root ->
                  // Arrange — one rule becomes two while the words grow tenfold:
                  // the shape a headline anchored to a later compaction misses
                  root |> skill "demo" "- one\n"
                  root |> committed "demo"
                  root |> skill "demo" $"- one {words 18}\n- two {words 18}\n"

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 0 $"should succeed, said: {result.Stderr}"
                  Expect.stringContains result.Stdout "2 rules at 20 words each" "what it states now"

                  Expect.stringContains
                      result.Stdout
                      "at the first commit, 1 rule of 2 words"
                      "against what it stated to begin with")
          }

          test "reports density even for a skill with no baseline at all" {
              withRoot (fun root ->
                  // Arrange — never compacted, never committed: the case the
                  // headline can say nothing about
                  root |> skill "demo" $"- {words 4}\n"

                  // Act
                  let result = root |> runlog [ "ratio"; "demo" ]

                  // Assert
                  Expect.stringContains result.Stdout "no baseline" "the headline still has nothing to compare"
                  Expect.stringContains result.Stdout "1 rule of 5 words" "but density needs no baseline to be measurable")
          } ]

let private dispatchTests =
    testList
        "dispatch"
        [ test "rejects an unknown command" {
              withRoot (fun root ->
                  // Act
                  let result = root |> runlog [ "summarise"; "demo" ]

                  // Assert
                  Expect.equal result.ExitCode 1 "should fail"
                  Expect.stringContains result.Stderr "unknown command" "the message names the problem")
          }

          test "prints usage when a command is missing its arguments" {
              withRoot (fun root ->
                  // Arrange
                  let cases = [ [ "log" ]; [ "log"; "demo" ]; [ "maturity" ]; [ "ratio" ]; [] ]

                  // Act & Assert
                  for args in cases do
                      let result = root |> runlog args
                      let described = if List.isEmpty args then "no arguments" else String.concat " " args
                      Expect.equal result.ExitCode 1 $"'{described}' should fail"
                      Expect.stringContains result.Stderr "usage:" $"'{described}' should print usage")
          } ]

let tests =
    testList "cli" [ logTests; maturityTests; ratioTests; dispatchTests ]
