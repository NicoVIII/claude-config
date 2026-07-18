---
name: prioritize
description: Prioritize work across my GitHub repositories — scan open PRs, issues, CI and security alerts, rank what to tackle first, discuss trade-offs. Use when I ask what to work on next, want my backlog triaged, or mention prioritizing my repos.
---

Help me decide what to work on next across my GitHub repositories.

## Gather

List my non-archived, non-fork repos: `gh repo list --no-archived --source --limit 200 --json name,owner,pushedAt`. For each, collect open work (batch independent calls; skip repos with nothing open):

- PRs: `gh pr list -R <repo> --json number,title,author,isDraft,reviewDecision,statusCheckRollup,updatedAt`
- Issues: `gh issue list -R <repo> --json number,title,author,labels,reactions,updatedAt`
- Default-branch CI: latest run conclusion via `gh run list -R <repo> --limit 1`
- Security alerts: `gh api repos/<repo>/dependabot/alerts?state=open --jq length` — skip silently where this 403s (alerts disabled)

## Rank

Two weights, in order:

1. **People waiting on me** — external PRs awaiting my review, issues from others without a maintainer response. Age amplifies urgency.
2. **Repo health** — failing default-branch CI, open security alerts, PRs going stale.

Personal momentum and quick wins are tiebreakers, not drivers.

## Present

A prioritized shortlist (top 5–8), each with a one-line WHY: who is waiting or what is broken, and for how long. Then at most one line per remaining cluster of repos. No padding.

When two candidates genuinely compete for the top and the trade-off is mine to make (e.g. review debt vs. a broken build), ask via AskUserQuestion instead of assuming a ranking.
