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

| Skill | Suggested model | Maturity |
| --- | --- | --- |
| [`grill-me`](skills/grill-me/SKILL.md) | Opus | 🟢 Usable |
| [`prioritize`](skills/prioritize/SKILL.md) | Sonnet | 🧪 Experimental |
| [`skill-retro`](skills/skill-retro/SKILL.md) | Opus | 🧪 Experimental |

Maturity: 🛡️ Battle-tested → 🟢 Usable → 🧪 Experimental → 🚧 WIP

Skills below 🛡️ end with a feedback footer that asks the agent to surface friction during runs; `/skill-retro` turns that feedback into skill edits.
