---
status: accepted
date: 2026-07-12
---

# The database is a boundary

Backfilled on 2026-11-14 from one of my projects, where this was decided on the date above, when the rule became a standard.

## Context and Problem Statement

Domain invariants are enforced by parsing at the boundary into types that are correct by construction. If the application were the only writer of its database, constraints in the schema would validate a second time what the types already guarantee.

It rarely is. In the project this comes from, a bulk load went through the database's own command-line tool, migrations and manual repairs edited the data directly, and the database outlived every version of the application. That project had just seen the failure: writers outside the application's types reaching tables whose declared types nothing enforced.

Which invariants does the schema hold?

## Considered Options

- The types own the invariants, and the schema holds keys and `NOT NULL` only
- The schema holds structure — keys and uniqueness — and value rules live in code
- The database is a boundary, and invariants are enforced on the way into it too

## Decision Outcome

Chosen option: the database is a boundary. A table carries the strictest typing the engine offers, `NOT NULL`, a `CHECK` for each value invariant, and a primary key that encodes real identity. A new table without them is a finding, not a matter of style.

The first option rests on the application being the only writer, which was false. The second leaves value rules to code that the other writers never run, and where the engine coerces types silently it leaves that open as well.

### Consequences

- Good, because malformed data fails when it is written, with a clear error, through every writer and not only through the application.
- Bad, because a value `CHECK` duplicates a domain rule by design: when the rule changes, the schema needs a migration in the same change.
- Bad, because an engine's strict mode can cost in tooling and in migrations. In the project this comes from it set a minimum version for every tool that opens the database, and changing an existing table meant recreating it and copying the rows.
