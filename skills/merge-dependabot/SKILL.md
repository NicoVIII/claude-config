---
name: merge-dependabot
description: Assess and safely merge Dependabot PRs in the current repo — merge bumps that green CI with a real test suite actually verifies, flag the rest with test or manual-verification guidance. Use when I ask to merge/triage Dependabot PRs, clear dependency bumps, or handle bot dependency updates in this repo.
---

Assess the current repo's open Dependabot PRs, merge the ones a real test suite actually verifies, and flag the rest with a way to make them safe.

Scope is the current repo only. `prioritize` finds *which* repos have bumps waiting; this handles *one* repo's bumps in depth — run it after you've cd'd into the target.

## Gather

Run `dotnet run --project ~/.claude/skills/merge-dependabot/scripts/survey` — an F# program. It prints the repo's merge method, then a labelled block per open Dependabot PR: `LEVEL` (worst member wins across a grouped PR; a pre-1.0 minor counts as major), `CI`, `MERGE`, `FILES`, a `bump:` line per dependency, `superseded-by:` where another open PR takes the same dependency further, and `notes:` links. No open PRs → it says so, and you stop.

If it fails, fix the cause — don't fall back to hand-rolled `gh` queries, since the point is that every run classifies on the same facts.

The one judgement it deliberately leaves to you, decided once for the whole repo: **does CI run a real test suite?** Read `.github/workflows/*.{yml,yaml}` and look for a genuine test-runner step (`npm test`, `pytest`, `jest`, `vitest`, `go test`, `cargo test`, `dotnet test`, `mvn test`, …) — *not* lint / typecheck / build / format alone. This is the load-bearing check: if no workflow runs tests, green CI proves nothing and **every** bump is flagged as unverified.

## Classify

Per PR, in order — first match wins:

1. `FILES=lock-only` → a stale refresh, not the bump the body advertises. Report it as one — `superseded-by` usually names its replacement — and don't merge.
2. `CI=red` → `✗ red CI`. Name the failing check and a one-line excerpt (`gh run view <run-id> --log-failed`; expired logs 410, which Unstick answers). A broken bump needs code changes, not tests — out of scope here; flag and move on.
3. `CI=pending` → `⏳ pending` — skip this run, no verdict yet.
4. **No real test suite** (from Gather) → `⚠ unverified` — green CI is meaningless without tests.
5. `MERGE=dirty` → `⚠ needs rebase` — note `@dependabot rebase`; do not merge. `MERGE=blocked` means branch protection will refuse the merge — say so rather than trying.
6. `LEVEL=major` or `LEVEL=unclear` → `⚠ major` — breaking by design, and tests rarely cover intentional breakage.
7. **Contradicts repo policy** → `⚠ policy` — even green + minor. Read `.github/dependabot.yml` if present: a bump matching an `ignore` rule shouldn't merge (likely a config gap — offer to close it). For a **library**, also be wary of bumps that raise a dependency floor consumers must match (target framework, `FSharp.Core`, a declared minimum) — the library should keep working against the *old* version, so verify compatibility instead of bumping. Flag; do not merge.
8. Otherwise → `✓ safe`.

## Present & merge

List safe PRs, then flagged PRs grouped by reason. Ask **once** to merge the safe batch. If no PR is safe, say so, skip the merge ask, and go straight to the flagged report and the unstick confirmation — one question total. On confirmation, merge each with the repo's method and delete the branch: `gh pr merge <n> --squash --delete-branch` (swap `--squash` for `--merge`/`--rebase` per the survey). Never touch a flagged PR.

For each **flagged** PR, make it actionable — state the reason, then:

- **major / unverified:** grep the repo for the package's import/use sites (`rg <package>`), pass on the survey's `notes:` links, and give a targeted manual-test suggestion for those sites plus what a covering test would assert. Do **not** write test files — this is guidance; `/verify-bump <n>` is the skill that actually runs it.
- **red CI:** the failing check + excerpt is enough; the fix is code, which you'd start deliberately.
- **needs rebase / pending / stale refresh:** the one-line note is enough.
- **policy:** say which rule it hits and offer to close it (`gh pr close <n> --comment ...`); if it exposes a config gap — an ignore rule that should exist but doesn't — say so.

Anything written to GitHub (comments, reviews) ends with "— written by an agent"; merges and branch deletions carry their own authorship and need no marker.

Keep it dense — one verdict block per PR, no padding. Example:

```
✓ safe (3) — merge these?
  jest 29.6→29.7 · lodash 4.17.20→4.17.21 · npm group (2 minor)
⚠ major axios 0.27→1.0 — breaking by design
  used in: src/api/client.ts, src/auth.ts · changelog: <link>
  manual: exercise login + a GET; assert error shape (axios reworked errors)
  test gap: no test hits src/api/client.ts
✗ typescript 5.2→5.3 — CI red: `tsc`, 2 type errors in src/db.ts (excerpt) — needs code changes
⚠ policy FSharp.Core 10.0→10.1 — matches dependabot.yml ignore (/src); library must stay compatible with the old floor — close?
```

## Unstick

After reporting, collect the mechanical unstick actions and offer them as one batch (a second confirmation, separate from the merge batch):

- Post `@dependabot rebase` (via `gh pr comment`) on each PR flagged needs-rebase, and on red-CI PRs whose logs have expired or whose last run used a stale toolchain — a fresh run beats chasing a gone log.
- Close every PR the survey marks `superseded-by` (`gh pr close <n> --comment ...`), in favour of the PR it names.

On confirmation fire the batch, report what was posted and closed, and end — never wait or poll for the fresh CI runs; the next `merge-dependabot` run picks up the results.

Don't start fixing broken bumps or writing tests unless I ask — deep verification of a single flagged bump is `/verify-bump`'s job.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
