# Skill History

2026-07-30 · claude-config · 1008 words · created
2026-09-13 · claude-config · 1001 words · retro minor: no edits; deferred first sightings: repo-wide real-test-suite verdict ignores per-package coverage (#80 CI-unexercised dep marked safe), test-runner detection misses task-runner wrappers (just recipe), empty unstick batch unhandled
2026-09-20 · tcg-card-collector · 1003 words · retro minor: three deferred mechanisms all recurred: task-runner step hides the real test runner, repo-wide test verdict overrides per-package coverage, empty unstick batch read as a question; plus dotnet-tool.sh build banner ahead of every survey; deferred first sighting: rule 6 flags major even where the bumped package is the tool CI itself runs
2026-09-20 · tcg-card-collector · 1122 words · fix big: resolve task-runner steps to the runner underneath; safe verdicts name what verified them, since green CI only covers packages CI runs; empty unstick batch ends without a question; dotnet-tool.sh captures the build so no banner precedes tool output
2026-10-01 · tcg-card-collector · 1122 words · retro minor: rule 6 flagged #146 major though its only major member (oxfmt pre-1.0 minor) is the formatter CI runs, second sighting; deferred: none (workflow-read cost declined, grep hides multi-line run blocks)
2026-10-01 · tcg-card-collector · 1133 words · fix big: rule 6: a major whose every major member is a formatter or linter a green CI step invokes falls through as CI runs it directly; dropped footer on reaching Usable
