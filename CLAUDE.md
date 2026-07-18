# Personal preferences

Cross-project preferences, loaded into every session. Personal — never copy them into project repos or force them on contributors. Project-level instructions (AGENTS.md) win where they conflict.

## Interaction

- Ask before assuming and proceeding in a direction that may be wrong; surface ambiguities before starting.
- Be critical and factual; no sycophancy. Push back when something is wrong or questionable, regardless of who said it. If something is unclear or uncertain, say so directly.

## Attribution

- Your writing and mine must never be mistakable for each other on anything persisted where others read it. Where a git convention already carries authorship (commit `Co-Authored-By`, PR footer), rely on it. Where none does — GitHub comments and reviews, prose files you author wholesale — add a short marker (e.g. "— written by an agent"). Attribute when you author a whole artifact or a self-contained section; skip it for edits or fixes within my text. Never mark code or inline comments; blame covers those.

## Commits

- Commit proactively after each independently meaningful change — don't wait to be asked. The full check suite must pass before every commit; no exceptions for lint or formatting.
- When a change is inherently atomic (e.g. a rename that won't compile until all sites update), accept one larger commit rather than an artificial split.
- Commit messages explain WHY — reasoning, trade-offs, non-obvious constraints — not WHAT; the diff carries the what.
- Never push, and don't ask about pushing — I push manually.

## Code style

- Prefer small, composable, single-purpose functions; a well-named function is the default unit of decomposition. A `// this block does X` comment is a trigger to extract a named function instead. Skip extraction only when it would reduce clarity: helpers needing many threaded parameters, or one-shot blocks that add pure indirection.
- Order files top-to-bottom F#-style: every definition references only things defined above it. Helpers before callers, types before functions that use them, entry point last. Exceptions only for circular dependencies.
- Comments explain WHY, not WHAT — if a reader could infer it from types and names, cut it. Carve-outs where prose is warranted: doc comments on public APIs; type-lossy seams where the signature can't express the contract (anonymous `String`/`Int` parameters, unencoded error semantics, idempotency assumptions); short orientation labels in long functions (`// Phase 2: reconcile`).

## Documentation for agents

- After any refactoring or structural decision, ask: would an agent need this to place new code correctly? If yes, update the relevant AGENTS.md at the appropriate directory level.
- AGENTS.md and memory files load into context on every invocation — every word has token cost. Include only what can't be inferred from the code; no padding, no restating the obvious.
