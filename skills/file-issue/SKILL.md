---
name: file-issue
description: File a GitHub issue in the current repo, or refine a rough one until someone could start it cold. Use when I say file an issue, open an issue for this, track this, write this up as an issue, or ask to refine, flesh out, or clarify an existing issue.
---

Two modes, both scoped to the current repo: **file** a new issue, or **refine** an existing one that isn't ready to start. Not for backlog cleanup across many issues (`groom`) or picking what to work on (`kickoff`).

## Read the conventions first

- **The standard:** `docs/standards/github.md` in this repo where it exists, else `~/.claude/standards/github.md`. It holds what an issue's title and body say, the type labels, how a dependency is recorded, and what a milestone is. The steps below are how to apply it and restate none of it.
- What this repo does differently: the issue section of `AGENTS.md` / `CLAUDE.md` at the repo root (`rg -n -i -A8 '^## Issues' AGENTS.md CLAUDE.md`). It overrides the standard, and names the scope document.
- Labels that exist: `gh label list --json name -q '.[].name'`. Where the repo lacks the type label the standard calls for, don't create it yourself: say which one is missing and ask once, because adding it changes the repo's label set.
- Open milestones and what each promises: `gh api repos/:owner/:repo/milestones --jq '.[] | "\(.number) \(.title): \(.description)"'`.
- The scope document — the one the conventions name, else `docs/vision.md`, else the README's scope or non-goals section. No scope doc: skip the scope check and say so.

## File

1. **Triage before filing**, by the standard's rules on what is not filed. Stop and report instead of filing when:
   - it is a trivial fix — make the fix instead, or say it is one;
   - the scope doc lists it as a non-goal — name the line; file only if I still want it;
   - it is a small nit — add it to the repo's open sweep issue (`gh issue list --search "sweep in:title"`) as a comment, or bundle it with other nits into one new sweep issue.
2. **Check for an existing issue.** `gh issue list --state open --search "<2-3 key terms>"`, then once more with synonyms. On a match — the same work packet under another wording — don't file and don't comment: fold the new scope into that issue's body as Refine step 5 does. An issue that shares the goal but is a different packet — another mechanism, its own acceptance, startable on its own — is not a match: file separately, and add one "Related: #n" line to the existing issue's body.
3. **Write the body to the standard's bar**: someone could start it cold, without this session.
4. **Title** by the standard: "Settings default sort is saved but never read", not "Settings issue".
5. **Label and milestone**: the one type label the standard calls for, and a milestone only where the issue passes the membership test — the repo's own where it states one, else the standard's. Say which way the milestone came out.
6. When I asked for the issue, create it directly with `gh issue create --title … --body-file … --label … [--milestone …]`, then show me the link and labels. Show a draft first only for open questions whose answer changes the shape of the work — what gets built, where it lives, how big it is — giving each your recommended answer, then fold my answers in and file without showing the draft again. A question the implementer can settle while working stays in the body's Open questions list; don't spend a round trip on it.

Only file when I asked. Issues you notice while doing other work get listed at the end of the run as proposals.

A dependency, as the standard defines one, is recorded with `--blocked-by <n>` / `--blocking <n>` on `gh issue create`, or `gh issue edit <n> --add-blocked-by <m>` / `--add-blocking <m>` on an existing issue. Two issues on one subject — one writing a rule, one proving it — are related, not blocked: if you can't name the blocked side, or the direction flips depending on how you read it, the link is prose.

Filing several at once: file in dependency order so each relation names an issue that already exists, and let the relation carry the link — a body reaching for a number that doesn't exist yet is a dependency that belongs in a relation.

## Refine

For an issue that is plausible but not startable — a one-paragraph idea, no acceptance, decisions nobody made.

1. `gh issue view <n> --json title,body,labels,comments,author`. If someone other than me authored or commented, say so before editing or posting anything.
2. **Scope check** against the scope doc and the repo's decision records (e.g. `docs/decisions/`): quote any non-goal, "someday, maybe", or settled decision it touches. If the scope doc doesn't plainly cover it, ask before any interview whether to close it as not planned, park it, or change the scope doc. **Park**: do step 3, skip steps 4–5, post one comment with the facts found and the decisions left open, close the issue as not planned, and commit a line linking it into the scope doc's "someday" section.
3. **Look up the facts** — what exists in the code, what the dependency or platform supports. Don't ask me what the repo can answer.
4. **List the open decisions.** Interview me only on those that pass File step 6's test — the answer changes the shape of the work — and that I have not already settled in this session. Follow the `grilling` skill's rules: one question at a time, each with your recommended answer, dependencies resolved in order. Every other open decision goes into the body's Open questions list with your recommended answer.
5. **Rewrite the body** to File's step 3 bar — the decisions made folded in, the acceptance section, anything still open. Apply it with `gh issue edit <n> --body-file …`, then show me the link. Fix the title in the same call with `--title` only if it misleads.

Stop once the issue is updated. Don't start implementing.

## Attribution

Every issue body and comment ends with `🤖 Written by code assistant` on its own line. Labels and title edits need no marker.
