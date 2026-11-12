# Architecture

Applies when designing or reviewing module structure, domain types, boundaries, or error handling — in any language, at any project size.

## Rules

- **Functional core, imperative shell, at every scale.** Domain logic is pure and immutable. IO, network, persistence, clock, and randomness sit in a thin shell at the boundary, which orchestrates and holds no domain rules.
- **Organise by feature slice and bounded context, never by layer.** The core/shell split sits inside each slice.
- **Bounded contexts do not nest.** What looks like a sub-context is a module or aggregate inside one.
- **Make illegal states unrepresentable.** Use sum types wherever a boolean flag, an optional field, or a string would encode an invariant the type system can hold.
- **Parse, don't validate.** Check once at the boundary into a type that is correct by construction. Nothing downstream validates again.
- **Newtypes over primitives for domain values**, opaque where the language allows.
- **Errors are values.** Expected failures are result types, propagated and never unwrapped and rethrown. Exceptions are for what the program cannot continue from. Where a dependency throws, map it to a value once, at that boundary.
- **A dependency's own type safety gates adopting it.** A loosely typed one is wrapped in a typed adapter in the shell.
- **Never hand-roll what is large, security-sensitive, or solved correctly once**: cryptography, parsers, date and time, serialisation.
- **Tactical DDD lives in types and names, not in ceremony.** Value objects are the newtypes, aggregate invariants are the unrepresentable states, and the ubiquitous language is naming: domain terms in code match the domain's vocabulary.
- **Types stay readable.** Where an invariant can only be encoded in types nobody can read, take the simpler design.
- **Don't propose layered architecture, service locators, or anaemic models with mappers as improvements.**

## When rules conflict

1. Correctness and readability, equal at the top.
2. Safety over velocity.
3. Safety over ecosystem convenience: a gap means writing it or picking a safer primitive.

## Not settled — raise the trade-off, leave the call to me

- **Ports and dependency inversion.** Functional core / imperative shell is the default. Formal hexagonal structure enters a slice only once the slice has earned it.
- **Strategic DDD and CQRS.** They are the toolkit once a project goes hexagonal. Name the cost in eventual consistency and a second model before reaching for them.
