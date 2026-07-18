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

Also assess maturity: after consistently clean runs, suggest promoting the skill in the README table; promotion to 🛡️ Battle-tested removes the feedback footer.

## Apply

Ask which edits to apply, then make them, update the README maturity table if it changed, and commit.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
