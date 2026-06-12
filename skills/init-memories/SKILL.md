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

**Why:** The diff shows the what; the commit message captures what isn't visible from the code alone.

**How to apply:** Before writing, ask: "could a reader infer this from the diff?" If yes, cut it.
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

**How to apply:** Commit mid-plan, not just at the end.
```

---

### feedback_interaction_style.md

```
---
name: feedback_interaction_style
description: How to interact with this user — ask before assuming, be critical and factual, no sycophancy
metadata:
  type: feedback
---

Ask before making assumptions and proceeding in a direction that may be wrong.

Do not simulate competence — if something is unclear or uncertain, say so directly.

Do not confirm or agree with what the user says just because they are the user. Be critical and factual. Push back when something is wrong or questionable, regardless of who said it.

**Why:** The user explicitly values being asked over having assumptions made. Sycophantic agreement and false confidence are actively unhelpful to them.

**How to apply:** Surface ambiguities before starting. Judge the user's claims on the merits, not on who stated them.
```

---

### feedback_agents_md_updates.md

```
---
name: feedback_agents_md_updates
description: After structural decisions, check whether AGENTS.md needs updating so agents can place new code correctly
metadata:
  type: feedback
---

After any refactoring or structural decision, ask: would an agent need to know this to place new code correctly? If yes, update or create the relevant AGENTS.md (at the appropriate directory level, not always the root).

AGENTS.md files are loaded into every agent context — every word has token cost. Write them to be minimal and precise: only include what an agent cannot infer from the code itself. No prose padding, no restating the obvious, no examples that don't add information the structure alone doesn't convey.

**Why:** Structural conventions like mirroring rules, naming patterns, or layer boundaries are non-obvious. Without documenting them, agents will either infer incorrectly or ask repeatedly. But bloated AGENTS.md files waste tokens on every invocation and dilute the signal.

**How to apply:** Make this a standard step at the end of any plan that introduces or changes structural conventions.
```

---

### MEMORY.md (index)

If MEMORY.md is missing or empty, create it with:

```
# Memory Index

- [Feedback: commit message style](feedback_commit_messages.md) — focus on WHY not WHAT; diff shows the what
- [Feedback: commit cadence](feedback_commit_cadence.md) — commit proactively after each meaningful working change, full suite must pass
- [Feedback: interaction style](feedback_interaction_style.md) — ask before assuming, be critical and factual, no sycophancy
- [Feedback: AGENTS.md updates](feedback_agents_md_updates.md) — after structural decisions, check if AGENTS.md needs updating
```

If MEMORY.md already exists, add only the entries for files you just created.
