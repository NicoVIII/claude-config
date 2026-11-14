# Standards

How a kind of work is done in [NicoVIII](https://github.com/NicoVIII)'s repositories, wherever that kind of work comes up. Each file covers one condition — a kind of software, an aspect of the work — and opens by stating it. They are written for people to re-read and revise, and for coding agents to be steered by.

- [`architecture.md`](architecture.md) — module structure, domain types, boundaries, error handling
- [`testing.md`](testing.md) — which tests exist, what they assert, where they live
- [`repo-setup.md`](repo-setup.md) — task runner, hooks, CI, devcontainer, dependency updates, releases
- [`documentation.md`](documentation.md) — where knowledge is written down, and what a decision record holds
- [`server-backend.md`](server-backend.md) — a backend's contexts, use cases, contract, schema, migrations

## Using them in a repository

Nothing here loads by itself. A repository takes this one in as a git submodule at `docs/standards/`, and its `AGENTS.md` lists the standards that apply to it, each with the work it is read before. Where that folder is empty, `git submodule update --init` fills it.

What a repository does differently is stated in its own `AGENTS.md`, never in a changed copy of a standard.

## Writing a standard

- A rule states what to do. Keep a purpose clause only where the rule would be misapplied without it.
- Rationale goes in a decision record, linked from the rule.
- State the rule without an example unless it cannot be stated otherwise: an example gets read as the rule's scope.
- Write only what departs from what a capable reader does unprompted.
- Each rule has one home. Don't restate another standard.
- A rule that is not settled goes under a heading that says so, phrased as a trade-off to raise, not a choice to make.
- No reference only the maintainer can resolve.
- A standard depends on no project: no link into one, and no rule that needs one to be understood. Projects point here, never the other way.

## Decision records

A rule's record follows [`documentation.md`](documentation.md), which says when a decision gets one and what it holds. Most rules get none.

Records live in `decisions/NNNN-short-title.md`, numbered in sequence.
