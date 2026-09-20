---
name: kickoff
description: Pick what to start with in the current repository — one concrete recommendation, not a ranked list. Use when I ask what to work on in this repo, what to pick up here, where to start after /prioritize pointed me at a repo, or say kickoff. Cross-repo triage is prioritize, not this.
---

Answer "I'm in this repo now — what do I start with?" with one concrete
recommendation. Read-only: nothing here writes to GitHub or the repo.

Scope is the repo you are `cd`'d into. Work the tiers in order and stop at the
first that yields a candidate — read a lower tier only when everything above
came up empty, or to fill the runners-up when the winning tier has fewer than
three candidates. The convention being read: issues are the backlog; an open
milestone, where present, names the current focus. Both are optional signals —
a repo using neither still gets an answer from the tiers that apply.

## Tier 1 — blocked or bleeding

Objective problems that stall everything else; no judgment calls here.

```sh
default=$(gh repo view --json defaultBranchRef -q .defaultBranchRef.name)
gh run list --branch "$default" --limit 10 --json workflowName,conclusion,createdAt,url
gh pr list --state open --json number,title,author,isDraft,reviewDecision,statusCheckRollup,updatedAt \
  --jq '.[] | {number,title,author:.author.login,isDraft,reviewDecision,updatedAt,checks:([.statusCheckRollup[].conclusion]|unique)}'
gh api "repos/{owner}/{repo}/dependabot/alerts?state=open" --jq length
```

- **Red CI** — judge the latest *completed* run per workflow (an empty
  `conclusion` means still running — skip it); an old red superseded by a
  green is history, not a blocker.
- **Security alerts** — a 403/404 from the alerts endpoint means no access or
  not enabled: say so in the report and move on, never treat it as zero.
- **PRs waiting on you** — review requested from you, or your own PR with
  changes requested, red checks, or green-and-unmerged. Bot dependency bumps
  are not PRs waiting on you: one or two open bumps are no candidate at any
  tier, whatever their checks say. Only 3 or more open become one tier-1
  candidate — "run `/merge-dependabot`" — so the pile is cleared before it
  reaches Dependabot's open-PR limit and updates stop arriving.

Severity order when several hit: red default-branch CI, then security alerts,
then stalled PRs. Recommend the worst and stop.

## Tier 2 — keep the lights on

```sh
gh issue list --state open --limit 200 --json number,title,labels,milestone,createdAt \
  --jq '.[] | "\(.number)\t[\([.labels[].name]|join(","))]\t\(.milestone.title // "-")\t\(.milestone.dueOn // "")\t\(.createdAt[0:10])\t\(.title)"'
```

Never add `body` to that call. Fetch bodies only for suspected defects via
`gh issue view <n> --json body`; for cross-references (tiers 3 and 4) run
`gh issue list --state open --limit 200 --json number,body --jq '.[] | "\(.number): " + ([.body | scan("#[0-9]+")] | unique | join(" "))'`.

A defect is an issue describing broken shipped behavior. Where the repo's label
set has a `bug` label (`gh label list --json name -q '.[].name'`), that label is
the maintainer's own verdict: an issue without it is not a defect here, however
its text reads. Only where the repo has no `bug` label is it inferred from the
text (crash, error, wrong result, "worked before"). The gate for this tier: **a
real user hits it in normal use**. Cosmetic glitches, papercuts with a
workaround, and error paths the user only reaches once something else has
failed drop to tier 4, wrong state left behind or not — and when genuinely
unsure, so does the issue: the milestone wins ties. Within the tier, rank by
blast radius, then age.

## Tier 3 — declared focus

From the milestones on the issues above — that column is the whole set that
matters: a milestone with no open issue holds no candidate. Nearest `dueOn`
wins; one open milestone needs no due date; several with none is a question —
ask me which is current instead of guessing. Then read the winning milestone's
own description once — `gh api "repos/{owner}/{repo}/milestones?state=open" --jq
'.[] | "\(.title)\n\(.description)"'` — it states what the release promises,
and the report names which part of that promise the recommendation serves.

Rank by how much each issue unblocks: cross-references pointing at it from any
open issue, in the milestone or not, minus any whose work has already landed —
a milestone issue whose commits are in `git log` is done but unclosed, so say
so. Break ties on blast radius, then age, as in tier 2; read the contenders'
bodies, not just their titles.

## Tier 4 — ranked backlog

Everything left, including the defects that failed the tier-2 gate. Rank:
unblocks other issues, then defect over idea, then older first. Recommend the
top.

## Tier 5 — nothing to do

Say so plainly. If the backlog is sizeable but produced no candidate you would
defend, that is a legibility problem, not a ranking problem — suggest `/groom`
and stop. Never invent work to have a recommendation.

## Report

One line first: **start with X because Y (tier N)**. Then at most two
runners-up, one line each, so I can veto without a re-run. Then stop: do not
begin the work, do not open or edit issues, do not post anywhere — wait for my
pick.
