---
name: verify-bump
description: Verify and land a single dependency-bump PR in the current repo — check out the branch, run the checks its green CI doesn't cover (codegen diffs, tool integration, extension-point behavior, use-site exercise), then merge with my confirmation or propose the fix it needs. Any bump author: Dependabot, Renovate, or human. Use when I ask to verify a bump or dependency-update PR, check whether a version bump is safe to land, or follow up on a PR merge-dependabot flagged as major or unverified.
---

Verify one dependency-bump PR by exercising what its green CI doesn't prove, then land it with my confirmation. The goal is landing the bump — a verification that ends in "flagged, again" has failed at its job; when something blocks the merge, propose the concrete fix and offer to execute it.

Scope: a single PR in the current repo whose diff is "a dependency changed version" — any author. `merge-dependabot` sweeps a repo's bot PRs in bulk and flags the risky ones; this skill is the deep follow-up on one PR. If a merge-dependabot report from earlier in this session covers the PR, reuse its assessment (use sites, changelog links, test-gap notes) instead of re-deriving it — but always re-fetch PR state first: a rebase since the sweep can change the bump itself (even major→patch), invalidating the report's risk assessment.

## Gather

Skip the *static* findings a same-session report established (use sites, workflow reading, changelog links) — never skip the PR-state query.

- PR state: `gh pr view <n> --json title,body,headRefName,baseRefName,mergeStateStatus,statusCheckRollup,isDraft`. `mergeStateStatus` is computed lazily and often reads `UNKNOWN` on the first query — re-query `gh pr view <n> --json mergeStateStatus,mergeable` before acting on it.
- Every bumped package: for grouped PRs parse each ``Updates `x` from A to B`` line in the body, not just the title.
- Situations to surface before verifying (they change the plan, not necessarily abort it):
  - **CI red** → read the failure first (`gh run view <run-id> --log-failed`); the verification plan then includes fixing it, not just observing it.
  - **`mergeStateStatus` DIRTY/CONFLICTING** → needs a rebase first (`@dependabot rebase` for bot PRs); fire that and stop — verification against a stale merge state is wasted.
  - **Checks pending** → verification can proceed, but landing must wait for green; say so in the verdict.
- Use sites per package: `rg -i <package>` — imports, config files, build scripts.
- What CI actually verifies: read `.github/workflows/*.{yml,yaml}`. The whole point of the plan below is covering what these workflows don't.
- Breaking changes: open the changelog/release notes linked from the PR body, list the breaking items across the crossed versions, and map each against the use sites.

## Plan the checks

Green CI proves compile + suite pass. Design checks for the gap — match each bump to its risk shape (grouped PRs can hit several; cover at least the worst member's):

- **The bump is a tool that runs other things** (test runner, build orchestration, formatter, CI action): run the orchestration end-to-end locally and confirm outputs and exit codes still integrate with whatever invokes it — a subtly broken runner config can pass trivially while exercising nothing. Cheap counter-check: break one test temporarily and confirm the failure still propagates as a nonzero exit. If the tests the runner executes are compiled/generated output (Fable, tsc, codegen), break the *source* and rebuild instead of editing the output — generated files are often untracked (no `git checkout` restore) and incremental builds may skip regenerating a tampered file; restoring may need a forced clean rebuild (delete the output dir).
- **The bump is a compiler or code generator**: build on the merge-base first and keep the generated output, rebuild on the PR branch, diff the two — a codegen regression that still compiles and passes is exactly what CI misses. Judge the diff: version-stamp noise is fine, behavioral changes need reading.
- **The bump is a test library with custom extension points** (generators/Arb instances, reporters, fixtures): confirm the extensions still compile *and still behave* — property-test generators can silently produce degenerate inputs post-bump while the suite stays green; sample generated values if the API allows.
- **Ordinary library**: exercise the use sites beyond the suite — run the code paths that touch them, walk the changelog's breaking list item-by-item against each use site.

## Verify

Never touch my working tree — verify in a temporary worktree:

```sh
git fetch origin pull/<n>/head:verify-bump-<n>
git worktree add <scratchpad>/verify-bump-<n> verify-bump-<n>
```

For baseline comparisons, add a second worktree at the tip of the PR's base branch (usually `origin/main`), and in the PR worktree merge the base branch first (`git merge origin/<base> --no-edit`). Comparing base-tip vs base-tip+PR isolates the bump exactly as well as the merge-base does, and it tests the tree that will actually land — a stale merge-base can predate repo fixes it needs to even build (tool config, lockfile policy). If that merge conflicts, the PR needed a rebase anyway — fire it and stop, per Gather. Run the planned checks and capture actual output, not impressions. Capture exit codes from the command itself, never after a pipe — `cmd | tail; echo $?` reports the pipe tail's status; use `${PIPESTATUS[0]}` or run the command unpiped. Clean up after the verdict (and after any merge):

```sh
git worktree remove --force <path> && git branch -D verify-bump-<n>
```

## Judge & land

Present a verdict with evidence: what ran, what it showed, and every judgment call stated rather than smoothed over ("the codegen diff has 3 changes; they look benign because …"). Then:

- **Clean pass** → ask once to merge. On confirmation, use the repo's preferred method (`gh repo view --json squashMergeAllowed,mergeCommitAllowed,rebaseMergeAllowed`, prefer squash → merge → rebase): `gh pr merge <n> --squash --delete-branch`.
- **Failure or needed code changes** → report what failed and which use sites and changelog items are implicated, then propose the concrete fix (API migration, config change, lockfile regen) and offer to implement it. On my confirmation, implement in the worktree, run the checks again, and push to the PR branch — Dependabot stops rebasing a branch once someone else has pushed to it, which is what you want here. If branch protection blocks pushing, say so and propose a superseding branch instead. Never implement or push without that confirmation.

Anything written to GitHub (comments) ends with "— written by an agent"; merges and pushed commits carry their own authorship and need no marker.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
