---
name: groom
description: Groom one repository's issue backlog — close issues that no longer apply, merge duplicates, fix descriptions that drifted from the code. Use when I ask to groom or clean up a repo's backlog, go through its stale issues, or check whether old issues still apply.
---

Clean up one repository's open issue backlog so what remains is real, distinct, and legible.

Scope is the current repo, and hygiene only. `prioritize` is the cross-repo read-only scan that says which repo needs attention; this one writes, in a repo you have already `cd`'d into. **Do not rank or mark priority** — no `next`/`p1` labels, no ordering, and no milestones: scoping one is `/scope-milestone`, which expects a groomed backlog and sends you back here when it doesn't find one. Ranking itself is an open question I have not settled, not an oversight; if a run makes the case for it, say so at the end instead of doing it.

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
5. **`vague`** — still plausible, but nobody could say when it is finished. That is a body fix, not a close.
6. **`keep`** — real, distinct, legible. No action.

Age alone never justifies a close. A seven-year-old issue for a feature the repo still lacks is `keep`, not `obsolete`.

Cap a run at ~15 non-`keep` verdicts, then report the rest as unreviewed and stop. A half-checked backlog believed to be fully checked is worse than an untouched one.

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
unreviewed (18)
```

On approval:

- `done` → `gh issue close <n> --reason completed --comment $'<one line why>\n\n🤖 Written by code assistant'`
- `duplicate` → `gh issue close <n> --duplicate-of <survivor>`, which sets both the reason and the relation. Where the survivor lacks scope the duplicate had, fold that scope into the survivor's body with `gh issue edit <survivor> --body-file` — not `--comment` on the close, which lands on the closed issue.
- `obsolete` → `gh issue close <n> --reason "not planned" --comment $'…\n\n🤖 Written by code assistant'`
- `vague` → make the issue read as currently true. A drifted **fact** (dead path, closed blocker, moved count, renamed symbol) is edited in place and silently — `gh issue edit <n> --body-file`, plus `--title` when the title carries it — with no comment and no dated "updated" note in the body; GitHub's edit history holds the superseded text, which is where a stale draft belongs. Only a **judgement or a question for me** (which of two overlaps survives, whether this is still wanted) is a `gh issue comment <n>`. Rewrite only the prose you are correcting — a correction left as a comment under a body that still reads false is the worst of both.
- A plainly wrong label → `gh issue edit <n> --add-label`/`--remove-label`. Don't invent a labelling scheme.

Everything written to GitHub as prose ends with `🤖 Written by code assistant` on its own line. Closes, labels, and title and body edits carry their own authorship and need no marker.

For `ask` issues, draft the reply and let me approve the wording. Never post one inside a batch.

Not yet exercised by a run: the ~15 cap, `obsolete` closes, `ask` drafts, and label edits. Say which of them did not fit.

Stop once the approved batches are applied. Do not start fixing an issue you just kept, and do not open new issues for work you noticed on the way — tell me instead.
