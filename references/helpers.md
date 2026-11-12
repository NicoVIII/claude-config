# Writing a helper for a skill

Read this before adding an executable helper to a skill.

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

## An F# project, always

A helper is an F# project. There is no "small enough for shell" case: that
call was made three times and was wrong each time — `skill-refiner` read as
three tidy shell scripts and was one four-case taxonomy with two readers that
disagreed twice (a8531b1, f803d2a), `feature-probe.sh` carried a registry's
answer as an in-band string (ac92d34), and `failing-log.sh` shipped a fallback
branch that had never once run (7368d95).

- Name the fields as types. A renamed or dropped one is then a parse error, not
  a null reading downstream as "nothing here".
- Name the cases as a union.
- Set `<WarningsAsErrors>FS0025</WarningsAsErrors>`, or the exhaustiveness is
  only a warning (7e0c921 is what that costs when it is missed).
- Split so each file answers one question, and let the compile order carry the
  dependencies.
- Call `gh` through [`lib/gh`](../lib/gh/Gh.fs), which aborts on everything it
  was not told to expect. Make HTTP requests in-process, with the
  `HttpMessageHandler` passed in so a test can fake it; don't shell out to
  `curl`.

Shell is only what cannot be F#: the wrapper a skill names, `bin/dotnet-tool.sh`
that builds and launches the tools, and the stub the contract suites put on
`PATH`. It has to pass `just shell` — `shellcheck -o all -S style` and
`shfmt -d`. Every optional shellcheck check is on because the opt-in ones are
where the bugs were: SC2312 is what caught `failing-log.sh` gating its fallback
on a pipeline exit status that `head` made zero unconditionally. Write
`${braces}`, `[[ ]]`, and no bare pipelines whose exit status you mean to test,
and it passes first try.

## How a skill names it

A SKILL.md names a wrapper path, never the runtime behind it — an F# project is
reached through a `<tool>.sh` beside it that delegates to
[`bin/dotnet-tool.sh`](../bin/dotnet-tool.sh). That keeps the invocation one
path when a tool moves, grows or is rewritten in another language, instead of a
command repeated across skill prose. `dotnet run --project` is wrong regardless:
it builds in the caller's cwd, and the IncrementalClean that follows breaks the
*next* run from anywhere else (75bd76e).

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
