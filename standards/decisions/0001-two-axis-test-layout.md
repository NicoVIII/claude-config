# 0001 — Owed tests and earned tests are laid out on different axes

- Status: accepted
- Date: 2026-11-12 (decided 2026-06-14 in one of my projects, recorded here when the rule became a standard)

## Context

An architecture with a functional core and adapters around it creates a fixed set of things to verify per slice: the assembled slice through its entry point, the pure mappings at the transport, and the adapters against real-ish infrastructure. The hard logic worth a fast, targeted test can sit in any layer. One directory axis cannot express both "what coverage is owed" and "where the hard bit lives" without lying about one of them.

## Considered options

- **A single axis** — mirror the source tree, or the unit/integration/e2e pyramid. It forces the mandatory set and the supplementary set into one taxonomy, and an empty directory becomes ambiguous: a test owed and missing, or nothing to test.
- **Two levels on two axes** — owed tests cut by architectural role, where an empty directory means a test is owed and missing; unit tests cut by source location, where a directory has to be earned.

## Decision

Owed tests are grouped by what they cover, unit tests by where their subject lives. The two axes are not unified.

## Consequences

- An empty directory for an owed test is kept as a visible marker of the debt, and an empty directory for unit tests is forbidden. The same signal means opposite things on the two levels, so a project using the layout has to say so where its tests live.
- Proving that a fake matches its real adapter, and tests through the real transport, sit outside this layout and stay open.
- The nesting is for readers. Whether it is free depends on the test runner: one that discovers tests wherever they sit pays nothing for it.
