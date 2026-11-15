# Contributing

What it takes to change this config: the tools, the checks, how a helper is
tested, and how a change lands. Using the skills needs none of it; the
[README](README.md) covers that.

## Setup

Clone with the submodule, as the README's Setup section does:

```sh
git clone --recurse-submodules https://github.com/NicoVIII/claude-config.git ~/.claude
```

Then either open the repo in its
[devcontainer](.devcontainer/devcontainer.json), which holds every tool below
at a pinned version and runs `lefthook install` when it is created, or install
them yourself:

- the [.NET SDK](https://dotnet.microsoft.com/download) 10 or newer — the
  helpers and their test suites are F# projects
- [`just`](https://just.systems), the task runner
- [`lefthook`](https://lefthook.dev), which runs the pre-commit hook
- [`shellcheck`](https://www.shellcheck.net) and
  [`shfmt`](https://github.com/mvdan/sh)
- [`scrut`](https://github.com/facebookincubator/scrut), x86_64 only for now

and run `lefthook install` once per clone. `just check` should then pass.

The devcontainer bind-mounts the host's `~/.claude` read-write, so Claude Code
inside it shares your skills, memory **and login**: convenient for working on
this config, not a sandbox.

## Checks

`just check` is the whole suite:

| Recipe | What it runs |
| --- | --- |
| `just typecheck` | `dotnet build` on every tracked `*.fsproj` |
| `just test` | the Expecto suites |
| `just contract` | the Scrut suites |
| `just shell` | `shellcheck -o all -S style` and `shfmt -d` on every tracked `*.sh` |

- **The pre-commit hook runs only the recipes the staged paths call for.**
  [`lefthook.yml`](lefthook.yml) has the globs.
- **CI runs `just check` on every push to `main` and every pull request, inside
  the devcontainer.** So [`devcontainer.json`](.devcontainer/devcontainer.json)
  is the one place a tool version is stated. Only Claude Code itself tracks
  latest there: its feature has no version option.
- **`typecheck` and `shell` take their file lists from `git ls-files`,** not
  from a glob, which would walk everything else `~/.claude` holds. Stage a new
  project or script before running `just check`, which otherwise passes
  without having seen it.
- **`.claude/settings.json` is committed** and applies only to Claude Code
  sessions started in this repo: it allows `just check` and the recipes it is
  made of, so the checks do not prompt.

The tooling follows [`standards/repo-setup.md`](standards/repo-setup.md). All
of CI running in the devcontainer is that standard's exception, taken because
`shfmt` and `scrut` have no setup action that takes a version.

## How a helper is tested

Tests follow [`standards/testing.md`](standards/testing.md). The helpers are
thin layers over files and processes, that standard's mostly-shell case: a
helper is owed a suite that drives the built program, and a unit test only
where its logic has earned one, as the history log's parser in `skill-refiner`
has.

The seam depends on what the helper reaches:

| The helper reaches | Seam | Suites |
| --- | --- | --- |
| files and git | a throwaway root | `bin/skill-refiner.tests`, whose `Harness.fs` drives the built binary; `bin/claude-md-growth.contract`, whose script takes the config directory as its argument |
| `gh` | `PATH`, with [`lib/cli-stub.sh`](lib/cli-stub.sh) symlinked as `gh` | the `gather`, `survey` and `failing-log` contract suites |
| HTTP | the `HttpMessageHandler`, taken as an argument | `feature-probe.tests` |

- **A contract suite is a `<tool>.contract/` directory beside the helper,** and
  runs it through the wrapper a skill would name. The stub answers only the
  calls a scenario's `calls` file lists, so a changed flag fails too. It runs
  no `--jq` filter: a fixture holds what `gh` would have printed after it.
- **Nothing gh-facing is covered in-process.** `lib/gh`'s `run` hardcodes
  `Process.Start "gh"` with no seam to fake, so `survey.tests` reaches only
  `Bumps`, the one pure module.
- **A helper that only needs HTTP fakes the handler and does not shell out to
  `curl` to reach the stub.** The fake answers only the requests a test lists,
  so a changed URL or a dropped header fails the same way. The price is that
  `Program.fs`, kept to the printing, goes unexercised.
- **`skill-refiner` has a contract suite too,** for the output the skills quote
  whole. Its wrapper's binary sits in the real config tree, so the suite owns
  only the `.claude/skills` of a throwaway session repo: a skill name its
  fixture lacks falls through to the real `skills/`, where a `log` would append
  to a real `HISTORY.md`.
- **`dependabot-schema.tests` covers only `Check`,** fed YAML text and a
  hand-built `Constraints`. `Schema.fetch` downloads in-process without the
  handler seam, so the fetch, the schema's reduction to `Constraints` and
  `Program.fs` go unexercised, and no suite drives the built program.

## Adding a helper

Read [`references/helpers.md`](references/helpers.md) first: when a skill
should ship one, why it is an F# project, where it lives, how a skill names it.
Then three registrations no check reminds you of:

- **A new suite is added to `just test` or `just contract` by hand.** Both
  name their suites one by one.
- **An F# project deeper than `bin/*`, `lib/*` or `skills/*/scripts/*` needs its
  level listed in [`.github/dependabot.yml`](.github/dependabot.yml),** or no
  NuGet bump reaches it.
- **A new top-level path needs its own `!/…` entry in `.gitignore`,** which is
  an allowlist, and so does a new file in `.claude/`. Anything beneath an
  already-allowed directory such as `skills/` or `bin/` is tracked without one.

## How a change lands

- **The commit message carries the why,** by
  [`standards/commit-messages.md`](standards/commit-messages.md).
- **Docs move in the same commit.** Which file a fact belongs in is laid out
  in [`.claude/CLAUDE.md`](.claude/CLAUDE.md), which is written for agents and
  holds for everyone.
- **A skill is written and edited by
  [`references/skill-conventions.md`](references/skill-conventions.md),**
  through the skills the README lists under Skill lifecycle.
