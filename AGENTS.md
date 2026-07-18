# Working in this repo

This is the live `~/.claude` config — edits to skills take effect in running Claude Code sessions immediately.

- `.gitignore` is an allowlist by design: secrets and runtime state live in this directory, so everything is ignored by default. Never loosen the catch-all; tracking a new file means adding an explicit `!/...` entry.
- Do not create `CLAUDE.md` here — `~/.claude/CLAUDE.md` is global user memory loaded into every session in every project, not a repo-scoped file. This file is the repo-scoped one.
- `skills/*/SKILL.md` frontmatter must start at byte 0 (nothing above the first `---`, or it silently fails to parse). The `description` field is the model's only trigger signal — keep it rich in trigger phrases.
- `skills/init-memories/memories/` holds canonical copies installed verbatim into project memory directories — edits here propagate everywhere. They load into context on every invocation; keep them terse.
