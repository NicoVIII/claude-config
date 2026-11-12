# Standards

How I do a kind of work, in any repository where that kind of work comes up. [`CLAUDE.md`](../CLAUDE.md) holds what applies always; a standard holds what applies under a condition — a language, a platform, an aspect of the work. They are written for me to re-read and revise, and to steer coding assistants working for me.

Each file covers one condition and opens by stating it. Nothing here loads by itself: `CLAUDE.md` lists when each file is read.

## Writing a standard

- A rule states what to do. Keep a purpose clause only where the rule would be misapplied without it.
- Rationale goes in a decision record, linked from the rule.
- State the rule without an example unless it cannot be stated otherwise: an example gets read as the rule's scope.
- Write only what departs from what a capable reader does unprompted.
- Each rule has one home. Don't restate `CLAUDE.md` or another standard.
- A rule I have not settled goes under a heading that says so, phrased as a trade-off to raise, not a choice to make.
- No reference only I can resolve.
- A standard depends on no project: no link into one, and no rule that needs one to be understood. Projects point here, never the other way.

## Decision records

A rule gets a record when the decision had real alternatives and is expensive to reverse or likely to be questioned again. Most rules get none.

Records live in `decisions/NNNN-short-title.md`, numbered in sequence, each with the context at the time, the options considered and why they lost, the decision, and its consequences. An accepted record is not edited: a changed mind gets a new record that supersedes it.
