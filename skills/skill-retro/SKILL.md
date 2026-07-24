---
name: skill-retro
description: Review a skill run from this session against its SKILL.md and turn observed friction into skill edits. Use right after a skill executed — when I say skill retro, review that skill run, or want to improve or refine a skill based on how the run went.
---

Improve the skill that ran in this session, using the transcript as evidence.

## Scope

Review the most recent skill run in this session, or the skill named as argument. A completed skill-retro run is itself a valid subject: when every other skill run in the session is already retro'd, reviewing the retro is the expected reading, not an error. The transcript is the evidence base — if the named skill did not run in this session, say so and stop; do not review a skill from its text alone.

One retro covers one skill: never fold another skill's friction into this one, and don't hunt for other retro candidates — other skills that ran this session get their own retros when I invoke this skill on them; whether that already happened is not this retro's business.

## Collect evidence

Walk the run and collect observed friction only:

- Steps skipped, reordered, or worked around — and why
- Places the agent had to guess or ask because an instruction was ambiguous
- Knowledge re-derived during the run (commands, flags, environment quirks) that the skill could state once
- Corrections the user made mid-run
- Trigger mismatch: the skill fired when it shouldn't have, or had to be invoked manually when its description should have caught the request
- Anything the run's feedback footer surfaced

## Propose

Present each finding as a concrete edit to the SKILL.md — quote current text, show the replacement — ranked by how much friction it caused. Before writing a patch, check the friction's history in `RUNS.md` and the git log of the skill's directory: when the same mechanism has already been patched for friction before, propose removing or simplifying the mechanism instead of rewording it again — repeated friction from one feature is evidence against the feature, not its phrasing. Mark anything not backed by the transcript as speculative; observed friction outranks ideas. If the run was clean, say so and propose nothing — do not manufacture findings.

Write edits for the skill's suggested model in the README maturity table (`~/.claude/README.md`), not for the model doing the retro — the retro may run on a stronger model (switch via `/model`; session context survives), so do not compress instructions the target model would need spelled out.

Also assess maturity — from the run log, not this run alone. Read the skill's `RUNS.md`, next to its SKILL.md. A missing file means no logged runs yet; if the skill is nonetheless rated above 🚧 WIP, that rating is grandfathered (an import, or use predating the log) — start the log, and don't read the empty file as evidence for a demotion.

Judge against these rough bars:

- **🚧 WIP → 🧪 Experimental** — any one run completed end-to-end. WIP means untested, or every attempt so far was canceled; one logged completed run clears it, friction or not.
- **🧪 Experimental → 🟢 Usable** — ~3 entries that are clean or `minor:`. The bar is that the skill reliably completes, even if its text is still being polished.
- **🟢 Usable → 🛡️ Battle-tested** — ~5 strictly clean entries spanning at least 2–3 different repos. `minor:` entries don't count here, and breadth of conditions counts as much as the number of runs.
- **Demotion to 🧪 Experimental** — a `friction:` entry logged against a 🟢 Usable or 🛡️ Battle-tested skill. Both ratings claim the skill reliably completes; a run that didn't is evidence against the claim, and the restored footer puts the next runs back under observation.

Suggest the change in the README table. Crossing up into 🟢 Usable removes the feedback footer; dropping back below it restores the footer.

## Apply

Ask which edits to apply, then make them, update the README maturity table if it changed, and commit.

Log the reviewed run before committing: append one line to the skill's `RUNS.md` (create it with a `# Run log` heading if missing) — `YYYY-MM-DD · <repo the skill ran in> · <verdict>` (the repo of the session's working directory — for a cross-repo skill, still where the session ran, not the repos it touched). The verdict is one of: `clean` — no edits needed; `minor: <one clause>` — the run completed correctly without user correction, and the edits are clarifications the run itself didn't stumble over (a guessed format pinned down, a stale reference updated); `friction: <one clause>` — anything the user corrected mid-run, a step that failed or needed a workaround, or a wrong outcome. When unsure, it's friction. This log is the evidence base for maturity promotions, so write the entry even when the run was clean and nothing else changed. The `RUNS.md` entry belongs in the same commit as the skill edits — a retro commit touching only SKILL.md means this step was skipped.

How to ask is your call — no tool is mandated. A plain-prose "which should I apply?" taking a free-form pick ("all", "1 and 3", "2 but reword X") usually fits best: the decision needs the quoted diffs in view, and answers often carry modifications. `AskUserQuestion` remains an option when a simple pick suffices, but mind its limits — options cap at four and multiSelect questions can't show previews, so the option slots must never become the only place the findings live.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
