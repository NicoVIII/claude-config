---
name: prioritize
description: Prioritize work across my GitHub repositories — scan open PRs, issues, CI and security alerts, rank what to tackle first, discuss trade-offs. Use when I ask what to work on next, want my backlog triaged, or mention prioritizing my repos.
---

Help me decide what to work on next across my GitHub repositories.

## Gather

List my non-archived, non-fork repos: `gh repo list --no-archived --source --limit 200 --json name,owner,pushedAt`. For each, collect open work (batch independent calls; skip repos with nothing open):

- PRs: `gh pr list -R <repo> --json number,title,author,isDraft,reviewDecision,statusCheckRollup,updatedAt --jq '.[] | {number, title, author: .author.login, isBot: .author.is_bot, isDraft, reviewDecision, updatedAt, checks: ([.statusCheckRollup[].conclusion] | group_by(.) | map({(.[0] // "PENDING"): length}) | add)}'` — the `--jq` collapses each PR's check array into a pass/fail histogram; do not dump the raw rollup (it is mostly URLs and timestamps and blows the output limit).
- Issues: `gh issue list -R <repo> --json number,title,author,labels,reactionGroups,updatedAt` (the reactions field is `reactionGroups` — `reactions` is rejected). A repo with issues disabled errors with "repository has disabled issues"; treat that as no open issues, not a failure — don't retry.
- Default-branch CI: `gh run list -R <repo> --branch <default-branch> --limit 1 --json conclusion,displayTitle` — resolve the default branch first (`gh repo view <repo> --json defaultBranchRef`); a bare `--limit 1` returns the newest run on any branch and misreads health when a feature branch is active.
- Security alerts: `gh api repos/<repo>/dependabot/alerts?state=open --jq '[.[] | {severity: .security_advisory.severity, package: .dependency.package.name, summary: .security_advisory.summary, created_at}]'` — severity drives ranking (one critical outweighs many lows), so pull it now rather than a bare count. Skip silently where this 403s (alerts disabled).

When several PRs in one repo fail the **same** check, sample one failing run's log (`gh run view -R <repo> <run-id> --log-failed`) to tell a real blocker (e.g. a config migration) from flakiness — the answer shapes the WHY line.

## Rank

Two weights, in order:

1. **People waiting on me** — external PRs awaiting my review, issues from others without a maintainer response. Age amplifies urgency.
2. **Repo health** — failing default-branch CI, open security alerts, PRs going stale.

Personal momentum and quick wins are tiebreakers, not drivers.

## Present

A prioritized shortlist (top 5–8), each with a one-line WHY: who is waiting or what is broken, and for how long. Then at most one line per remaining cluster of repos. No padding.

Bot-authored PRs (Dependabot etc.) are never weight 1 — cluster them ("8 green Dependabot bumps") unless one is failing CI or carries a security fix.

When two candidates genuinely compete for the top and the trade-off is mine to make (e.g. review debt vs. a broken build), ask via AskUserQuestion instead of assuming a ranking.

Stop at the ranking. Do not offer to start fixing anything and do not begin work in any repo — I take the shortlist and open the target repository myself.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
