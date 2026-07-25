---
name: skill-retro
description: Review a skill run from this session against its SKILL.md and turn observed friction into skill edits. Use right after a skill executed — when I say skill retro, review that skill run, or want to improve or refine a skill based on how the run went.
---

Improve the skill that ran in this session, using the transcript as evidence.

## Scope

Review the most recent skill run in this session — the skill that executed, not a skill it took as an argument — or the skill named as argument. A completed skill-retro run is itself a valid subject: when every other skill run in the session is already retro'd, reviewing the retro is the expected reading, not an error. The transcript is the evidence base — if the named skill did not run in this session, say so and stop; do not review a skill from its text alone. An argument that isn't a skill name at all resolves to the nearest skill that did run (`skill-retryo` → `skill-retro`); say which you took it as and continue. Stopping is for a real skill this session never ran, not for a typo.

One retro covers one skill: never fold another skill's friction into this one. A defect the reviewed run *created* in shared machinery is the exception — fix it here and say why, rather than filing it against a skill whose own next run would have to rediscover it.

## Collect evidence

Walk the run and collect observed friction only:

- Steps skipped, reordered, or worked around — and why
- Places the agent had to guess or ask because an instruction was ambiguous
- Knowledge re-derived during the run (commands, flags, environment quirks) that the skill could state once
- Corrections the user made mid-run
- Trigger mismatch: the skill fired when it shouldn't have, or had to be invoked manually when its description should have caught the request
- What the run left behind — when it wrote to a shared script, log, or README table, run the readers of that file and check they still agree. The transcript won't show a counter the run quietly corrupted.
- Anything the run's feedback footer surfaced

## Propose

Present each finding as a concrete edit to the SKILL.md — quote current text, show the replacement — ranked by how much friction it caused. Before writing a patch, check the friction's history in `RUNS.md` and the git log of the skill's directory, then take the furthest rung that fits: reword → remove or simplify the mechanism → move it into a helper script beside the SKILL.md (`references/helpers.md` carries when that fits), cutting the prose to an invocation. Repeated friction from one mechanism is evidence against the mechanism, not its phrasing; and prose that exists only to make a model reproduce a fixed pipeline reloads into context every run and is free to regress, where a script is not. Take the script rung even when it outsizes the run's observed friction. Mark anything not backed by the transcript as speculative; observed friction outranks ideas. Every numbered finding must carry an edit — a speculative "leave this alone" note goes in a sentence after the list, so "apply all of them" never picks up an item with nothing to apply. An edit you have a case *against* still gets numbered: make the case inside the item. What stays out of the list is anything with nothing to apply, not anything you doubt. If the run was clean, say so and propose nothing — do not manufacture findings.

Write edits for the skill's suggested model in the README maturity table (`~/.claude/README.md`), not for the model doing the retro — the retro may run on a stronger model (switch via `/model`; session context survives), so do not compress instructions the target model would need spelled out.

Also assess maturity: `dotnet run --project ~/.claude/bin/runlog -- maturity <skill>` rates the skill from its `RUNS.md`, which is the only evidence — where its rating and the README's disagree, the log wins. One judgement stays yours: when a rewrite replaces the machinery an entry was logged against — a step moved into a script, a mechanism deleted — that entry stops being evidence about the current skill. Don't delete it; say the ladder restarts and hold the rating below what the count gives, until runs against the new machinery accumulate. A skill is not more mature for having had its problems rewritten away. Suggest the change in the README table: crossing up into 🟢 Usable removes the feedback footer; dropping back below it restores the footer.

Also report accretion: run `dotnet run --project ~/.claude/bin/runlog -- ratio <skill>` and pass on its line, plus where accepting the proposed edits would land it. Don't act on it — removing text in the same pass that fixes friction is how the friction always wins, which is why `/skill-compact` is separate.

## Apply

Ask which edits to apply, then make them, update the README maturity table if it changed, and commit. Record declined findings and the reason in the commit message — otherwise a later retro re-derives the same friction and re-proposes what I already turned down.

Log the reviewed run before committing: `dotnet run --project ~/.claude/bin/runlog -- log <skill> '<verdict>'`, which creates the log if missing and fills in the date and the repo the session ran in. The verdict is one of: `clean` — no edits needed; `minor: <one clause>` — the run completed correctly without user correction, and the edits are clarifications the run itself didn't stumble over (a guessed format pinned down, a stale reference updated); `friction: <one clause>` — anything the user corrected mid-run, a step that failed or needed a workaround, a wrong outcome, or a check you ran that the skill never called for and without which the output would have been wrong. That last one stays behavioral: the test is whether you departed from the skill's text to save the result, not how bad the result would have been. When unsure, it's friction. Write the entry even when the run was clean, in the same commit as the skill edits — a retro commit touching only SKILL.md means this step was skipped.

Ask in plain prose, taking a free-form pick ("all", "1 and 3", "2 but reword X") — the decision needs the quoted diffs in view, and answers often carry modifications. If you use `AskUserQuestion` instead, its option slots must never become the only place the findings live.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
