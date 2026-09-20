---
name: scope-milestone
description: Scope a milestone in the current repo — what the next release promises and which open issues it needs. Use when I ask what the next milestone or release should contain, whether the milestone already open is still the right promise, to plan a release, or say scope a milestone.
---

Turn one repository's backlog into a milestone with a stated promise and a
member issue set. Scope is the repo you are `cd`'d into. Read-only until I
approve; then it writes the milestone and its memberships to GitHub.

Not this skill's job: ranking the work inside a milestone (`kickoff`), filing
issues (`file-issue`), closing, deduping or rewording them (`groom`), or
starting any of the work.

## Read the conventions

These vary per repo and override every default below:

- The issues section of `AGENTS.md` / `CLAUDE.md`
  (`rg -n -i -A8 '^## Issues' AGENTS.md CLAUDE.md`) — the milestone policy and,
  where stated, its membership test.
- The scope document the conventions name, else `docs/vision.md`, else the
  README's scope or direction section.
- The house pattern, from every milestone the repo has had:

```sh
gh api "repos/{owner}/{repo}/milestones?state=all" \
  --jq '.[] | "\(.title)\t\(.state)\topen=\(.open_issues) closed=\(.closed_issues)\t\(.description)"'
```

Read from it: how titles are shaped, whether descriptions state a promise or
just name a theme, and how many issues one has historically held. With neither
a stated policy nor a past milestone to read one from, ask me what milestones
mean here — don't invent a versioning scheme.

## Read the state

```sh
gh issue list --state open --limit 200 --json number,title,labels,milestone,createdAt \
  --jq '.[] | "\(.number)\t[\([.labels[].name]|join(","))]\t\(.milestone.title // "-")\t\(.createdAt[0:10])\t\(.title)"'
git log --oneline -30
```

Never add `body` to that call — bodies run ~1 KB each. Pull them with
`gh issue view <n> --json body`, and only for the shortlist you are about to
judge. The promise comes from the scope document's current-status and direction
sections: the backlog says which direction is *ready*, not which one is right.

## Gate: is the backlog legible?

A milestone scoped over duplicates and finished-but-open issues promises work
already done. Sample ~10 candidates first for: work already landed
(`git log --oneline --since=<createdAt> -- <paths it names>`), near-duplicate
titles, and issues with no criterion for being finished. Past
roughly one in five failing, stop and tell me to run `/groom` first.
*(Untested — that ratio is a designed threshold, not an observed one. A run
where it fires too eagerly, or never fires on a backlog that clearly needed
grooming, should say so.)*

## Mode: the repo state chooses, not me

**A milestone is already open** → the live question is whether its promise is
still right, not what comes after it. Test its issues against its stated
promise, and the unmilestoned ones too — a promise that has since acquired
blockers is under-scoped, not just behind. Produce confirm / trim / add
verdicts for it. Scope a successor only once it is coherent, and say plainly
that you are deferring one rather than silently not doing it.

**No open milestone** → scope the next one from the scope document's direction
themes, preferring the theme the backlog already has the most ready work in.

Several open milestones is a question, not a guess: ask which one is current.

## The promise

One sentence saying what the milestone guarantees, phrased so a reader can tell
what breaks without it. A theme name is not a promise, and neither is a list of
features.

**Membership**: the repo's own test where it states one, else — would the
promise be false without this issue? Apply it to every open issue, not only the
ones in the theme. Issues that nearly qualify are reported as explicitly
excluded, so the omission reads as a decision.

**Size**: calibrate against what past milestones held, or against what the repo
actually closed in a comparable window. A promise nobody can finish is not a
scope statement — when the membership test yields far more than that, the
promise is too broad, so narrow the promise rather than dropping issues that
qualify under it. *(Untested — no repo has yet been scoped against its own
closed-milestone history.)*

## Present

One block, then stop:

```
promise · v0.2.0
  <one sentence: what is guaranteed, and what breaks without it>
in scope (5)
  #NN <subject> — <why the promise is false without it>
  …
excluded · near miss (2)
  #NN <subject> — real, but <why the promise survives without it>
gap (1)
  nothing open covers <the part of the promise nothing addresses> → /file-issue
```

Ask once, for the whole block — never issue by issue. Gaps hand off to
`/file-issue`; never file one from here.

## Apply

On approval:

- create — `gh api repos/{owner}/{repo}/milestones -f title=<title> -f description=<promise>`
- re-scope — `gh api --method PATCH repos/{owner}/{repo}/milestones/<number> -f description=<promise>`
- membership — `gh issue edit <n> --milestone <title>`, and
  `gh issue edit <n> --remove-milestone` to drop one

The milestone description carries no agent marker: it is a scope statement read
as project fact, and a marker inside a single sentence is noise. Any issue
comment you post does carry one — `🤖 Written by code assistant` on its own
line.

Stop once the approved block is applied. Don't begin any issue in the milestone
you just scoped.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
