---
name: groom
description: Groom one repository's issue backlog — close issues that no longer apply, merge duplicates, fix descriptions that drifted from the code. Use when I ask to groom or clean up a repo's backlog, go through its stale issues, or check whether old issues still apply.
---

Clean up one repository's open issue backlog so what remains is real, distinct, and legible.

Scope is the current repo, and hygiene only. `prioritize` is the cross-repo read-only scan that says which repo needs attention; this one writes, in a repo you have already `cd`'d into. **Do not rank or mark priority** — no `next`/`p1` labels, no ordering, and no milestones: scoping one is `/scope-milestone`, which expects a groomed backlog and sends you back here when it doesn't find one. Ranking is left open by the standard, not overlooked; if a run makes the case for it, say so at the end instead of doing it.

The conventions this pass holds issues to are a standard: read `docs/standards/github.md` in this repo where it exists, else `~/.claude/standards/github.md`, before judging anything. The repo's own issue section in `AGENTS.md` / `CLAUDE.md` overrides it where they differ.

## Survey

```sh
gh issue list --state open --limit 200 --json number,title,author,createdAt,updatedAt,comments,labels
```

Add `body` to that same call, dumped to a file you Read. Budget 3 KB per issue, not 1 — an agent-written backlog runs 2–8 KB each, so a 40-issue repo is ~120 KB of reading. Never head-truncate bodies to trim that: a `Related:` section sits at the *end* of a body, and a reference that has since closed is the most common drift a groom catches. Past what fits, groom a slice — one label, or one dump's date range — and report the rest as unreviewed. `comments` is the full array — its length is the count, and the last entry's author is who spoke last.

Compute days since `createdAt` (age) and since `updatedAt` (touched). Read the shape of the whole list before judging any single issue; two profiles seen in real backlogs need different work:

- **Recent dump** — most issues created in one short window, few comments, all mine. Nothing is stale by age; the payoff is overlap, which hides behind differently-worded titles ("No mobile layout" and "Responsive pass over client-web" were one job filed twice).
- **Dormant** — ages in years and `updatedAt` years back too. The payoff is closure; expect much of it to be gone.

Mixed backlogs exist. Classify per issue; use the profile only to decide where to spend the expensive checks.

## Classify

First match wins:

1. **`ask`** — anyone but me authored or commented on it. Never close or edit these without asking, whatever the age.
2. **`done`** — the code already does it. Confirm with `rg` for the symbol or feature named, or `git log --oneline --since=<createdAt> -- <paths it names>`. No confirmation, no verdict: fall through to `keep`.
3. **`duplicate`** — another open issue covers it. Name the survivor, and say what scope moves into it.
4. **`obsolete`** — the premise is gone: the dependency, platform, or subsystem it targets is no longer here. Same evidence bar as `done`.
5. **`body fix`** — still plausible, but the body breaks the standard's *What an issue says* or *Relations* in a way that misleads or stops whoever starts it: it states something no longer true (dead path, closed blocker, moved count, removed feature), states a dependency only in prose, or nobody could say when it is finished. Those are examples; the standard's sections are the test, so judge against them as they read now. Edit it; never close it.
6. **`trim`** — true and startable, but carries what *What an issue says* leaves out: facts a reader finds at the path the body names, the title or scope said again, research or rejected options in the body. Typical of issues filed before the standard's lean-body rules. One line naming what would go; groom does not rewrite it.
7. **`keep`** — real, distinct, legible. No action.

A seven-year-old issue for a feature the repo still lacks is `keep`, not `obsolete`.

The type label is a second axis, checked on every issue whatever its verdict, `keep` included: missing or wrong by the standard, propose the one it should carry. Where two types fit, name the runner-up and why it lost. Label and `trim` lines are one-line checks and don't count toward the cap below. *(Untested on a large unlabelled backlog: whether one group of 40 label lines is still reviewable.)*

Cap a run at ~15 verdicts other than `keep` and `trim`, then report the rest as unreviewed and stop. A half-checked backlog believed to be fully checked is worse than an untouched one.

## Propose & apply

Present verdicts grouped by action, densely, and ask **once per group** — never per issue:

```
close · done (2)
  #101 export is not atomic — wrapped in a transaction in a1b2c3d
  #102 default sort never read — wired up in src/settings.rs:88
close · duplicate (1)
  #103 No mobile layout → #104 Responsive pass — same work; #103's tap-target minimum folds in
close · obsolete (1)
  #105 wine installer — the Windows installer path was dropped in 2023
body fix (1)
  #106 "Alternative sources" — no acceptance criterion; propose: ...
ask (1)
  #107 getShortcutTarget — lostmsu, their word last 29d ago; needs a reply, not a close
trim (1)
  #108 "Retry on timeout" — Current state restates what src/http.rs shows; the rejected backoff options belong in a comment
label (2)
  #104 enhancement — none set; a new capability
  #106 foundation — none set; dev tooling, arguably enhancement but nothing user-facing
unreviewed (18)
```

On approval:

- `done` → `gh issue close <n> --reason completed --comment $'<one line why>\n\n🤖 Written by code assistant'`
- `duplicate` → `gh issue close <n> --duplicate-of <survivor>`, which sets both the reason and the relation. Where the survivor lacks scope the duplicate had, fold that scope into the survivor's body with `gh issue edit <survivor> --body-file` — not `--comment` on the close, which lands on the closed issue.
- `obsolete` → `gh issue close <n> --reason "not planned" --comment $'…\n\n🤖 Written by code assistant'`
- `body fix` → make the issue read as currently true, the way the standard says. A drifted **fact** (dead path, closed blocker, moved count, renamed symbol) goes into the body with `gh issue edit <n> --body-file`, plus `--title` when the title carries it. A **judgement or a question for me** (which of two overlaps survives, whether this is still wanted) is a `gh issue comment <n>`. Rewrite only the prose you are correcting — a correction left as a comment under a body that still reads false is the worst of both.
- A dependency stated only in prose ("blocked by #n", "needs #n first") → record it as the relation, `gh issue edit <n> --add-blocked-by <m>`, and cut the sentence. Whether it is a dependency is the standard's test; a "related" mention stays prose.
- `trim` → once the other approved groups are applied, run `/file-issue` refine on each approved trim, one issue at a time: writing a body to the standard is that skill's job, and its questions stay per issue. *(Untested — refine is written for issues that are not startable yet; whether it trims a startable one without reopening its scope is unconfirmed.)*
- `label` → `gh issue edit <n> --add-label <type>`, plus `--remove-label` for a wrong one. A type label the repo lacks is created first with `gh label create`, described by the standard's wording.

Everything written to GitHub as prose ends with `🤖 Written by code assistant` on its own line. Closes, labels, and title and body edits carry their own authorship and need no marker.

For `ask` issues, draft the reply and let me approve the wording. Never post one inside a batch.

Not yet exercised by a run: the ~15 cap, `obsolete` closes, `ask` drafts, and removing a wrong label. Say which of them did not fit.

Stop once the approved batches are applied. Do not start fixing an issue you just kept, and do not open new issues for work you noticed on the way — tell me instead.
