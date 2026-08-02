---
name: skill-retro
description: Review a skill run from this session against its SKILL.md and turn observed friction into skill edits. Use right after a skill executed — when I say skill retro, review that skill run, or want to improve or refine a skill based on how the run went.
---

Improve the skill that ran in this session, using the transcript as evidence.

## Scope

Review the most recent skill run in this session — the skill that executed, not a skill it took as an argument — or the skill named as argument. A completed skill-retro run is itself a valid subject — the expected reading when every other skill run in the session is already retro'd. The transcript is the evidence base — if the named skill did not run in this session, say so and stop; do not review a skill from its text alone. An argument that isn't a skill name at all resolves to the nearest skill that did run (`skill-retryo` → `skill-retro`); say which you took it as and continue — stopping is for a real skill this session never ran.

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

Present each finding as a concrete edit to the SKILL.md — quote current text, show the replacement — ranked by how much friction it caused, except that a mechanism whose `HISTORY.md` shows it failing again goes first — repeat offenders outrank a one-off that hurt more this run. Before writing a patch, check the friction's history in `HISTORY.md` and the git log of the skill's directory, then take the rung of `~/.claude/references/prose-ladder.md` that fits — read it, don't recall it. Name the stronger rungs you ruled out, and why, inside the finding — ruled out in a commit message only means the next retro re-derives it. Repeated friction from one mechanism is evidence against the mechanism, not its phrasing; take the script rung even when it outsizes the run's observed friction. Every numbered finding must carry an edit, so "apply all of them" never picks up an item with nothing to apply — a note with no edit goes in a sentence after the list, and an edit you have a case *against* stays numbered, the case argued inside the item. Mark anything not backed by the transcript as speculative — observed friction outranks ideas — and weigh what a finding costs in prose against the friction it prevents: if its absence changed no outcome this run and the rule is generic good practice rather than something this repo or this skill specifically gets wrong, it isn't a finding. A clean run proposes nothing — say so; imagined risk is not evidence. Of what survives that, an edit that *adds* a rule waits for a second sighting on its first: name the mechanism in the retro's clause and log no fix against it, then patch it when a later run hits the same thing — only additions bloat, and the log costs nothing to write where `SKILL.md` is billed on every run. A clause with no fix beside it is exactly the queue this reads, so name the mechanism, not the symptom: a later retro can only match a recurrence against wording it recognises. Fix on first sighting anyway when I corrected it mid-run, when the text is certainly wrong (self-contradictory, or pointing at something that no longer exists), or when the fix removes text. Clean means nothing was left to pin down, not that nothing went wrong.

Write edits for the skill's suggested model in the README maturity table (`~/.claude/README.md`), not for the model doing the retro — the retro may run on a stronger model (switch via `/model`; session context survives), so do not compress instructions the target model would need spelled out.

## Apply

Ask which edits to apply in plain prose, taking a free-form pick ("all", "1 and 3", "2 but reword X") — the decision needs the quoted diffs in view, and answers often carry modifications; if `AskUserQuestion` is used anyway, its option slots must never become the only place the findings live. Record declined findings and the reason in the commit message — otherwise a later retro re-derives the same friction and re-proposes what I already turned down.

Log the reviewed run as two entries, one before the edits and one after — every command below is `dotnet run --project ~/.claude/bin/skill-refiner -- <skill> log …`, and both entries go in the same commit as the skill edits. A retro commit touching only SKILL.md means this step was skipped.

**Before editing**, grade the run, so its entry records the size the run actually ran against: `log retro clean`, `log retro minor '<one line>'`, or `log retro major '<one line>'`. Grade by the damage done, not by what caused it: `clean` — nothing was left to pin down; `minor` — issues that didn't much hinder a good result (a guessed format, a stale reference, a correction absorbed without derailing, an unstated case you resolved correctly); `major` — big issues, up to a wrong outcome or an aborted run. When unsure, take the worse grade. Write the entry even when the run was clean.

With that entry written, assess maturity: `dotnet run --project ~/.claude/bin/skill-refiner -- <skill> maturity` rates the skill from its `HISTORY.md`, which is the only evidence — where its rating and the README's disagree, the log wins. Update the README table: crossing up into 🟢 Usable removes the feedback footer; dropping back below it restores it verbatim from `~/.claude/references/skill-footer.md`.

**After the edits**, record what they did: `log fix small '<one line>'` or `log fix big '<one line>'`. Big means the runs before this edit no longer vouch for the skill — a mechanism replaced, a step added or removed, a contract changed; small means the procedure is intact and only its wording moved. A big fix resets the maturity streak, so this is the one place that judgement gets recorded, and stating it now beats a later reader inferring it from a diff. A run that needed no edits logs no fix.

After applying, report accretion: run `dotnet run --project ~/.claude/bin/skill-refiner -- <skill> ratio` and pass on its lines. Don't project where the edits would land it beforehand — the estimate decides nothing, since acting on the ratio is `/skill-compact`'s separate pass either way, and the one run that tried it was wrong in the direction that flattered its own edits. Don't act on it here: removing text in the same pass that fixes friction is how the friction always wins.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
