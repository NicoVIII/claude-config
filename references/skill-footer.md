# Skill feedback footer

The canonical wording, so no skill has to copy it from another one. Every skill
below 🟢 Usable in the README maturity table ends with exactly this — the rule,
the paragraph, and nothing after it. It asks for friction, and has a run that
met none log itself, so a run costs a review only when there is something to
review:

```markdown
---

This skill is not yet battle-tested. At the end of the run, say briefly whether any instruction above was ambiguous, wrong, or needed a workaround. If none was and I corrected nothing during the run, log it instead: from the session repo's working directory run `~/.claude/bin/skill-refiner.sh <this skill's name> log run`, then commit the `HISTORY.md` line it names, alone, in the repo that holds the skill. Where that script does not exist, skip the logging.
```
