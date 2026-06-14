---
name: feedback_small_composable_functions
description: "Default to small single-purpose functions as the unit of decomposition; a named function often replaces a 'what' comment"
metadata:
  type: feedback
---

Prefer small, composable functions that each do one thing. A named function is the default
unit of decomposition — reach for it even where there is no comment to remove, because a good
name makes intent legible to humans and agents and keeps each function reason-about-able in
isolation.

A frequent trigger: a `// this block does X` comment marking a sub-step. Extracting that block
into an `x(...)` function usually beats the comment — the name carries the what and the code
stays flat. This is the positive counterpart to [[feedback_why_not_what]].

**Boundary — when NOT to extract:** extraction is a win only when it *reduces* complexity, not
when it merely relocates it. Skip it when the helper would need many threaded parameters (the
call site becomes as noisy as the inlined code) or when a one-shot block adds only a jump
without clarifying intent. Judge by whether the extracted name + signature reads more clearly
than the inline block, not by line count.

**How to apply:** when a function does several things in sequence, look for cohesive sub-steps
to name and lift out — but only where the helper has a tight, low-arity signature. Prefer this
over leaving a long function annotated with section comments.
