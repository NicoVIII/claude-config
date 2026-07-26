# Writing a helper for a skill

Read this before adding an executable helper to a skill, or when deciding
whether one should be a shell script or an F# project.

## When a skill should ship one

A skill may ship an executable helper beside its `SKILL.md` when a step is a
fixed tool pipeline with no judgement in it (`prioritize/gather/`, eec9963).

The signal to reach for one is SKILL.md prose that exists only to make a model
reproduce exact flags and error strings: that text reloads into context every
run and regresses, a script doesn't.

Keep judgement and anything conditional in the SKILL.md.

## What the helper must guarantee

Make the helper abort loudly. A partial result the reader cannot tell apart
from good news is worse than no result.

## Shell or an F# project

A helper that owns a **contract** — output it decodes, a format with a second
reader, or a closed taxonomy — is an F# project, so that changing the contract
is an error at every site that has to decide about it:

- Name the fields as types. A renamed or dropped one is then a parse error, not
  a null reading downstream as "nothing here".
- Name the cases as a union.
- Set `<WarningsAsErrors>FS0025</WarningsAsErrors>`, or the exhaustiveness is
  only a warning (7e0c921 is what that costs when it is missed).
- Split so each file answers one question, and let the compile order carry the
  dependencies.

Only a helper performing a single mechanical action nobody parses is a shell
script.

A shell one has to pass `just shell` — `shellcheck -o all -S style` and
`shfmt -d`. Every optional shellcheck check is on, not just the default
severity, because at this size the style rules cost nothing to satisfy and the
opt-in ones are where the bugs were: SC2312 caught `failing-log.sh` gating its
fallback on a pipeline exit status that `head` made zero unconditionally, so
the branch had never once run. Write `${braces}`, `[[ ]]`, and no bare
pipelines whose exit status you mean to test, and it passes first try.

Judge by the contract, not by whether the code looks short: `runlog` read as
three tidy shell scripts and was one four-case taxonomy with two readers that
disagreed twice (a8531b1, f803d2a).

## Where it lives

A helper no single skill owns — two skills call it, or it reads state living in
every skill directory — goes in `bin/` rather than under whichever skill needed
it first.
