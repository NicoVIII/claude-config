---
status: accepted
date: 2026-11-14
---

# One decision log per repository, in `docs/decisions/`

## Context and Problem Statement

A repository with several parts — a server, a client, a contract between them — has to put its decision records somewhere. A record should be found by whoever touches the area it governs, which argues for keeping it near that area. But a record is immutable, and the directory layout around it is not. My repositories had also drifted apart on the name: `docs/decisions/` in one, `docs/adrs/` in another.

Where do records live, and how are they numbered?

## Considered Options

- One log per repository, in `docs/decisions/`, with one number sequence
- A `decisions/` folder beside each part, and one at the root for what belongs to no single part
- One log per repository, split into a subfolder per category, as MADR allows

## Decision Outcome

Chosen option: one log per repository, in `docs/decisions/`, with one number sequence. The `AGENTS.md` of each part links the records that govern it. Only a part released on its own, with its own version and changelog, keeps a log of its own.

A folder per part lost on what records turn out to be about. In the repository of mine with the most records, about a third of 25 belonged to a single part, going by their titles. The rest decided something about the domain, the contract, or which part does what: they would have gone to the root folder anyway, or needed a call each time on where they belong. It also ties the path of a record that never changes to a directory that can be renamed or split.

Category subfolders lost for the same placement problem, and because MADR numbers each category separately: a number alone no longer names a record.

The path and the file name are MADR's defaults, which settles `decisions` against `adrs` without a preference of my own.

### Consequences

- Good, because where a new record goes is never a question, and its number names it across the whole repository.
- Good, because moving or splitting a part leaves every record and every link to one where it was.
- Bad, because a record is not found by walking up from the code it governs. That depends on the part's `AGENTS.md` linking it, and a missing link fails silently.
- Bad, because a repository that keeps its records elsewhere has to move them, and each link to them with it.
- Bad, because "released on its own" has not met a real case. It is the criterion that needed no judgement per record, not one that was tried.
