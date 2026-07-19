# Working in this repo

This is the live `~/.claude` config — edits to skills take effect in running Claude Code sessions immediately.

- No ADRs. Decision rationale lives in commit messages; a decision that must constrain future work (especially "we tried/considered X — don't") gets a one-line guardrail here or in the relevant SKILL.md, citing its commit for the full story.
- `.gitignore` is an allowlist by design: secrets and runtime state live in this directory, so everything is ignored by default. Never loosen the catch-all; tracking a new file means adding an explicit `!/...` entry.
- `CLAUDE.md` here is global user memory, loaded into every session in every project — only universal personal preferences belong in it, and every word has token cost everywhere. Repo-scoped guardrails go in this file instead.
- Write and refine skills with the most capable model available (currently Fable, otherwise Opus), even when the skill's suggested execution model is Sonnet: skill text is written once but steers every future run, and the hard part is anticipating how a smaller executor will misread it. Mechanical edits (typos, renames) don't need this.
- `skills/*/SKILL.md` frontmatter must start at byte 0 (nothing above the first `---`, or it silently fails to parse). The `description` field is the model's only trigger signal — keep it rich in trigger phrases.
- Skills below 🟢 Usable in the README maturity table (🚧 WIP, 🧪 Experimental) end with a one-line feedback footer asking the executing agent to surface friction. Add it (identical wording, copy from an existing skill) to every new skill; remove it when a skill graduates to Usable — mature skills are daily drivers where the footer is noise, and `/skill-retro` on demand covers them. `/skill-retro` turns the surfaced friction into skill edits.
