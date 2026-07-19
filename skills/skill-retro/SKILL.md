---
name: skill-retro
description: Review a skill run from this session against its SKILL.md and turn observed friction into skill edits. Use right after a skill executed — when I say skill retro, review that skill run, or want to improve or refine a skill based on how the run went.
---

Improve the skill that ran in this session, using the transcript as evidence.

## Scope

Review the most recent skill run in this session, or the skill named as argument. The transcript is the evidence base — if the named skill did not run in this session, say so and stop; do not review a skill from its text alone.

## Collect evidence

Walk the run and collect observed friction only:

- Steps skipped, reordered, or worked around — and why
- Places the agent had to guess or ask because an instruction was ambiguous
- Knowledge re-derived during the run (commands, flags, environment quirks) that the skill could state once
- Corrections the user made mid-run
- Trigger mismatch: the skill fired when it shouldn't have, or had to be invoked manually when its description should have caught the request
- Anything the run's feedback footer surfaced

## Propose

Present each finding as a concrete edit to the SKILL.md — quote current text, show the replacement — ranked by how much friction it caused. Mark anything not backed by the transcript as speculative; observed friction outranks ideas. If the run was clean, say so and propose nothing — do not manufacture findings.

Write edits for the skill's suggested model in the README maturity table (`~/.claude/README.md`), not for the model doing the retro — the retro may run on a stronger model (switch via `/model`; session context survives), so do not compress instructions the target model would need spelled out.

Also assess maturity — from the run log, not this run alone. Read the skill's `RUNS.md` (next to its SKILL.md; missing means no logged runs yet) and judge against the rough bars: 🧪 Experimental → 🟢 Usable after ~3 clean entries; 🟢 Usable → 🛡️ Battle-tested after ~5 clean entries spanning at least 2–3 different repos — breadth of conditions counts as much as the number of runs. Suggest promotions in the README table; promotion to 🟢 Usable removes the feedback footer.

## Apply

Ask which edits to apply, then make them, update the README maturity table if it changed, and commit.

Log the reviewed run before committing: append one line to the skill's `RUNS.md` (create the file if missing) — `YYYY-MM-DD · <repo the skill ran in> · clean` when the run needed no edits, or `YYYY-MM-DD · <repo> · friction: <one clause>` otherwise. This log is the evidence base for maturity promotions, so write the entry even when the run was clean and nothing else changed.

`AskUserQuestion` caps at four options per question. With more than four findings, don't split into extra question rounds — present them all as ranked text and take a free-form pick ("all", "A, C, F"), or bundle related findings into one option each.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
