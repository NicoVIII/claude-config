---
name: author-skill
description: Create a new skill in ~/.claude/skills from the current session's context, or edit an existing one against my skill standards, from inside any project. Use when I say write, create, or make a skill, edit, update, or extend a skill, turn this workflow into a skill, or want to capture what we just did as a skill. Not for reviewing a skill right after it ran — that is skill-retro.
---

Create a new global skill in `~/.claude/skills/<name>/SKILL.md`, capturing the workflow while its context is live in this session — or edit an existing skill on request, holding it to the same standards. For edits, skip the creation-only steps (directory, maturity row starts at WIP) and update the README maturity-table row only if the skill's one-line summary no longer fits; post-run friction review stays with `skill-retro`.

## Before writing

- **Read the conventions at their source** — they only auto-load in sessions inside `~/.claude`, and this skill usually runs elsewhere: `~/.claude/AGENTS.md` (skill guardrails) and both the Skills and the Workflows section of `~/.claude/README.md` — sibling sections, not one, holding the maturity table and the documented skill sequences. Follow what they say now; don't rely on a remembered copy.
- **Model check.** AGENTS.md names the model to author with — the only statement of that threshold, so don't restate it here. If you are on a weaker one, say so and suggest switching via `/model` before continuing; session context survives. If I decline, proceed and record the exception in the commit message — otherwise the next retro cannot tell a deliberate call from an oversight.
- **Confirm it's global.** Default home is `~/.claude/skills` — personal, cross-project. If the workflow only makes sense in the current repo, say so and ask whether it belongs in the project's `.claude/skills` instead; a project skill follows that repo's conventions, not the `~/.claude` ones.

## Mine the session

The reason to write the skill now, here, is that the knowledge is in this transcript. Collect from it:

- Commands actually run, with the flags, output fields, and quirks discovered — exact error messages and workarounds included
- Decisions made and why — these become the skill's rules
- Corrections and clarifications from the user — these become explicit instructions
- Where the workflow started and stopped — these become scope and stop conditions

Not every rule comes from the transcript. When a skill is requested outright ("add a skill for X"), or when — as is common — some rules are mined and others designed to fill gaps, extract the derived ones before drafting: ask scope questions, or run the `grilling` skill when the design has real decision branches to resolve. Mark each derived rule as untested **in the SKILL.md itself**, not only in the draft presentation, which does not survive the session: a rule with no evidence behind it is what the next `/skill-retro` most needs to find.

Encode this observed knowledge, not generic advice. A skill earns its tokens by stating what the executing agent would otherwise re-derive or get wrong.

## Write

- The frontmatter `description` is the only trigger signal: pack it with phrases I would actually say, ending with a first-person "Use when …" clause.
- Write the body for the suggested execution model, which may be weaker than you: spell out commands, orderings, and edge cases rather than compressing.
- Size the draft against what exists (`wc -w ~/.claude/skills/*/SKILL.md`) — the counterweight to the bullet above. Landing above the current longest is a signal to cut before committing, not after: until a `/skill-compact` records a baseline, `runlog ratio` measures growth from the first commit, so an overweight first draft raises its own trigger permanently instead of ever reporting as accretion.
- State scope and stop conditions explicitly — what the skill does *not* do, and when to stop and report instead of continuing.
- Attribution: if the skill writes anywhere others read on my behalf — GitHub comments, reviews, issues, wholesale prose — its instructions must require ending that output with a short agent marker (e.g. "— written by an agent"). Media that already carry authorship (commits via `Co-Authored-By`, PR footers, merges) need no marker; code never gets one. This norm's only home is here — deliberately not in global memory — so every skill that writes externally must restate it (as `verify-bump` and `merge-dependabot` do).
- Suggest an execution model: Sonnet for mechanical, procedural runs; Opus for judgment-heavy ones. Other tiers need explicit justification — a skill worth writing is rarely a Haiku task, and Fable as a routine run model defeats the cost point of the column.

## Land it

Present the draft and incorporate feedback before persisting anything — write it to a scratchpad file and show that file's contents rather than pasting the text into the reply, so the bytes reviewed are the bytes installed and a long skill is never typed twice. Then, in the `~/.claude` repo — a separate git repository from the current project, so use `git -C ~/.claude`:

- Write the SKILL.md with the feedback footer, copying the exact wording from an existing 🧪 Experimental skill — the footer is dropped at 🟢 Usable, so a Usable skill has none to copy from despite the footer's own "not yet battle-tested" phrasing. Don't create a `RUNS.md`; `runlog log` writes it at the first `/skill-retro`, and `maturity`/`ratio` both read a missing one correctly.
- Add the README maturity-table row, starting at 🚧 WIP; if the skill pairs with existing ones, extend the Workflows section.
- Commit in `~/.claude` — the message explains why the skill exists, not what it contains.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
