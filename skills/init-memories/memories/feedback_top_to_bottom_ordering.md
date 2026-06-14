---
name: feedback_top_to_bottom_ordering
description: "Files must be ordered top-to-bottom F#-style — every definition may only reference things defined above it"
metadata:
  type: feedback
---

Order definitions top-to-bottom: every function, type, or constant may only reference things already defined above it in the same file. This mirrors F# source ordering. Exceptions only when circular dependencies make it genuinely impossible.

**Why:** User's explicit style preference — reads naturally, avoids forward-reference surprises.

**How to apply:** When writing or refactoring any file, place helper/leaf functions before their callers, types before functions that use them. The public entry-point typically goes last. Apply this whenever editing or generating any file in this project.
