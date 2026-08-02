# The prose ladder

The one list of moves for getting text out of a SKILL.md. A retro resolving a
finding and a compaction pass both pick from here, so neither can drift from
the other — read it, don't recall it. Take the move that leaves the least
prose in the SKILL.md, not the smallest edit that works: the bands are
ordered, and within a band the kind of text decides.

Remove — no text survives:

1. **Fix the artifact the instruction works around**, so the instruction goes
   away instead of improving.
2. **Delete the mechanism** — right when the fact has another home, or the
   case it guards never recurred.

Move out — the text survives, but not here:

3. **Move mechanics into a script** beside the SKILL.md when the prose only
   makes a model reproduce a fixed pipeline — [`references/helpers.md`](helpers.md)
   carries that signal and the shell-or-F# rules — cutting the prose to an
   invocation.
4. **Move text into `references/`** — *only* if some runs need it and others
   don't. Always-needed content in a reference file is the same tokens plus a
   round trip, which is pure indirection.
5. **Split into a second skill** — only where the extracted part is useful on
   its own. One half depending on the other is ordinary layering; neither
   standing alone is the failure.

Shrink — fewer words in place:

6. **Merge or generalize** several rules into one.
7. **State the exact command inline** when it reads in one line.
8. **Reword** — the last resort and never the default: rewording is what
   accretes in the first place.
