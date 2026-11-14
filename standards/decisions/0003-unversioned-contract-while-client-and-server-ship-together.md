---
status: accepted
date: 2026-05-31
---

# The contract is unversioned while client and server ship together

Backfilled on 2026-11-14 from one of my projects, where this was decided on the date above, when the rule became a standard.

## Context and Problem Statement

A backend and its client are connected by a contract, with code generated from it for both sides. Both live in one repository and ship as one unit to a single deployment, so no client ever runs against a server of another version.

A contract still changes, and some changes break. How is a breaking change made?

## Considered Options

- A versioned contract with a deprecation cycle
- An unversioned contract, where a breaking change lands on both sides at once

## Decision Outcome

Chosen option: an unversioned contract. A breaking change is legal only when the contract, the code generated from it, and the adaptations of server and client land in one change. A snapshot of the contract is checked in, and a check fails when the contract and the snapshot disagree.

Versioning is the safe default for an API others consume. Here it would maintain compatibility shims for consumers that cannot exist, since client and server are never deployed apart.

The rule holds only as long as that is true. The first release a client I don't ship depends on ends it, and what replaces it is a decision of its own.

### Consequences

- Good, because nothing is spent on versions and shims, and the generated types keep both sides aligned at every commit.
- Good, because the snapshot check turns accidental drift into a failed check instead of a runtime surprise.
- Bad, because a contract change makes for a wide change: it has to cross every layer on both sides at once.
- Bad, because an outside consumer of this contract breaks the model the day it appears. Third-party access needs another door or a versioning decision before it is offered.
