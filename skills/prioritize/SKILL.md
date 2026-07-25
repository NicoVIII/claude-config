---
name: prioritize
description: Prioritize work across my GitHub repositories — scan open PRs, issues, CI and security alerts, rank what to tackle first, discuss trade-offs. Use when I ask what to work on next, want my backlog triaged, or mention prioritizing my repos.
---

Help me decide what to work on next across my GitHub repositories.

## Gather

Run `dotnet run --project ~/.claude/skills/prioritize/gather` — an F# program. ~10s, plus a few seconds the first time it builds.

It prints one row per non-archived, non-fork repo I own — open PR count, open issue count, default-branch CI, alert severities, last push — then an ATTENTION block holding only what needs a judgement call: PRs failing CI, human-authored PRs, issues opened by someone else (with who spoke last), and PRs awaiting my review in repos I don't own. The counts are complete; the ATTENTION block is the shortlist's raw material.

If it fails, fix the cause — don't fall back to hand-rolled `gh` queries, since the point is that every run covers the same ground.

Drill down only where the ranking actually turns on it:

- **Several PRs in one repo failing the same check** — sample one with `~/.claude/skills/prioritize/failing-log.sh <repo> <pr>` to tell a real blocker (e.g. a config migration) from flakiness; the answer shapes the WHY line.
- **An ATTENTION row whose title doesn't say enough to place it** — `gh issue view <n> -R <repo>` or `gh pr view <n> -R <repo>`.

## Rank

Two weights, in order:

1. **People waiting on me** — PRs awaiting my review, issues from others where the last word is theirs (`UNANSWERED` or `THEIRS-LAST`). An issue sitting on my own reply is not waiting on me. Age amplifies urgency.
2. **Repo health** — failing default-branch CI, open security alerts, PRs going stale.

Personal momentum and quick wins are tiebreakers, not drivers.

## Present

A prioritized shortlist (top 5–8), each with a one-line WHY: who is waiting or what is broken, and for how long. Then at most one line per remaining cluster of repos. No padding.

Bot-authored PRs (Dependabot etc.) are never weight 1 — cluster them ("8 green Dependabot bumps") unless one is failing CI or carries a security fix.

Claim only what the digest's columns say, and only about what it covers: dormant means the PUSHED column, not a skim of the backlog, and "nothing is waiting on you" is a claim about the repos I own plus review requests elsewhere, not about every repo I touch.

When two candidates genuinely compete for the top and the trade-off is mine to make (e.g. review debt vs. a broken build), ask via AskUserQuestion instead of assuming a ranking.

Stop at the ranking. Do not offer to start fixing anything and do not begin work in any repo — I take the shortlist and open the target repository myself.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
