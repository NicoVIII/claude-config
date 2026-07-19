# Claude Code Config

Shared skills and configuration for Claude Code, versioned as `~/.claude`.
Primarily versioning my own setup, but meant to be usable by others —
fork it and make it yours.

## Setup

```sh
git clone https://github.com/NicoVIII/claude-config.git ~/.claude
```

If `~/.claude` already exists:

```sh
cd ~/.claude
git init
git remote add origin https://github.com/NicoVIII/claude-config.git
git pull origin main
git branch --set-upstream-to=origin/main main
```

## After cloning

- Add your `settings.json` manually — it is gitignored and not tracked.
- Use `settings.local.json` for secrets and machine-specific overrides (also gitignored).
- If you are not me: `CLAUDE.md` holds *my* personal preferences and loads
  into every Claude Code session — review it and replace what isn't yours.

## Contents

- `CLAUDE.md` — global personal preferences, loaded into every Claude Code session; applies automatically after cloning
- `AGENTS.md` — guardrails for working on this repo itself
- `skills/` — slash-command skills for Claude Code, see below

## Skills

| Skill | Summary | Suggested model | Maturity |
| --- | --- | --- | --- |
| [`grilling`](skills/grilling/SKILL.md) | Grill the user relentlessly about a plan, decision, or idea, one question at a time, resolving each decision branch until you reach shared understanding. | Opus | 🟢 Usable |
| [`merge-dependabot`](skills/merge-dependabot/SKILL.md) | Assesses the current repo's open Dependabot PRs, merges the bumps a real test suite verifies, and flags the rest with test or manual-verification guidance. | Sonnet | 🧪 Experimental |
| [`prioritize`](skills/prioritize/SKILL.md) | Scans your GitHub repos for open PRs, issues, CI failures, and security alerts, then ranks what to tackle first. | Sonnet | 🧪 Experimental |
| [`skill-retro`](skills/skill-retro/SKILL.md) | Reviews a skill's run in the current session against its SKILL.md and turns observed friction into concrete skill edits. | Opus | 🧪 Experimental |
| [`verify-bump`](skills/verify-bump/SKILL.md) | Verifies a single dependency-bump PR by running the checks its green CI doesn't cover, then merges on confirmation or proposes the fix it needs. | Opus | 🧪 Experimental |
| [`write-skill`](skills/write-skill/SKILL.md) | Creates a new global skill from the current session's context, carrying this repo's conventions into project sessions where AGENTS.md doesn't load. | Fable | 🚧 WIP |

"Suggested model" is the model to *run* a skill with. Writing or refining a
skill is different: switch to the most capable model available (currently
Fable, otherwise Opus) before editing — conventions and rationale in
[AGENTS.md](AGENTS.md).

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

Maturity: 🚧 WIP → 🧪 Experimental → 🟢 Usable → 🛡️ Battle-tested — judged
from each skill's run log by `/skill-retro`; promotion bars and log format
live in [its SKILL.md](skills/skill-retro/SKILL.md).
