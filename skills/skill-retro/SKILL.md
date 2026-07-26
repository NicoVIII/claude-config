---
name: skill-retro
description: Review a skill run from this session against its SKILL.md and turn observed friction into skill edits. Use right after a skill executed — when I say skill retro, review that skill run, or want to improve or refine a skill based on how the run went.
---

Improve the skill that ran in this session, using the transcript as evidence.

## Scope

Review the most recent skill run in this session — the skill that executed, not a skill it took as an argument — or the skill named as argument. A completed skill-retro run is itself a valid subject: when every other skill run in the session is already retro'd, reviewing the retro is the expected reading, not an error. The transcript is the evidence base — if the named skill did not run in this session, say so and stop; do not review a skill from its text alone. An argument that isn't a skill name at all resolves to the nearest skill that did run (`skill-retryo` → `skill-retro`); say which you took it as and continue. Stopping is for a real skill this session never ran, not for a typo.

One retro covers one skill's friction — don't go hunting for another skill's. Where an edit lands is a separate question: put each one where the rule it changes actually lives, shared file or not.

## Collect evidence

Walk the run and collect observed friction only:

- Steps skipped, reordered, or worked around — and why
- Places the agent had to guess or ask because an instruction was ambiguous
- Knowledge re-derived during the run (commands, flags, environment quirks) that the skill could state once
- Corrections the user made mid-run
- Trigger mismatch: the skill fired when it shouldn't have, or had to be invoked manually when its description should have caught the request
- What the run left behind — when it wrote to a shared script, log, or README table, run that file's readers before making any edits and check they still agree. The transcript won't show a counter the run quietly corrupted.
- Anything the run's feedback footer surfaced

## Propose

Present each finding as a concrete edit to the SKILL.md — quote current text, show the replacement — ranked by how much friction it caused, except that a mechanism whose `RUNS.md` history shows it failing again goes first — repeat offenders outrank a one-off that hurt more this run. Before writing a patch, check the friction's history in `RUNS.md` and the git log of the skill's directory, then take the option that leaves the least prose behind — not the smallest edit that works. In rough order: fix the artifact the instruction exists to work around, so the instruction goes away instead of improving; delete the mechanism; move a fixed pipeline into a helper script beside the SKILL.md (`references/helpers.md` carries when that fits), cutting the prose to an invocation; state the exact command inline when it reads in one line; reword, which is the last resort and not the default. Name the stronger options you ruled out, and why, inside the finding — ruled out in a commit message only means the next retro re-derives it. Repeated friction from one mechanism is evidence against the mechanism, not its phrasing; and prose that exists only to make a model reproduce a fixed pipeline reloads into context every run and is free to regress, where a script is not. Take the script rung even when it outsizes the run's observed friction. Mark anything not backed by the transcript as speculative; observed friction outranks ideas. Every numbered finding must carry an edit — a speculative "leave this alone" note goes in a sentence after the list, so "apply all of them" never picks up an item with nothing to apply. An edit you have a case *against* still gets numbered: make the case inside the item. What stays out of the list is anything with nothing to apply, not anything you doubt. If the run was clean, say so and propose nothing — do not manufacture findings. Weigh what a finding costs in prose against the friction it prevents before numbering it: if its absence changed no outcome this run, and the rule is generic good practice rather than something this repo or this skill specifically gets wrong, it isn't a finding — imagined risk is not evidence. A run that resolved an unstated case correctly is not clean: state it once as an edit and log the run `minor:`. Clean means nothing was left to pin down, not that nothing went wrong.

Write edits for the skill's suggested model in the README maturity table (`~/.claude/README.md`), not for the model doing the retro — the retro may run on a stronger model (switch via `/model`; session context survives), so do not compress instructions the target model would need spelled out.

Also assess maturity: `dotnet run --project ~/.claude/bin/runlog -- maturity <skill>` rates the skill from its `RUNS.md`, which is the only evidence — where its rating and the README's disagree, the log wins. One judgement stays yours: a clean-or-minor entry logged against machinery a later retro replaced doesn't count toward promotion. A skill is not more mature for having had its problems rewritten away. Suggest the change in the README table: crossing up into 🟢 Usable removes the feedback footer; dropping back below it restores it verbatim from `~/.claude/references/skill-footer.md`.

## Apply

Ask which edits to apply, then make them, update the README maturity table if it changed, and commit. Record declined findings and the reason in the commit message — otherwise a later retro re-derives the same friction and re-proposes what I already turned down.

Log the reviewed run before committing: `dotnet run --project ~/.claude/bin/runlog -- log <skill> '<verdict>'`, which creates the log if missing and fills in the date and the repo the session ran in. The verdict is one of: `clean` — no edits needed; `minor: <one line>` — the run completed correctly without user correction, and the edits are clarifications the run itself didn't stumble over (a guessed format pinned down, a stale reference updated); `friction: <one line>` — anything the user corrected mid-run, a step that failed or needed a workaround, a wrong outcome, or a check you ran that the skill never called for and without which the output would have been wrong. That last one stays behavioral: the test is whether you departed from the skill's text to save the result, not how bad the result would have been. When unsure, it's friction. Write the entry even when the run was clean, in the same commit as the skill edits — a retro commit touching only SKILL.md means this step was skipped.

After applying, report accretion: run `dotnet run --project ~/.claude/bin/runlog -- ratio <skill>` and pass on its lines — it reports growth since the last compaction, and, once a skill has been compacted more than once, how far that reference point has itself drifted up. Don't project where the edits would land it beforehand — the estimate decides nothing, since acting on the ratio is `/skill-compact`'s separate pass either way, and the one run that tried it was wrong in the direction that flattered its own edits. Don't act on it here: removing text in the same pass that fixes friction is how the friction always wins.

Ask in plain prose, taking a free-form pick ("all", "1 and 3", "2 but reword X") — the decision needs the quoted diffs in view, and answers often carry modifications. If you use `AskUserQuestion` instead, its option slots must never become the only place the findings live.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
