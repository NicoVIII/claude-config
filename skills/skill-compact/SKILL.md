---
name: skill-compact
description: Shrink a skill whose SKILL.md has accreted rules — merge, generalize, or move text out until the file is smaller than it started. Use when a skill has bloated, when /skill-retro reports it past its growth trigger, or when I say a skill has too much text.
---

Reduce a skill's SKILL.md without losing what it knows.

## Scope

One skill, named as argument. Start with `~/.claude/bin/skill-refiner.sh <skill> ratio`. Its last line splits the file into rules and words per rule: more words per rule points at the ladder's shrink rungs, more rules at its remove and move-out rungs. Its growth trace says when the words arrived and which entry's clause explains each rise — the fastest way to the passages worth reading first.

The pass must end with **fewer words in the SKILL.md than it started** — that file is the whole measure. What bounds the pass is the candidates that carry evidence, not the ratio: ending still above the trigger is a fine outcome. Text the pass moves out costs nothing against it, into a new file or an existing one, provided a file the consuming run does load still points at it. Moving a fact obliges you to update its other readers in the same pass. Generalizing three rules into one principle inserts text and still counts. Adding a rule for something you noticed while reading does not — mention it and let a retro decide.

## Find candidates

Read the SKILL.md against its `HISTORY.md` and `git log -p` for its directory: every rule was added for a reason, and the commit that added it states the reason. Rank by:

- **Sentences that justify a rule rather than instruct its executor** — motivation, war stories, why a gap exists; the test `author-skill` applies when drafting (12e09b7). This is where the words are, and a low ratio is no evidence against it (75bd76e).
- **Prose that only makes a model reproduce a fixed pipeline** — flags, field names, counting rules re-derived every run. The ladder's script rung turns it into one. A mechanism the git log shows patched more than twice is the same signal.
- **A fact this SKILL.md restates from elsewhere** — a `references/` file, the README, another skill; `git log` shows one copy corrected while the others drifted. Give the fact one home; the explanation goes with it.
- **Special cases that never recurred** — a clause added for one run's mishap, with nothing resembling it in the log since.
- **Rules the skill's suggested model would follow unprompted** — check the README table for which model that is; write for it, not for the model doing the compaction.
- **Several rules that are one principle** — coverage survives, the words don't.

Don't decide alone — no run can show a rule was unnecessary. Present each candidate with its evidence (what it was added for, what the log shows since) in prose, and take a free-form pick ("all", "1 and 3", "merge 2 into 4") — the evidence *is* the decision, and `AskUserQuestion`'s option slots hold neither it nor candidates that depend on each other. Quote a candidate as the measured size of its block, never as what it saves. Nor project where the pass lands — the final ratio, the word count, or which baseline it would clear.

## Ladder

Take the strongest rung of `~/.claude/references/prose-ladder.md` that fits each candidate — read it, don't recall it.

## Finish

Leave the earlier entries of `HISTORY.md` alone — the baselines already there are the only record of whether the floor is rising.

Record the new baseline: `~/.claude/bin/skill-refiner.sh <skill> log compacted '<one line>'`, which measures the file itself. The clause says what the pass cut, in the terms the candidates were picked by ("merged the retraction rules", "moved the model table to references"). Commit the skill edits and the log together.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
