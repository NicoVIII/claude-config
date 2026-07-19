# Claude Code Config

Shared skills and configuration for Claude Code, versioned as `~/.claude`.

## Setup

```sh
git clone <repo-url> ~/.claude
```

If `~/.claude` already exists:

```sh
cd ~/.claude
git init
git remote add origin <repo-url>
git pull origin main
git branch --set-upstream-to=origin/main main
```

## After cloning

- Add your `settings.json` manually — it is gitignored and not tracked.
- Use `settings.local.json` for secrets and machine-specific overrides (also gitignored).

## Contents

- `CLAUDE.md` — global personal preferences, loaded into every Claude Code session; applies automatically after cloning
- `AGENTS.md` — guardrails for working on this repo itself
- `skills/` — slash-command skills for Claude Code, see below

## Skills

| Skill | Summary | Suggested model | Maturity |
| --- | --- | --- | --- |
| [`grilling`](skills/grilling/SKILL.md) | Grill the user relentlessly about a plan, decision, or idea, one question at a time, resolving each decision branch until you reach shared understanding. | Opus | 🟢 Usable |
| [`merge-dependabot`](skills/merge-dependabot/SKILL.md) | Assesses the current repo's open Dependabot PRs, merges the bumps a real test suite verifies, and flags the rest with test or manual-verification guidance. | Sonnet | 🚧 WIP |
| [`prioritize`](skills/prioritize/SKILL.md) | Scans your GitHub repos for open PRs, issues, CI failures, and security alerts, then ranks what to tackle first. | Sonnet | 🧪 Experimental |
| [`skill-retro`](skills/skill-retro/SKILL.md) | Reviews a skill's run in the current session against its SKILL.md and turns observed friction into concrete skill edits. | Opus | 🧪 Experimental |
| [`verify-bump`](skills/verify-bump/SKILL.md) | Verifies a single dependency-bump PR by running the checks its green CI doesn't cover, then merges on confirmation or proposes the fix it needs. | Opus | 🚧 WIP |
| [`write-skill`](skills/write-skill/SKILL.md) | Creates a new global skill from the current session's context, carrying this repo's conventions into project sessions where AGENTS.md doesn't load. | Fable | 🚧 WIP |

"Suggested model" is the model to *run* a skill with. *Writing* or refining a
skill is different: always use the most capable model available (currently
Fable, otherwise Opus) — skill text is written once but steers every future
run, so authoring quality dominates authoring cost.

## Workflows

Some skills are meant to run in sequence:

- **Session triage** — Run `/prioritize` to scan your repos and decide what to
  work on. When it surfaces dependency bumps, `cd` into that repo and run
  `/merge-dependabot` to clear the ones CI actually verifies. For a flagged
  bump you still want to land, follow up with `/verify-bump <n>`.
- **Capturing a workflow as a skill** — When a session in any project reveals a
  repeatable workflow, run `/write-skill` while the context is fresh — the
  transcript holds the commands, quirks, and decisions the skill should encode.
  Later runs feed `/skill-retro` as usual.
- **Refining a skill after use** — After running any skill that isn't yet
  🛡️ Battle-tested, run `/skill-retro` in the same session to turn the friction
  you hit into concrete skill edits (this is what the skills' feedback footer
  feeds).

These are starting points, not fixed pipelines — each skill also stands alone.

Maturity: 🚧 WIP → 🧪 Experimental → 🟢 Usable → 🛡️ Battle-tested

Skills below 🟢 Usable end with a feedback footer that asks the agent to surface friction during runs; `/skill-retro` turns that feedback into skill edits.
