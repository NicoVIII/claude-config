---
name: skill-retro
description: Review a skill run from this session against its SKILL.md and turn observed friction into skill edits. Use right after a skill executed — when I say skill retro, review that skill run, or want to improve or refine a skill based on how the run went.
---

Improve the skill that ran in this session, using the transcript as evidence.

## Scope

Review the most recent skill run in this session, or the skill named as argument. A completed skill-retro run is itself a valid subject: when every other skill run in the session is already retro'd, reviewing the retro is the expected reading, not an error. The transcript is the evidence base — if the named skill did not run in this session, say so and stop; do not review a skill from its text alone. An argument that isn't a skill name at all resolves to the nearest skill that did run (`skill-retryo` → `skill-retro`); say which you took it as and continue. Stopping is for a real skill this session never ran, not for a typo.

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

Present each finding as a concrete edit to the SKILL.md — quote current text, show the replacement — ranked by how much friction it caused. Before writing a patch, check the friction's history in `RUNS.md` and the git log of the skill's directory: when the same mechanism has already been patched for friction before, propose removing or simplifying the mechanism instead of rewording it again — repeated friction from one feature is evidence against the feature, not its phrasing. When what keeps failing is command mechanics — exact flags, field names, error strings — the rung above simplifying is to stop writing it as prose at all: propose moving the step into a helper script beside the SKILL.md (AGENTS.md carries the rule for when that fits) and cutting the prose to an invocation. Prose that exists only to make a model reproduce a fixed pipeline reloads into context every run and is free to regress; a script is not. Propose it even when it outsizes the run's observed friction — the smaller patch is wording the eventual script deletes. Mark anything not backed by the transcript as speculative; observed friction outranks ideas. Keep the numbered list to items carrying an actual edit — a speculative note whose recommendation is "leave this alone" goes in a sentence after the list, never as a numbered finding, or "apply all of them" acquires an item with nothing to apply. If the run was clean, say so and propose nothing — do not manufacture findings.

Write edits for the skill's suggested model in the README maturity table (`~/.claude/README.md`), not for the model doing the retro — the retro may run on a stronger model (switch via `/model`; session context survives), so do not compress instructions the target model would need spelled out.

Also assess maturity — from the run log, not this run alone. Read the skill's `RUNS.md`, next to its SKILL.md. The log is the only evidence: a rating the entries don't support is over-rated, whatever the README currently claims, and a missing file means no logged runs to support anything. When a rewrite replaces the machinery an entry was logged against — a step moved into a script, a mechanism deleted — that entry stops being evidence about the current skill. Don't delete it; say in the retro that the ladder restarts, and hold the rating until runs against the new machinery accumulate. A skill is not more mature for having had its problems rewritten away.

Also report accretion: run `~/.claude/skills/skill-compact/ratio <skill>` and pass on its line. Don't act on it — removing text in the same pass that fixes friction is how the friction always wins, which is why `/skill-compact` is separate.

Judge against these rough bars:

- **🚧 WIP → 🧪 Experimental** — any one run completed end-to-end. WIP means untested, or every attempt so far was canceled; one logged completed run clears it, friction or not.
- **🧪 Experimental → 🟢 Usable** — ~3 entries that are clean or `minor:`. The bar is that the skill reliably completes, even if its text is still being polished.
- **🟢 Usable → 🛡️ Battle-tested** — ~5 strictly clean entries spanning at least 2–3 different repos. `minor:` entries don't count here, and breadth of conditions counts as much as the number of runs.
- **Demotion to 🧪 Experimental** — a `friction:` entry logged against a 🟢 Usable or 🛡️ Battle-tested skill. Both ratings claim the skill reliably completes; a run that didn't is evidence against the claim, and the restored footer puts the next runs back under observation.

Suggest the change in the README table. Crossing up into 🟢 Usable removes the feedback footer; dropping back below it restores the footer.

## Apply

Ask which edits to apply, then make them, update the README maturity table if it changed, and commit. Record declined findings and the reason in the commit message — otherwise a later retro re-derives the same friction and re-proposes what I already turned down.

Log the reviewed run before committing: `~/.claude/skills/skill-retro/log-run <skill> '<verdict>'`, which creates the log if missing and fills in the date and the repo the session ran in. The verdict is one of: `clean` — no edits needed; `minor: <one clause>` — the run completed correctly without user correction, and the edits are clarifications the run itself didn't stumble over (a guessed format pinned down, a stale reference updated); `friction: <one clause>` — anything the user corrected mid-run, a step that failed or needed a workaround, a wrong outcome, or a check you ran that the skill never called for and without which the output would have been wrong. That last one stays behavioral: the test is whether you departed from the skill's text to save the result, not how bad the result would have been. When unsure, it's friction. This log is the evidence base for maturity promotions, so write the entry even when the run was clean and nothing else changed. The `RUNS.md` entry belongs in the same commit as the skill edits — a retro commit touching only SKILL.md means this step was skipped.

How to ask is your call — no tool is mandated. A plain-prose "which should I apply?" taking a free-form pick ("all", "1 and 3", "2 but reword X") usually fits best: the decision needs the quoted diffs in view, and answers often carry modifications. `AskUserQuestion` remains an option when a simple pick suffices, but mind its limits — options cap at four and multiSelect questions can't show previews, so the option slots must never become the only place the findings live.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
