---
name: pick-model
description: Recommend which Claude model (Haiku, Sonnet, Opus, Fable) fits the task at hand in an interactive Claude Code session, so a too-strong model doesn't burn budget and a too-weak one doesn't waste retries. Use when I ask which model to use, whether the current model is overkill, whether a cheaper model would do, what model a task needs, or say pick a model.
---

Recommend the model for the task I describe (or the work already underway in this session). You only recommend — switching is my move via `/model`. Session context survives a switch, but the new model re-reads it as uncached input: a one-time cost proportional to conversation length. Near-free early in a session, real deep into a long one — factor that into the advice.

## Ladder

Recommend the cheapest tier that plausibly handles the task. Cost and capability rise together: Haiku ≪ Sonnet < Opus < Fable. On subscription plans the cost is usage-limit burn rather than dollars — the ratios still hold.

- **Haiku** — mechanical and fully specified: renames, formatting, boilerplate, running commands and triaging their output, summarizing, single-file edits with an obvious shape. Wrong when the task needs multi-file reasoning or leaves ambiguity to resolve.
- **Sonnet** — the default workhorse: routine features, bug fixes with a clear repro, tests, refactors inside established patterns, procedural skill runs. When in doubt between Haiku and Sonnet, take Sonnet — Haiku's savings are small next to one failed attempt.
- **Opus** — judgment-heavy: debugging without a repro, cross-cutting refactors, design and architecture decisions, code review, unfamiliar codebases.
- **Fable** — escalation target, rarely a starting point: problems Opus stalled on.

## Overrides

These beat the ladder:

- **Start high when failure is hard to detect.** The ladder assumes a cheap model's failure is visible, so escalating fixes it. If a subtle error would survive review (security-sensitive changes, tricky concurrency, irreversible operations), start at Opus or above.
- **Write-once-run-many text starts at the top.** Skills, prompts, and agent-facing docs steer every future run; authoring them wants Fable (`~/.claude/AGENTS.md` encodes the same rule for skills).
- **Repo skills are pre-decided.** For skills in `~/.claude/skills`, the "Suggested model" column in the README maturity table is the answer — don't re-derive it.
- **Split multi-phase work.** For larger features: plan on Opus, execute on Sonnet. If the `/model` menu offers a plan/execute hybrid, that's the mechanism.

## Mid-session signals

- Escalate after the second correction or re-prompt on the same problem — repeated retries on a weak model cost more than the stronger model would have, even counting the context re-read the switch triggers.
- Recommend downgrades as readily as upgrades, including away from the model currently running you — catching overkill is the point of this skill; no loyalty to the incumbent. But weigh the switch cost: with a large context and little work left, the re-read can exceed what the cheaper model saves, and staying put wins.

## Cost questions

Never quote prices from memory. If I ask for dollar figures or per-model pricing, load the `claude-api` skill for current numbers.

## Scope

Session model choice only. Out of scope: model selection for API calls in applications I build, and per-subagent model overrides — say so and stop if asked.

## Output

One recommendation, the one-line reason, and — if it differs from the current model — the reminder that `/model` switches without losing context. No rubric recital.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
