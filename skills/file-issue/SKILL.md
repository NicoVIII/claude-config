---
name: file-issue
description: File a GitHub issue in the current repo, or refine a rough one until someone could start it cold. Use when I say file an issue, open an issue for this, track this, write this up as an issue, or ask to refine, flesh out, or clarify an existing issue.
---

Two modes, both scoped to the current repo: **file** a new issue, or **refine** an existing one that isn't ready to start. Not for backlog cleanup across many issues (`groom`) or picking what to work on (`kickoff`).

Untested: the sweep-issue search, and a Refine run that ends in a rewritten body.

## Read the repo's conventions first

These vary per repo and override the defaults below:

- The issue section of `AGENTS.md` / `CLAUDE.md` at the repo root (`rg -n -i -A8 '^## Issues' AGENTS.md CLAUDE.md`): label taxonomy, milestone/Project policy, the scope document.
- Labels that exist: `gh label list --json name -q '.[].name'`. Never create a label. With no stated taxonomy, apply an existing label only where one plainly fits, else none.
- The scope document — the one the conventions name, else `docs/vision.md`, else the README's scope or non-goals section. No scope doc: skip the scope check and say so.

## File

1. **Triage before filing.** Stop and report instead of filing when:
   - it is a trivial fix — make the fix instead, or say it is one;
   - the scope doc lists it as a non-goal — name the line; file only if I still want it;
   - it is a small nit — add it to the repo's open sweep issue (`gh issue list --search "sweep in:title"`) as a comment, or bundle it with other nits into one new sweep issue.
2. **Check for an existing issue.** `gh issue list --state open --search "<2-3 key terms>"`, then once more with synonyms: one job often hides behind two wordings ("No mobile layout" vs "Responsive pass"). On a match, comment the new scope onto that issue instead of filing.
3. **Write the body so someone could start it cold**, without this session:
   - bug: repro steps, expected, actual;
   - feature or enabler: goal, current state with file paths, scope, and an acceptance section saying when it is done;
   - decisions you could not make: an explicit "Open questions" list, not buried in prose.
4. **Title** specific enough to triage from the list view alone: name the thing and the gap, not the area ("Settings default sort is saved but never read", not "Settings issue").
5. **Labels** per the repo's taxonomy.
6. When I asked for the issue, create it directly with `gh issue create --title … --body-file … --label …`, then show me the link, labels, and body. Show a draft first only when it carries open questions I haven't seen yet — give each one your recommended answer, then fold my answers into the body and file it without showing the draft again.

Only file when I asked. Issues you notice while doing other work get listed at the end of the run as proposals.

Record a dependency as GitHub's relation, not as body prose: `--blocked-by <n>` / `--blocking <n>` on `gh issue create`, or `gh issue edit <n> --add-blocked-by <m>` / `--add-blocking <m>` on an existing issue.

## Refine

For an issue that is plausible but not startable — a one-paragraph idea, no acceptance, decisions nobody made.

1. `gh issue view <n> --json title,body,labels,comments,author`. If someone other than me authored or commented, say so before editing or posting anything.
2. **Scope check** against the scope doc and the repo's decision records (e.g. `docs/decisions/`): quote any non-goal, "someday, maybe", or settled decision it touches. If the scope doc doesn't plainly cover it, ask before any interview whether to close it as not planned, park it, or change the scope doc. **Park**: do step 3, skip steps 4–5, post one comment with the facts found and the decisions left open, close the issue as not planned, and commit a line linking it into the scope doc's "someday" section.
3. **Look up the facts** — what exists in the code, what the dependency or platform supports. Don't ask me what the repo can answer.
4. **List the open decisions**, then interview me on them following the `grilling` skill's rules: one question at a time, each with your recommended answer, dependencies resolved in order.
5. **Rewrite the body** to File's step 3 bar — the decisions made folded in, the acceptance section, anything still open — so the issue reads cleanly when work starts instead of as a body plus a comment trail. Show it to me, then `gh issue edit <n> --body-file …`; GitHub's edit history keeps the original. Fix the title in the same call with `--title` only if it misleads.

Stop once the issue is updated. Don't start implementing.

## Attribution

Every issue body and comment ends with `🤖 Written by code assistant` on its own line. Labels and title edits need no marker.
