---
name: instructions-retro
description: Review how standing instructions — CLAUDE.md, AGENTS.md, references, memory — steered this session's plain-prompt work, and turn misleading or missing ones into edits. Use when I say instructions retro, retro this session, did my instructions help or mislead, or want to refine CLAUDE.md from how a run without a skill went.
---

Judge whether the instructions in context helped or misled this session's work, using the transcript as evidence, and propose edits to the files that hold them.

## Scope

The subject is the session's work done outside any skill. Friction that arose inside a skill run belongs to `/skill-retro` — name the skill, say to run that instead, and leave the friction out here. If the session did no work outside a skill, say so and stop.

Before collecting evidence, ask whether this repo's instruction files are in scope: its `CLAUDE.md`, the `AGENTS.md` files it imports, and its docs. Ask every run — some projects want them reviewed, others don't. The global sources are always in scope:

- `~/.claude/CLAUDE.md`
- `~/.claude/references/*.md` that the session actually read
- memory: `MEMORY.md` in the project's memory directory and any memory recalled during the session

Only text that was in context counts. A rule the session never loaded cannot have steered it — don't review files the run never saw.

## Collect evidence

Walk the transcript and record only these four kinds, each tied to the exact rule text and the turn where it showed:

1. **Correction** — I corrected the agent mid-run, and an in-scope rule caused the mistake, failed to prevent it, or should exist to prevent it.
2. **Conflict** — two in-scope sources gave contradictory instructions, and the agent had to pick one or ask.
3. **Followed into harm** — the agent obeyed a rule and the outcome was worse than ignoring it would have been.
4. **Ignored** — a rule clearly applied, the agent did not follow it, and the outcome suffered.

Also note a rule that visibly *prevented* a mistake the transcript shows the agent about to make — it vouches for that rule against a later removal. *(Untested — assumes such moments are recognisable in a transcript; drop this if runs never find one.)*

Anything else — a rule that might have influenced a choice, generic good practice the run happened to follow — is speculation. Leave it out, or mark it speculative and propose no edit.

## Check the sighting log

`~/.claude/references/instructions-history.md` holds one line per past finding. For each finding, grep it for the mechanism — the way the instruction failed, not this run's symptom — and for the source file:

```sh
grep -i '<file or keyword>' ~/.claude/references/instructions-history.md
```

A match with the same mechanism makes this a second sighting.

## Propose

Present each finding as a concrete edit: quote the current text, show the replacement, name the file. Prefer, in this order:

1. Delete the rule.
2. Narrow or tighten it.
3. Move it to where it applies — a global rule that only one repo needs goes to that repo's `AGENTS.md` or a skill; personal preferences never go into a project repo.
4. Add a rule — last resort.

An edit that **adds** text waits for a second sighting: on a first sighting, propose no edit, list it after the numbered findings as deferred, and log it. Apply on first sighting anyway when I corrected it mid-run, when the edit only removes text, or when the text is certainly wrong (it contradicts another source, points at something that no longer exists, or steers a correct follower to a worse answer).

Rank findings by how much friction each caused. A second sighting goes first. Write the replacement as an instruction to its reader; the justification goes in the commit message. A run with no findings proposes nothing — say so.

## Apply

Ask which edits to apply in plain prose and take a free-form pick ("all", "1 and 3", "2 but reword X").

Append one line per finding to the sighting log — applied, declined, or deferred — plus one `clean` line for a run with no findings, so the log also shows how often the skill runs:

```
- 2026-10-23 · <repo> · <source file> · <fixed|declined|deferred|helped|clean> · <mechanism in a few words>
```

`<repo>` is the basename of `git rev-parse --show-toplevel` in the session's working directory. The log is append-only: fix an uncommitted line in place, and leave committed lines as they are.

Then make the edits and commit each repo separately:

- `~/.claude` — the log plus edits to `CLAUDE.md` and `references/`. Markdown-only, so there are no checks: review the diff and commit with `git -C ~/.claude`. Record declined findings and the reason in the commit message.
- Memory files are untracked — edit them and don't commit.
- A project repo — its own commit, under its own checks and commit conventions. Never push.

After editing `~/.claude/CLAUDE.md`, run `wc -w ~/.claude/CLAUDE.md`. Past ~500 words, suggest `/claude-md-compact` as a separate pass.

---

This skill is not yet battle-tested. At the end of the run, say briefly whether any instruction above was ambiguous, wrong, or needed a workaround. If none was and I corrected nothing during the run, log it instead: from the session repo's working directory run `~/.claude/bin/skill-refiner.sh <this skill's name> log run`, then commit the `HISTORY.md` line it names, alone, in the repo that holds the skill. Where that script does not exist, skip the logging.
