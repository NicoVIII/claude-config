---
name: init-memories
description: Initialize persistent memories that aren't derivable from the codebase. Run on a fresh machine after pulling ~/.claude to restore preferences and feedback.
---

Check if the following memory files exist in the current project's memory directory. Create each one that is missing. Do not overwrite existing files. After creating any files, ensure MEMORY.md contains an entry for each.

---

### feedback_commit_messages.md

```
---
name: feedback_commit_messages
description: How to write commit messages — focus on WHY not WHAT
metadata:
  type: feedback
---

When writing commit messages, focus on the reasoning and context — WHY the change was made this way — not on describing WHAT changed (the diff already shows that).

**Why:** The diff shows the what. The commit message's job is to explain the intent, the constraint, the design decision, or the tradeoff that isn't visible from the code alone.

**How to apply:** Before writing the body of a commit message, ask: "could a reader infer this from the diff?" If yes, cut it. Keep only what requires context to understand.
```

---

### feedback_commit_cadence.md

```
---
name: feedback_commit_cadence
description: When and how to commit during complex multi-step implementations
metadata:
  type: feedback
---

Commit proactively after each independently meaningful change — don't wait to be asked. A meaningful change is one that stands on its own conceptually (not tied to plan step boundaries).

The full check suite must pass before every commit. No exceptions for lint or formatting.

When a change is inherently atomic (e.g. a type rename touching many files that won't compile until all sites are updated), accept one larger commit for the whole thing rather than forcing an artificial split.

**Why:** Small working commits give natural checkpoints, make the history readable, and avoid a large all-or-nothing diff at the end of a plan.

**How to apply:** During implementation, pause and commit whenever a coherent, passing unit of work is complete — even mid-plan. Don't batch everything until the end.
```

---

### MEMORY.md (index)

If MEMORY.md is missing or empty, create it with:

```
# Memory Index

- [Feedback: commit message style](feedback_commit_messages.md) — focus on WHY not WHAT; diff shows the what
- [Feedback: commit cadence](feedback_commit_cadence.md) — commit proactively after each meaningful working change, full suite must pass
```

If MEMORY.md already exists, add only the entries for files you just created.
