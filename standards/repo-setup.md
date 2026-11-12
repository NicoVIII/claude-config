# Repository setup

Applies when creating a repository or changing its tooling: task runner, hooks, CI, devcontainer, dependency updates, releases, licence.

## One gate

- **`just` is the task runner, and `just check` runs every check the repository has.**
- **Hooks and CI call `just` recipes and nothing else.** A new check is added to the `justfile`, never to a workflow or to the hook configuration.
- **lefthook runs the pre-commit hook.** It runs only the checks the staged paths need, so a commit that touches no code stays instant.

## Toolchain

- **The devcontainer is the toolchain definition.** Every tool version is pinned in `devcontainer.json`, to the exact patch version, and nowhere else.
- **CI runs `just check` inside the devcontainer.** No workflow installs a tool itself.
- **A tool comes in as a devcontainer feature,** from the first source that has it: the official features, the tool's own maintainers, my own (`ghcr.io/nicoviii/devcontainer-features`), devcontainers-extra, and only then an unknown third party or a manual install.
- **`devcontainer-lock.json` is tracked,** and refreshed in the commit that changes a feature.

## Dependency updates

- **Dependabot watches every ecosystem the repository has,** its GitHub Actions and its devcontainer features included.
- **Each ecosystem gets two groups:** minor and patch updates together, major updates apart. Weekly.
- **A bump lands on green CI only where the test suite exercises what was bumped.** Anything else is verified by hand first.

## Workflows

- **A workflow declares the least `permissions` it needs.**

## Agent instructions

- **Agent instructions live in `AGENTS.md`, at the directory level they apply to.** No `CLAUDE.md` sits beside it.

## Releases

- **`CHANGELOG.md` opens with an `Unreleased` section.** A user-visible change adds its line there in the same commit. Internals, refactors, and dependency bumps add none.
- **The version is declared in exactly one file.**
- **A release is a tag, pushed by hand.** CI checks the tag against the declared version and the changelog section, publishes, and writes the release notes from that section.

## Licence

- **A public repository carries the MIT licence.**

## Not settled — raise the trade-off, leave the call to me

- **CI on native runners.** Where a build matrix or a very short job makes building the container for every job cost more than it gives, the alternative is native runners that read their versions out of `devcontainer.json`. Untried.
- **Pinning actions to commit SHAs** instead of tags. Dependabot maintains either.
- **Where a project's own skills live:** `.claude/skills/`, or `.agents/skills/` with a symlink from it.
- **The directory for a project's decision records:** `docs/decisions/` or `docs/adrs/`.
