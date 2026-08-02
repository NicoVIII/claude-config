---
name: upgrade-toolchain
description: Upgrade a hand-pinned toolchain version — compiler, language SDK, runtime, or CLI — across every place a repo pins it, then prove the new version actually builds. Use when I ask to upgrade or bump a language/toolchain version, update a compiler or SDK, move a repo onto a newer runtime, or say a repo's tool version is out of date.
---

Move one tool's pinned version across every site that pins it, in one commit, verified.

Scope: one repo, one tool. Not library/package bumps — `merge-dependabot` sweeps those, `verify-bump` lands the risky ones. Not initial devcontainer setup — that's `add-devcontainer`. Never pushes.

## Find every pin

Search for the version string, not the tool name:

```sh
CURRENT=1.17.0
grep -rnF "$CURRENT" . --exclude-dir={.git,node_modules,build,dist,target,.venv}
```

Sort the hits:

- **Pins to edit** — anything deciding which toolchain something runs: devcontainer feature `version`, CI `env:` vars, Dockerfile `ARG`/`FROM`. Badges and docs snippets too (anticipated, not yet observed in a real run).
- **Vendored/submodule paths** — report, never edit.
- **Incidental** — changelogs, lockfiles, fixtures. Leave.

Formats differ per site (`1.18.1` vs `v1.18.1`). Edit each by hand; never blind-`sed`.

## Confirm the target exists

Pin sites resolve different artifacts that don't all ship together. Verify each by pulling:

```sh
docker pull ghcr.io/<org>/<image>:v<TARGET>-<variant>
```

Don't bother with `gh api /orgs/<org>/packages/container/<pkg>/versions` (403 without `read:packages`) or `docker manifest inspect` (reports failure for images that pull fine).

Stop and report if an artifact is missing for the target. *(Untested — all existed on the originating run.)*

## Check the changelog

```sh
gh api repos/<owner>/<repo>/releases/latest --jq .tag_name   # if no target given
```

Release bodies are often just a link, so read `https://raw.githubusercontent.com/<owner>/<repo>/<tag>/CHANGELOG.md` directly.

Report **breaking changes**, **deprecations**, and **formatter/codegen changes** before editing.

Stop if a breaking change requires editing project code — report and let me decide. *(Untested — the originating run's deprecation touched no project code.)*

## Rebuild, don't sideload

Ask me to rebuild the devcontainer, then wait. Never download a tarball or unpack a binary to verify faster.

Confirm it took: `<tool> --version`.

## Verify

- Run the repo's check suite (`just check`, `make check`, whatever it defines).
- Check whether that suite runs tests — often it doesn't. Run the test recipe separately.
- Formatter churn is expected fallout: apply it, keep it in the same commit, name the files touched.
- `docker build` if the repo ships an image — nothing else exercises a Dockerfile pin.

## Commit

One commit: every pin site plus formatter churn. If a missed pin surfaces after the commit was pushed, follow-up commit, not amend.

Don't propose consolidating pin sites away — a repo shipping an image needs at least two by design.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
