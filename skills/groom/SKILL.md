---
name: groom
description: Groom one repository's issue backlog — close issues that no longer apply, merge duplicates, fix descriptions that drifted from the code. Use when I ask to groom or clean up a repo's backlog, go through its stale issues, or check whether old issues still apply.
---

Clean up one repository's open issue backlog so what remains is real, distinct, and legible.

Scope is the current repo, and hygiene only. `prioritize` is the cross-repo read-only scan that says which repo needs attention; this one writes, in a repo you have already `cd`'d into. **Do not rank or mark priority** — no `next`/`p1` labels, no milestones, no ordering. That is an open question I have not settled, not an oversight. If a run makes the case for it, say so at the end instead of doing it.

## Survey

```sh
gh issue list --state open --limit 200 --json number,title,author,createdAt,updatedAt,comments,labels
```

Add `body` to that same call when the repo has ≤40 open issues; above that, pull bodies per cluster later with `gh issue view <n> --json body`, since bodies run ~1 KB each. `comments` is the full array — its length is the count, and the last entry's author is who spoke last.

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
  #44 replace_collection atomicity — exec_all_atomically applied in a1b2c3d
  #52 settings sort never read — wired up in src/settings.rs:88
close · duplicate (1)
  #57 No mobile layout → #73 Responsive pass — same work; #57's tap-target minimum folds in
close · obsolete (1)
  #40 wine installer — the Windows installer path was dropped in 2023
body fix (1)
  #21 "Alternative sources" — no acceptance criterion; propose: ...
ask (1)
  #65 getShortcutTarget — lostmsu, their word last 29d ago; needs a reply, not a close
unreviewed (18)
```

On approval:

- `done` → `gh issue close <n> --reason completed --comment "<one line why> — written by an agent"`
- `duplicate` → `gh issue close <n> --duplicate-of <survivor>`, which sets both the reason and the relation. Add `--comment` only where the survivor needs the absorbed scope spelled out, and edit the survivor's body only to add that scope.
- `obsolete` → `gh issue close <n> --reason "not planned" --comment "… — written by an agent"`
- `vague` → `gh issue comment <n>` with the missing acceptance criterion, or `gh issue edit <n> --title` when only the title misleads. **Never** replace a body wholesale: the original text is the record of what I was thinking.
- A plainly wrong label → `gh issue edit <n> --add-label`/`--remove-label`. Don't invent a labelling scheme.

Everything written to GitHub as prose ends with `— written by an agent`. Closes, labels, and title edits carry their own authorship and need no marker.

For `ask` issues, draft the reply and let me approve the wording. Never post one inside a batch.

Untested: no run has executed the writes above. The survey command, its fields, and both profiles come from a real scan; the verdict taxonomy, the ~15 cap, and the ≤40 body threshold are designed. Say which of them did not fit.

Stop once the approved batches are applied. Do not start fixing an issue you just kept, and do not open new issues for work you noticed on the way — tell me instead.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
