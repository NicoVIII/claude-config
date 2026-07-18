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

- `skills/` — slash-command skills for Claude Code
