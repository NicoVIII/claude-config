---
name: author-skill
description: Create a new skill — global in ~/.claude/skills, or in the current repo's .claude/skills — from the current session's context, or edit an existing one against my skill standards, from inside any project. Use when I say write, create, or make a skill, edit, update, or extend a skill, turn this workflow into a skill, or want to capture what we just did as a skill. Not for reviewing a skill right after it ran — that is skill-retro.
---

Create a new skill in `~/.claude/skills/<name>/SKILL.md` — or in the current repo's `.claude/skills/<name>/SKILL.md` when the workflow only makes sense there — capturing it while its context is live in this session; or edit an existing skill on request, holding it to the same standards. For edits, skip the creation-only steps (directory, maturity row starts at WIP) and update the README maturity-table row only if the skill's one-line summary no longer fits; post-run friction review stays with `skill-retro`.

## Before writing

- **Read the conventions at their source** — nothing auto-loads them, wherever this skill runs: `~/.claude/references/skill-conventions.md` (skill guardrails) and both the Skills and the Workflows section of `~/.claude/README.md` — sibling sections, not one, holding the maturity table and the documented skill sequences; `awk '/^## Skills/,/^## Setup/' ~/.claude/README.md` is exactly those two and half the file. For a project skill, its peers are that repo's `.claude/skills/*/SKILL.md` descriptions and the table in its `.claude/skills/README.md`, in place of the Skills section. Follow what they say now; don't rely on a remembered copy.
- **Model check.** `skill-conventions.md` names the model to author with — the only statement of that threshold, so don't restate it here. If you are on a weaker one, say so and suggest switching via `/model` before continuing; session context survives. If I decline, proceed and record the exception in the commit message — otherwise the next retro cannot tell a deliberate call from an oversight.
- **Pick the tree.** A workflow that only makes sense in the current repo goes in that repo's `.claude/skills`, where it ships with the repo; ask when unsure. Everything below holds there too, with the project repo standing in wherever a step names `~/.claude`: its own commit, its own checks, and its maturity table one directory lower, in `.claude/skills/README.md`.
- **Earn it before mining.** Say why the skill should exist, and be willing to answer no — the case is strongest when a capable agent would get this *wrong* by default, or when an existing skill declares the gap and hands off to nothing. If a docs link would do, say so and stop.

## Mine the session

The reason to write the skill now, here, is that the knowledge is in this transcript. Collect from it:

- Commands actually run, with the flags, output fields, and quirks discovered — exact error messages and workarounds included
- Decisions made, and why — the decision becomes a rule, the why becomes commit-message material
- Corrections and clarifications from the user — these become explicit instructions
- Where the workflow started and stopped — these become scope and stop conditions

Not every rule comes from the transcript. When a skill is requested outright ("add a skill for X"), or when — as is common — some rules are mined and others designed to fill gaps, extract the derived ones before drafting: ask scope questions, or run the `grilling` skill when the design has real decision branches to resolve. Mark each derived rule as untested **in the SKILL.md itself**, not only in the draft presentation, which does not survive the session: a rule with no evidence behind it is what the next `/skill-retro` most needs to find.

Encode this observed knowledge, not generic advice. A skill earns its tokens by stating what the executing agent would otherwise re-derive or get wrong.

## Write

- The frontmatter `description`: phrases I would actually say, ending with a first-person "Use when …" clause — and nothing about how the skill works. `skill-conventions.md` carries the rule and the length signal; don't restate them here.
- Write the body for the suggested execution model, which may be weaker than you: spell out commands, orderings, and edge cases rather than compressing.
- Cut every sentence that justifies the skill to me rather than instructing its executor — motivation, war stories, why the gap exists; those go in the commit message. No word count catches this: a draft can sit well under the longest existing skill (`wc -w ~/.claude/skills/*/SKILL.md`) and still be half motivation. Cut before committing, not after — the size you land on is logged as the skill's origin baseline, and until a `/skill-compact` records a new one, `ratio` measures all growth from it, so an overweight first draft raises its own trigger permanently instead of ever reporting as accretion.
- State scope and stop conditions explicitly — what the skill does *not* do, and when to stop and report instead of continuing.
- Attribution: the agent-marker norm in `~/.claude/CLAUDE.md` (Interaction) covers the skill's *executor*, but a skill run elsewhere may not have it loaded — so if the skill writes anywhere others read on my behalf, its instructions must still require the marker explicitly. `verify-bump` and `merge-dependabot` show the shape.
- Suggest an execution model: Sonnet for mechanical, procedural runs; Opus for judgment-heavy ones. Other tiers need explicit justification — a skill worth writing is rarely a Haiku task, and Fable as a routine run model defeats the cost point of the column.

## Land it

Write the SKILL.md straight to its final path, `cat` it for review, and edit in place until I'm happy — never retype it into the reply, so a long skill is never typed twice. Nothing is persisted until the commit, and an unwanted draft is one `rm` away; it is live in new sessions while under review, which is the price of skipping a copy. Then, in the repo that holds the skill — for a global one `~/.claude`, a separate git repository from the current project, so use `git -C ~/.claude`:

- End the SKILL.md with the feedback footer verbatim from `~/.claude/references/skill-footer.md`.
- Open the skill's log: `~/.claude/bin/skill-refiner.sh <skill> log creation`. It records the current size as the origin baseline every later `ratio` is read against; re-run it after each further edit, until the first retro or fix freezes it. Never write `HISTORY.md` by hand. The name resolves to the session repo's `.claude/skills` before `~/.claude/skills` — check the echoed `logged to` path is the tree you meant, and pass the skill's directory path if not.
- Add the maturity-table row, starting at 🚧 WIP and naming the execution model you suggested above — it is the only record of that choice, and the next author or retro writes for whatever model the row names. For a project skill the table is in the `.claude/skills/README.md` the `log creation` above seeded; it arrives empty, so the row is yours to add either way. If the skill pairs with existing ones, extend the Workflows section — in a project, wherever that repo documents its skills.
- Commit there — the message explains why the skill exists, not what it contains.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
