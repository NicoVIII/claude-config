# Documentation

Applies when deciding where a piece of knowledge gets written down, when writing or reviewing a README, a `CONTRIBUTING.md`, an `AGENTS.md`, a decision record, or developer docs, and when checking docs against the code.

## Entry points

- **A repository has three entry points, one per reader:** `README.md` for whoever uses it, `CONTRIBUTING.md` for whoever changes it, `AGENTS.md` for an agent working in it.
- **An entry point holds what its reader needs first and links to the rest.** Every other doc is reachable from one of them, directly or through an overview page it links to.
- **`README.md`:** what it is, and how to install, run, and operate it, for a reader who has never seen the code. Working on it is one link to `CONTRIBUTING.md`.
- **`CONTRIBUTING.md`:** setup, the checks, and how a change lands. It is owed wherever there is a setup step or a check to run. Rationale, diagrams, and reference go to `docs/dev/`, which exists once there is something to put there.
- **`AGENTS.md` is loaded every session, so it holds only what an agent would get wrong.** A rule earns its line if working in that directory without it would likely do damage and no check enforces it. It keeps one sentence of why and links to the rest, and its links say when to read the target. It repeats nothing from `CONTRIBUTING.md`.
- **A rule that a check enforces shrinks to naming the check.**

## Where knowledge lives

- **One home per fact.** Every other place that needs it links there. The same knowledge never appears twice at full length.
- **A decision goes in a decision record** when it had real alternatives and is expensive to reverse or likely to be questioned again. A convention nobody contested gets none.
- **Goals and non-goals live in `docs/vision.md`.** A repository without one states them in its README.
- **The why of one change goes in its commit message.**

## Decision records

- **Records live in `docs/decisions/NNNN-title-with-dashes.md`, in one sequence per repository** ([decision](decisions/0002-one-decision-log-per-repository.md)). A part released on its own, with its own version and changelog, keeps its own.
- **A record follows MADR's minimal template,** with the `status` and `date` front matter of the full one.
- **An accepted record is never edited,** except to set the status that links the record superseding it. A changed mind gets a new record.
- **The context is written for a reader who was not there.**
- **Every option listed could have won,** and each says why it lost.
- **The decision is stated as a rule someone can follow.**
- **The consequences include the bad ones.**
- **A record written after the fact carries the date of the decision and says it was backfilled.**
- **A record is read when its area is touched, never loaded as a standing instruction.** The `AGENTS.md` of the part it governs states the current rule and links to it.
- **A settled decision is not reargued in review.** Disagreement is a proposed record that supersedes it.

## Checking docs against the code

- **A claim counts as checked once it was looked up:** the path exists, the command ran, the code does what the text says.
- **A setup or run instruction that fails is a defect, not a nit.**
- **A record's context and decision are history and never drift.** Its consequences do, wherever they state an obligation that still holds.

## Not settled — raise the trade-off, leave the call to me

- **Where user docs beyond the README live:** in `docs/` itself, or in a folder of their own beside `docs/dev/`.
