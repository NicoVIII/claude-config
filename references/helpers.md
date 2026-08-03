# Writing a helper for a skill

Read this before adding an executable helper to a skill, or when deciding
whether one should be a shell script or an F# project.

## When a skill should ship one

A skill may ship an executable helper beside its `SKILL.md` when a step is a
fixed tool pipeline with no judgement in it (`prioritize/scripts/gather/`,
eec9963).

The signal to reach for one is SKILL.md prose that exists only to make a model
reproduce exact flags and error strings: that text reloads into context every
run and regresses, a script doesn't.

Keep judgement and anything conditional in the SKILL.md.

## What the helper must guarantee

Make the helper abort loudly. A partial result the reader cannot tell apart
from good news is worse than no result.

## Shell or an F# project

A helper that owns a **contract** — a format it parses, a format with a second
reader, or a closed taxonomy — is an F# project, so that changing the contract
is an error at every site that has to decide about it:

- Name the fields as types. A renamed or dropped one is then a parse error, not
  a null reading downstream as "nothing here".
- Name the cases as a union.
- Set `<WarningsAsErrors>FS0025</WarningsAsErrors>`, or the exhaustiveness is
  only a warning (7e0c921 is what that costs when it is missed).
- Split so each file answers one question, and let the compile order carry the
  dependencies.

Only a helper performing a single mechanical action, parsing nothing, is a
shell script. Length argues one way only: one long enough to need sections is
an F# project whatever it does.

A shell one has to pass `just shell` — `shellcheck -o all -S style` and
`shfmt -d`. Every optional shellcheck check is on, not just the default
severity, because at this size the style rules cost nothing to satisfy and the
opt-in ones are where the bugs were: SC2312 caught `failing-log.sh` gating its
fallback on a pipeline exit status that `head` made zero unconditionally, so
the branch had never once run. Write `${braces}`, `[[ ]]`, and no bare
pipelines whose exit status you mean to test, and it passes first try.

Judge by the contract, not by whether the code looks short: `skill-refiner` read
as three tidy shell scripts and was one four-case taxonomy with two readers that
disagreed twice (a8531b1, f803d2a).

## Where it lives

A helper no single skill owns — two skills call it, or it reads state living in
every skill directory — goes at the repo root rather than under whichever skill
needed it first: in `bin/` if a skill runs it, in `lib/` if it has no entry
point and exists only to be referenced (`lib/gh`). The split is by entry point,
not by who depends on it, so a directory listing answers "what can I run".

One the skill does own goes in `skills/<skill>/scripts/`, the directory the
[Agent Skills spec](https://agentskills.io/specification) reserves for
executable code, so the skill folder stays a self-contained bundle another
agent can read. Its test project is a sibling there rather than left at the
skill root — the spec calls `scripts/` code the agent runs, which a test suite
isn't, but splitting the pair across two levels costs more than the loose
reading. `bin/` and `lib/` are deliberately outside that layout: what they hold
belongs to no skill, so no per-skill directory is the right home for it, and
the spec says nothing about a repo holding many skills.

Adding one under `scripts/` puts a `.fsproj` a level deeper than `skills/*/*` —
the depth `.github/dependabot.yml` has to list, and a glob matching nothing
there fails silently.
