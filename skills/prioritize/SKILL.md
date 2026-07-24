---
name: prioritize
description: Prioritize work across my GitHub repositories — scan open PRs, issues, CI and security alerts, rank what to tackle first, discuss trade-offs. Use when I ask what to work on next, want my backlog triaged, or mention prioritizing my repos.
---

Help me decide what to work on next across my GitHub repositories.

## Gather

List my non-archived, non-fork repos: `gh repo list --no-archived --source --limit 200 --json name,owner,pushedAt`. Then collect the four signals below. Batch them with a shell `for` loop over the repo list — one tool call per signal, not one per repo. The PR and issue queries may skip repos that return nothing; **the CI and alert queries must cover every repo**, since a repo with no open PRs or issues can still have a red default branch or an unpatched advisory. If you narrow those two to the repos that had open work, you cannot make any claim about overall repo health in the summary.

- PRs: `gh pr list -R <repo> --json number,title,author,isDraft,reviewDecision,statusCheckRollup,updatedAt --jq '.[] | {number, title, author: .author.login, isBot: .author.is_bot, isDraft, reviewDecision, updatedAt, checks: ([.statusCheckRollup[].conclusion] | group_by(.) | map({(.[0] // "PENDING"): length}) | add)}'` — the `--jq` collapses each PR's check array into a pass/fail histogram; do not dump the raw rollup (it is mostly URLs and timestamps and blows the output limit). `checks: null` means no workflow ran for that PR at all (nothing matches its paths) — report it as unverified-by-CI, not failing, and don't spend calls investigating.
- Issues: `gh issue list -R <repo> --json number,title,author,labels,reactionGroups,updatedAt --jq '.[] | {number, title, author: .author.login, labels: [.labels[].name], reactions: ([.reactionGroups[]?.users.totalCount] | add), updatedAt}'` (the reactions field is `reactionGroups` — `reactions` is rejected). A repo with issues disabled errors with "repository has disabled issues"; treat that as no open issues, not a failure — don't retry. The issue list carries no comment data, so it cannot tell you whether an issue was answered. Once the lists are in, pick out the issues whose author is not me — usually a handful — and check only those: `gh issue view <n> -R <repo> --json comments --jq '[.comments[].author.login]'`. An empty list means genuinely unanswered; anything else means it is waiting on the reporter, not on me. Never infer this from `updatedAt`.
- Default-branch CI: `gh run list -R <repo> --branch <default-branch> --limit 1 --json conclusion,displayTitle` — resolve the default branch first (`gh repo view <repo> --json defaultBranchRef`); a bare `--limit 1` returns the newest run on any branch and misreads health when a feature branch is active.
- Security alerts: `gh api repos/<repo>/dependabot/alerts?state=open --jq '[.[] | {severity: .security_advisory.severity, package: .dependency.package.name, summary: .security_advisory.summary, created_at}]'` — severity drives ranking (one critical outweighs many lows), so pull it now rather than a bare count. Where this 403s with "Dependabot alerts are disabled for this repository", that is a per-repo setting, full stop. `gh` appends a boilerplate hint about refreshing `admin:repo_hook` scope to any 403 — it is noise, and your own output disproves it: the same token returned alerts for other repos in the same batch. Treat the repo as alerts-disabled, move on, and do not raise it as a caveat or suggest `gh auth refresh` in the summary.

My own repos aren't the whole picture: `gh repo list` enumerates only repos I own, so a review waiting on me in someone else's or an org's repo never enters the loop above. Catch those separately — `gh search prs --review-requested=@me --state=open --limit 100 --json number,title,repository,author,isDraft,updatedAt,url`; `--limit` defaults to 30 and truncates silently, and `repository` comes back as an object (use `.nameWithOwner`). These are weight 1 by definition — deduplicate against the per-repo results, since a review request in one of my own repos shows up in both.

When several PRs in one repo fail the **same** check, sample one failing run's log to tell a real blocker (e.g. a config migration) from flakiness — the answer shapes the WHY line. The PR list carries no run id — get it from `gh pr checks <number> -R <repo>`, whose failing row links `.../runs/<run-id>/job/<job-id>`. Then `gh run view -R <repo> <run-id> --log-failed | grep -iE 'error|failed' | head -30`; the raw log ends in pages of git cleanup, so tailing it shows the teardown instead of the failure.

## Rank

Two weights, in order:

1. **People waiting on me** — external PRs awaiting my review, issues from others without a maintainer response. Age amplifies urgency.
2. **Repo health** — failing default-branch CI, open security alerts, PRs going stale.

Personal momentum and quick wins are tiebreakers, not drivers.

## Present

A prioritized shortlist (top 5–8), each with a one-line WHY: who is waiting or what is broken, and for how long. Then at most one line per remaining cluster of repos. No padding.

Bot-authored PRs (Dependabot etc.) are never weight 1 — cluster them ("8 green Dependabot bumps") unless one is failing CI or carries a security fix. A cluster line must still account for every repo's open PRs; never let a repo with open work be described as dormant. Call a repo dormant on its `pushedAt`, not on a skim of its backlog.

When two candidates genuinely compete for the top and the trade-off is mine to make (e.g. review debt vs. a broken build), ask via AskUserQuestion instead of assuming a ranking.

Stop at the ranking. Do not offer to start fixing anything and do not begin work in any repo — I take the shortlist and open the target repository myself.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
