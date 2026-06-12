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

### MEMORY.md (index)

If MEMORY.md is missing or empty, create it with:

```
# Memory Index

- [Feedback: commit message style](feedback_commit_messages.md) — focus on WHY not WHAT; diff shows the what
```

If MEMORY.md already exists, add only the entries for files you just created.
