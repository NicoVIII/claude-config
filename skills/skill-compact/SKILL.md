---
name: skill-compact
description: Shrink a skill whose SKILL.md has accreted rules — merge, generalize, or move text out until the file is smaller than it started. Use when a skill has bloated, when /skill-retro reports it past its growth trigger, or when I say a skill has too much text.
---

Reduce a skill's SKILL.md without losing what it knows.

`/skill-retro` adds; this removes. They stay separate passes because a removal that also fixes friction spends all its attention on the friction — every logged size reduction so far was cancelled by additions made in the same commit.

## Scope

One skill, named as argument. Start with `dotnet run --project ~/.claude/bin/runlog -- ratio <skill>`. Its second line splits the file into rules and words per rule: more words per rule points at rungs 1–2, more rules at rungs 3–5.

The pass must end with **fewer words in the SKILL.md than it started** — that file is the whole measure; text the pass moves out costs nothing against it, whether into a file it creates or into one that is already the fact's better home (`AGENTS.md`, the README, a skill that consumes the same fact). Moving a fact obliges you to update its other readers in the same pass — a half-migrated fact is what some other skill's next run rediscovers. Generalizing three rules into one principle inserts text and still counts. Adding a rule for something you noticed while reading does not — mention it and let a retro decide. Ending still above the trigger is a fine outcome: the pass is bounded by the candidates that carry evidence, not by the ratio.

## Find candidates

Read the SKILL.md against its `RUNS.md` and `git log -p` for its directory. Every rule was added for a reason; `git blame` names the commit and its message gives the reason. Rank by:

- **Prose that only makes a model reproduce a fixed pipeline** — flags, field names, counting rules re-derived every run. Rung 3 turns it into a script, and this is usually where the words actually are. A mechanism the git log shows patched more than twice is the same signal.
- **A fact this SKILL.md restates from elsewhere** — `AGENTS.md`, the README, another skill. The copy drags prose explaining which copy wins, and `git log` will show one copy corrected while the others drifted. Give the fact one home; the explanation goes with it.
- **Special cases that never recurred** — a clause added for one run's mishap, with nothing resembling it in the log since.
- **Rules the skill's suggested model would follow unprompted** — check the README table for which model that is; write for it, not for the model doing the compaction.
- **Several rules that are one principle** — coverage survives, the words don't.

You cannot observe that a rule is unnecessary — a clean run is equally consistent with the rule working and with it never having been needed. So don't decide alone. Present each candidate with its evidence (what it was added for, what the log shows since) in prose, and take a free-form pick ("all", "1 and 3", "merge 2 into 4") — the evidence *is* the decision, and `AskUserQuestion`'s option slots hold neither it nor candidates that depend on each other. A deletion is an experiment the run log will judge, and git makes reverting free. If you quote what a candidate saves, measure it against a drafted replacement — eyeballed numbers ran 40% high the one time they were tried. Never project the pass's final ratio: it decides nothing, and the run that gave one promised clearing a trigger it then missed (77e45b2).

## Ladder

Take the furthest move down this list that fits:

1. **Reword.** Weakest — rewording is what accretes in the first place.
2. **Merge or generalize** several rules into one.
3. **Move mechanics into a script** beside the SKILL.md; `references/helpers.md` carries when that fits.
4. **Move text into `references/`** — *only* if some runs need it and others don't. Always-needed content in a reference file is the same tokens plus a round trip, which is pure indirection.
5. **Split into a second skill** — only where the extracted part is useful on its own. Two skills that always run together cost more than one file did.

## Finish

Leave `RUNS.md` alone — `ratio` reads its baseline from the line you are about to write and its floor from every earlier one, so tidying old baselines away resets the only record of the floor rising; `maturity` counts the verdict lines as runs.

Record the new baseline: `dotnet run --project ~/.claude/bin/runlog -- log <skill> compacted`, which measures the file itself. Commit the skill edits and the log together.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
