---
name: skill-compact
description: Shrink a skill whose SKILL.md has accreted rules — merge, generalize, or move text out until the file is smaller than it started. Use when a skill has bloated, when /skill-retro reports it past its growth trigger, or when I say a skill has too much text.
---

Reduce a skill's SKILL.md without losing what it knows.

## Scope

One skill, named as argument. Start with `dotnet run --project ~/.claude/bin/skill-refiner -- <skill> ratio`. Its last line splits the file into rules and words per rule: more words per rule points at the ladder's shrink rungs, more rules at its remove and move-out rungs. Its growth trace says when the words arrived and which entry's clause explains each rise — the fastest way to the passages worth reading first.

The pass must end with **fewer words in the SKILL.md than it started** — that file is the whole measure; text the pass moves out costs nothing against it, into a new file or an existing one — provided a file the consuming run does load still points at it. Only the SKILL.md is guaranteed in context; everything else is read only when an agent opens it. Moving a fact obliges you to update its other readers in the same pass — a half-migrated fact is what some other skill's next run rediscovers. Generalizing three rules into one principle inserts text and still counts. Adding a rule for something you noticed while reading does not — mention it and let a retro decide. Ending still above the trigger is a fine outcome: the pass is bounded by the candidates that carry evidence, not by the ratio.

## Find candidates

Read the SKILL.md against its `HISTORY.md` and `git log -p` for its directory. Every rule was added for a reason; `git blame` names the commit and its message gives the reason. Rank by:

- **Prose that only makes a model reproduce a fixed pipeline** — flags, field names, counting rules re-derived every run. The ladder's script rung turns it into one, and this is usually where the words actually are. A mechanism the git log shows patched more than twice is the same signal.
- **A fact this SKILL.md restates from elsewhere** — a `references/` file, the README, another skill. The copy drags prose explaining which copy wins, and `git log` will show one copy corrected while the others drifted. Give the fact one home; the explanation goes with it.
- **Special cases that never recurred** — a clause added for one run's mishap, with nothing resembling it in the log since.
- **Rules the skill's suggested model would follow unprompted** — check the README table for which model that is; write for it, not for the model doing the compaction.
- **Several rules that are one principle** — coverage survives, the words don't.

You cannot observe that a rule is unnecessary — a clean run is equally consistent with the rule working and with it never having been needed. So don't decide alone. Present each candidate with its evidence (what it was added for, what the log shows since) in prose, and take a free-form pick ("all", "1 and 3", "merge 2 into 4") — the evidence *is* the decision, and `AskUserQuestion`'s option slots hold neither it nor candidates that depend on each other. A deletion is an experiment the run log will judge, and git makes reverting free. If you quote what a candidate saves, measure it against a drafted replacement — eyeballed numbers ran 40% high the one time they were tried. Never project the pass's final ratio: it decides nothing, and the run that gave one promised clearing a trigger it then missed (77e45b2).

## Ladder

Take the strongest rung of `~/.claude/references/prose-ladder.md` that fits each candidate — read it, don't recall it. Retro findings resolve against the same list, which is why it lives in one file.

## Finish

Leave the earlier entries of `HISTORY.md` alone — the baselines already there are the only record of whether the floor is rising.

Record the new baseline: `dotnet run --project ~/.claude/bin/skill-refiner -- <skill> log compacted '<one line>'`, which measures the file itself. The clause says what the pass cut, in the terms the candidates were picked by ("merged the retraction rules", "moved the model table to references") — it is the growth trace's only account of why the size fell. Commit the skill edits and the log together.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
