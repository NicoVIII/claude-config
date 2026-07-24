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
git init -b main
git remote add origin https://github.com/NicoVIII/claude-config.git
git pull origin main
git branch --set-upstream-to=origin/main main
```

`git pull` refuses to overwrite untracked files, so move an existing
`CLAUDE.md`, `README.md`, or `skills/` aside first and merge back what you want
to keep.

## After cloning

- Install what the skills shell out to: [`gh`](https://cli.github.com/),
  authenticated (`prioritize`, `merge-dependabot` and `verify-bump` are built on
  it), `jq` (`prioritize`'s gather script), and `rg` (ripgrep).
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
| [`author-skill`](skills/author-skill/SKILL.md) | Capture a session's workflow as a new skill, or refine an existing one. | Fable | 🧪 Experimental |
| [`grilling`](skills/grilling/SKILL.md) | Stress-test a plan or idea through relentless questioning. | Opus | 🟢 Usable |
| [`merge-dependabot`](skills/merge-dependabot/SKILL.md) | Clear the Dependabot PRs that are actually safe to merge. | Sonnet | 🧪 Experimental |
| [`pick-model`](skills/pick-model/SKILL.md) | Pick the cheapest Claude model that still fits the task. | Sonnet | 🧪 Experimental |
| [`prioritize`](skills/prioritize/SKILL.md) | Decide what to work on next across your GitHub repos. | Sonnet | 🧪 Experimental |
| [`skill-retro`](skills/skill-retro/SKILL.md) | Improve a skill right after running it, from observed friction. | Opus | 🧪 Experimental |
| [`verify-bump`](skills/verify-bump/SKILL.md) | Land a dependency bump that green CI alone doesn't prove safe. | Opus | 🧪 Experimental |

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
  repeatable workflow, run `/author-skill` while the context is fresh — the
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
