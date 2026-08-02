---
name: verify-bump
description: Verify and land a single dependency-bump PR in the current repo — check out the branch, run the checks its green CI doesn't cover (codegen diffs, tool integration, extension-point behavior, use-site exercise), then merge with my confirmation or propose the fix it needs. Any bump author: Dependabot, Renovate, or human. Use when I ask to verify a bump or dependency-update PR, check whether a version bump is safe to land, or follow up on a PR merge-dependabot flagged as major or unverified.
---

Verify one dependency-bump PR by exercising what its green CI doesn't prove, then land it with my confirmation. The goal is landing the bump — a verification that ends in "flagged, again" has failed at its job; when something blocks the merge, propose the concrete fix and offer to execute it.

Scope: a single PR in the current repo whose diff is "a dependency changed version" — any author. `merge-dependabot` sweeps a repo's bot PRs in bulk and flags the risky ones; this skill is the deep follow-up on one PR. If a merge-dependabot report from earlier in this session covers the PR, reuse its assessment (use sites, changelog links, test-gap notes) instead of re-deriving it — but always re-fetch PR state first: a rebase since the sweep can change the bump itself (even major→patch), invalidating the report's risk assessment.

## Gather

Skip the *static* findings a same-session report established (use sites, workflow reading, changelog links) — never skip the PR-state query.

- PR state: `gh pr view <n> --json title,headRefName,baseRefName,mergeStateStatus,statusCheckRollup,isDraft` — leave `body` out; a Dependabot body runs ~15k characters of embedded changelog, 83× the rest of the query, and you need at most two lines of it. `mergeStateStatus` is computed lazily and often reads `UNKNOWN` on the first query — re-query `gh pr view <n> --json mergeStateStatus,mergeable` before acting on it.
- Every bumped package: for grouped PRs pull just those lines — `gh pr view <n> --json body -q .body | grep -E '^(Updates|Bumps) '` — not the whole body, and not just the title. Use the same targeted grep for bot notes that change the plan: `grep -i rebase` surfaces "automatic rebases have been disabled on this pull request".
- Situations to surface before verifying (they change the plan, not necessarily abort it):
  - **CI red** → read the failure first — `gh run view <run-id> --log-failed | grep -nEi 'error|failed|exit code'`; unfiltered it returns the entire job, 304 lines of runner boilerplate around the 4 that matter. The verification plan then includes fixing it, not just observing it.
  - **`mergeStateStatus` DIRTY/CONFLICTING** → integrate the base branch yourself in the worktree (see Verify) rather than waiting on `@dependabot rebase`: a bot rebase only re-resolves the bump it owns, so it cannot fix a conflict that needs a co-bump or a config change, and Dependabot stops auto-rebasing a PR left open over 30 days. Stop and hand back only when the conflict is in hand-written source you'd be guessing at.
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

For baseline comparisons, add a second worktree at the tip of the PR's base branch (usually `origin/main`), and in the PR worktree rebase onto the base branch first (`git rebase origin/<base>`). Comparing base-tip vs base-tip+PR isolates the bump exactly as well as the merge-base does, and it tests the tree that will actually land — a stale merge-base can predate repo fixes it needs to even build (tool config, lockfile policy). Rebase rather than merge: it keeps the bump author's commit intact and leaves the branch pushable as the PR's own history. Conflicts in a generated lockfile are not a reason to stop — restore the base copy (`git checkout origin/<base> -- <lockfile>`) and let the package manager regenerate it. Run the planned checks and capture actual output, not impressions. Capture exit codes from the command itself, never after a pipe — `cmd | tail; echo $?` reports the pipe tail's status, and a pipe-status array isn't portable (`PIPESTATUS` is bash-only, and zsh's `pipestatus` indexes from 1 — so a copied `[0]` expands to empty and reads as a pass). Check output and status both by redirecting first: `cmd > <scratchpad>/step.log 2>&1; echo $?`, then grep or tail the file. Clean up after the verdict (and after any merge):

```sh
git worktree remove --force <path> && git branch -D verify-bump-<n>
```

## Judge & land

Present a verdict with evidence: what ran, what it showed, and every judgment call stated rather than smoothed over ("the codegen diff has 3 changes; they look benign because …"). Then:

- **Clean pass** → ask once to merge. On confirmation, match how this repo actually lands bump PRs — `git log --merges --oneline <base> | head` shows whether they arrive as merge commits or squashes, which `gh repo view --json squashMergeAllowed,mergeCommitAllowed,rebaseMergeAllowed` cannot tell you (it reports what is allowed, not what is used). Fall back to squash → merge → rebase only when history is silent: `gh pr merge <n> --squash --delete-branch`.
- **Failure or needed code changes** → report what failed and which use sites and changelog items are implicated, then propose the concrete fix (API migration, config change, lockfile regen) and offer to implement it. On my confirmation, implement in the worktree, run the checks again, and push to the PR branch. The local branch name is not the PR's, so spell the refspec out: `git push --force-with-lease=<headRefName>:<original-head-sha> origin verify-bump-<n>:<headRefName>` (`headRefName` from the Gather query) — the rebase rewrote history, and the lease pinned to the SHA you fetched refuses the push if anything landed on the branch meanwhile. Dependabot stops rebasing a branch once someone else has pushed to it, which is what you want here. If the push is rejected — branch protection, or a PR from a fork you can't write to — say so and propose a superseding branch instead. Never implement or push without that confirmation.

Anything written to GitHub (comments) ends with "— written by an agent"; merges and pushed commits carry their own authorship and need no marker.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
