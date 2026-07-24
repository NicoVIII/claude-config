---
name: prioritize
description: Prioritize work across my GitHub repositories — scan open PRs, issues, CI and security alerts, rank what to tackle first, discuss trade-offs. Use when I ask what to work on next, want my backlog triaged, or mention prioritizing my repos.
---

Help me decide what to work on next across my GitHub repositories.

## Gather

Run `~/.claude/skills/prioritize/gather.rs` — a single-file Rust program run by `cargo +nightly -Zscript` via its shebang. ~10s, plus a few seconds the first time it builds.

It prints one row per non-archived, non-fork repo I own — open PR count, open issue count, default-branch CI, alert severities, last push — then an ATTENTION block holding only what needs a judgement call: PRs failing CI, human-authored PRs, issues opened by someone else (with whether I ever replied), and PRs awaiting my review in repos I don't own. The counts are complete; the ATTENTION block is the shortlist's raw material, and everything absent from it is cluster-line material by construction.

The script aborts instead of half-reporting, because a short digest and good news look identical here. If it fails, fix the cause — don't fall back to hand-rolled `gh` queries, since the point is that every run covers the same ground.

Drill down only where the ranking actually turns on it:

- **Several PRs in one repo failing the same check** — sample one log to tell a real blocker (e.g. a config migration) from flakiness; the answer shapes the WHY line. The PR list carries no run id, so get it from `gh pr checks <number> -R <repo>`, whose failing row links `.../runs/<run-id>/job/<job-id>`. Then `gh run view -R <repo> <run-id> --log-failed | grep -iE 'error|failed' | head -30` — the raw log ends in pages of git cleanup, so tailing it shows the teardown instead of the failure.
- **An ATTENTION issue whose title doesn't say enough to place it** — `gh issue view <n> -R <repo>`.

## Rank

Two weights, in order:

1. **People waiting on me** — PRs awaiting my review, issues from others I never answered. Age amplifies urgency.
2. **Repo health** — failing default-branch CI, open security alerts, PRs going stale.

Personal momentum and quick wins are tiebreakers, not drivers.

## Present

A prioritized shortlist (top 5–8), each with a one-line WHY: who is waiting or what is broken, and for how long. Then at most one line per remaining cluster of repos. No padding.

Bot-authored PRs (Dependabot etc.) are never weight 1 — cluster them ("8 green Dependabot bumps") unless one is failing CI or carries a security fix. A cluster line must still account for every repo's open PRs; never let a repo the table shows open work for be described as dormant. Call a repo dormant on its PUSHED column, not on a skim of its backlog.

Claim only what the digest covers. It reports the repos I own plus review requests elsewhere — so "nothing is waiting on you" is a claim about those, not about every repo I touch.

When two candidates genuinely compete for the top and the trade-off is mine to make (e.g. review debt vs. a broken build), ask via AskUserQuestion instead of assuming a ranking.

Stop at the ranking. Do not offer to start fixing anything and do not begin work in any repo — I take the shortlist and open the target repository myself.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
