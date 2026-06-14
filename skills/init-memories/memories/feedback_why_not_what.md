---
name: feedback_why_not_what
description: WHY not WHAT in comments and commits; grouping/orientation labels are allowed and encouraged
metadata:
  type: feedback
---

In both commit messages and code comments, explain WHY — reasoning, context, trade-offs, non-obvious constraints — not WHAT. The artifact itself already carries the what, so restating it is noise that drifts out of sync.

- **Commit messages:** the diff shows what changed; the message captures what the diff can't — why the change was made this way.
- **Code comments:** the types and names carry what the code does; prose is reserved for rationale and non-obvious constraints.

**Carve-out — public APIs:** Public library APIs get real doc comments regardless. A library's interface is a contract for callers who can't read its internals — documenting it tells a caller what they need, which isn't the same as narrating what the code does.

**Carve-out — type-lossy seams:** Any definition where the type signature cannot express the full contract warrants a brief annotation. Common cases: anonymous generic parameters (`String`, `Int`) whose role isn't obvious from context, error semantics that aren't encoded in the type, idempotency assumptions. This applies to port/interface definitions, type aliases for callbacks, and similar seams that adapter authors read as a contract. The underlying reason is the same as for public APIs — the reader cannot resolve ambiguity by reading the implementation.

**Carve-out — grouping/readability:** Short labels that chunk a long function or case expression into named sections are allowed and encouraged. These are orientation markers, not explanations: a one-line comment like `// Fallback path` or `// Phase 2: reconcile` tells the reader where they are in the flow without restating what each line does. Favour these over prose that explains logic the code already expresses.

**How to apply:** Before writing, ask: "could a reader infer this from the artifact itself — the diff, or the types and names?" If yes, cut it. Reserve prose for the reasoning. Exceptions: public library API surfaces; type-lossy seams where the type is too generic to convey the contract; orientation markers that chunk a long block.
