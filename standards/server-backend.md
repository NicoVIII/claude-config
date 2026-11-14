# Server backend

Applies when designing or reviewing a server backend: its bounded contexts, use cases, API contract, database schema, or migrations.

## Shape

- **A server backend is hexagonal from the start:** ports and adapters around the core.
- **Its bounded contexts are drawn by strategic DDD, and they do not nest.** What looks like a sub-context is a module or aggregate inside one.
- **Commands are separated from queries.**

## Default stack

- **The project chooses its stack.** Where nothing argues for another: Gleam for the server, Skir for the contract, dbmate for migrations. The rules here hold whatever is chosen.

## Contexts

- **A check enforces the boundaries, not review.** Imports go inward only, and no context imports another.
- **One context reaches another only through the facade the provider publishes,** called from the consumer's infrastructure. Each such pair is declared in a list the check reads.
- **The shared kernel holds value types only.** A type qualifies when more than one context needs the same representation and none disputes what it means. Policy over it — orderings, labels, reductions — stays in the context that owns the policy.
- **Only the composition root knows every context.**
- **Isolation extends into the database.** Every table belongs to exactly one context. No statement reads another context's tables, and no foreign key crosses.

## Use cases

- **Each use case owns its handler and its ports.** A port covers one capability and is never shared between use cases.
- **A command handler owns the orchestration.** A query handler may be one line, and none is padded to look less thin.
- **Transports call the handlers directly,** with no application facade in between.
- **Where a use case is reachable through more than one transport, they behave identically.** Orchestration with an effect visible outside lives in one module all of them call, and which error becomes which status is written once.

## Contract

- **The API is contract-first, with code generated for both sides** and never edited.
- **The contract mirrors the bounded contexts and is shaped by use cases.** No endpoint is composed for one screen: screens compose queries.
- **A derivation that is a named domain concept with its own policy is a query on the context that owns it.** A fold over data the client already holds is client code.
- **The wire is as typed as the contract language allows,** and both sides still parse it into domain types.
- **A response carries no generic success or error variant.** Failure travels on the transport's error channel.
- **While client and server ship together, the contract is unversioned** ([decision](decisions/0003-unversioned-contract-while-client-and-server-ship-together.md)). A breaking change is legal only when the contract, the generated code, and both sides' adaptations land in one change, and a snapshot check catches drift.
- **Compatibility discipline starts with the first release that a separately shipped client depends on,** and it starts as a decision record.

## Database

- **The database is a boundary** ([decision](decisions/0004-the-database-is-a-boundary.md)). Other writers reach it, so its invariants are enforced in the schema: the strictest typing the engine offers, `NOT NULL`, a `CHECK` per value invariant, and primary keys that encode real identity.
- **An index is added for a read path that exists.**

## Migrations

- **An up drops data only when the domain no longer owns the concept,** and first carries forward everything a context still owns.
- **A destructive migration runs against real data only after a backup.**
- **From the first release on, an upgrade loses no user-authored data.** Data that can be fetched or derived again is exempt. Any other loss is a breaking change the release notes state plainly.
- **Owed: a migration that rewrites or drops a user-authored table ships a test** that seeds the previous schema, applies the migration, and asserts on the rows that survived.

## Not settled — raise the trade-off, leave the call to the maintainer

- **Enforcing context isolation inside SQL.** No import check sees into a statement, so review is the only enforcement there.
- **How a context reacts to a change in another.** An in-process event wired in the composition root keeps the imports clean. Whether it carries a payload, runs synchronously, or may fail the publisher is open.
- **Whether every use case ships on every transport,** where a backend has more than one.
