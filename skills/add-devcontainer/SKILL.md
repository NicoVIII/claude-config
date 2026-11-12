---
name: add-devcontainer
description: Add a devcontainer to a repository so its toolchain is pinned in exactly one place and CI takes its versions from it. Use when I ask to add or set up a devcontainer, containerize a repo's dev environment, pin a project's toolchain, make CI and my machine use the same tool versions, or stop CI and local builds drifting apart.
disable-model-invocation: true
---

Give a repo a devcontainer that *is* its toolchain definition: every version pinned in `devcontainer.json`, and CI taking its versions from that file, so no second statement of a version exists to drift from it.

Scope: one repository. This skill sets up the container and points CI at it. It does not wire up Dependabot (`add-dependabot` does, and is worth running after this one), publish prebuilt images to a registry, or tune CI caching — stop and report if the user wants those.

## Pin to what the machine already runs

Read what the repo asks for before choosing anything: its README setup section, its CI workflow, and any `AGENTS.md`. That list plus the repo's build tooling is the tool inventory.

Get the pin baseline by running each tool's `--version` locally rather than taking upstream "latest": the point is a container that matches what already works.

Pin exact patch versions (`10.0.110`, not `10.0`). A floating band means the container and CI resolve it at different moments and stop being the same toolchain, which is the whole problem being solved. Say so if the user would rather float a band — it is their maintenance/reproducibility trade, not yours.

## Map each tool to a feature, by maintainership

Prefer a feature over a manual install. Pick the source in this order, and stop at the first hit:

1. Official — `ghcr.io/devcontainers/features/*`
2. The tool's own maintainers (e.g. `ghcr.io/anthropics/devcontainer-features/claude-code`)
3. Mine — <https://github.com/NicoVIII/devcontainer-features/tree/main/src>
4. `ghcr.io/devcontainers-extra/features/*`
5. Tie between an unknown third-party feature and a manual install in a Dockerfile

`~/.claude/skills/add-devcontainer/scripts/feature-probe.sh <feature-ref>...` answers the two questions per candidate that have bitten — is it published as an OCI artifact, and can it pin a version. `--mine` lists my set (3), which is small and easy to forget to check before falling to devcontainers-extra; `--base` lists the newest exact-version base image tag per distro.

Read the `options` it prints. Some features expose none at all — `anthropics/devcontainer-features/claude-code` is `options: {}`, so it always installs latest and cannot be pinned. Where a version option exists, its `proposals` understates what it accepts: devcontainers-extra features advertise only `["latest"]` and still honored exact versions (`shellcheck` 0.11.0, `shfmt` 3.13.1, `ripgrep` 14.1.1). Test rather than infer.

When a needed tool can only be pinned by dropping to a lower-priority source, surface the conflict instead of deciding alone: the priority order and exact pinning genuinely conflict, and which one yields is the user's call.

## Write the config

`devcontainer.json` with `image` plus `features` — no Dockerfile, unless a tool reached rung 5 above; that is a design change, so confirm it first. Group the features by provenance with a comment per group, so the next reader can see the priority order was applied rather than guess. State in a comment that versions live here and nowhere else.

`postCreateCommand` needs two things:

```json
"postCreateCommand": "git config --global --add safe.directory ${containerWorkspaceFolder} && <hook install>"
```

`safe.directory` is not optional — without it git refuses the bind-mounted checkout as dubious ownership wherever the container user's uid differs from the host owner's, which is the normal case on a CI runner. Append the repo's hook installer (`lefthook install`, `pre-commit install`) only if it is idempotent.

Make it committable:

- If the repo's `.gitignore` is an allowlist, `.devcontainer/` is a new top-level path and needs an explicit un-ignore entry, or the whole directory stays invisible. Check with `git status --porcelain` and `git ls-files .devcontainer` — not by eye.
- Track `devcontainer-lock.json`. Building generates it, and it pins each feature *implementation* by digest, so a feature republishing `:1` cannot silently change how a tool gets installed. Commit the refreshed lock whenever a feature changes.

## Prove it, do not assume it

Check free disk before building — images run to gigabytes, and exhaustion surfaces far from its cause, as a build-tool error rather than a disk one:

```sh
df -h /; docker system df
```

Then build and verify the versions actually landed. A feature accepting a version option is not evidence it honored it:

```sh
devcontainer build --workspace-folder . --image-name <name>:verify
docker run --rm <name>:verify bash -lc 'dotnet --version; just --version; shellcheck --version | awk "/^version:/{print \$2}"'
```

Report it as a pinned-vs-actual table. A mismatch is a finding, not a rounding error; one you cannot explain stops the run.

Finally run the repo's real check suite inside the container, which is the only test that matters:

```sh
devcontainer up --workspace-folder .
devcontainer exec --workspace-folder . bash -lc '<the repo check command>'
```

Clean up afterwards: remove the images and containers you created, and nothing else. There is no `devcontainer down`; stop it with `docker rm -f <containerId>` using the id `devcontainer up` printed.

A feature install can fail on a network timeout without the feature being broken — devcontainers-extra ones use `nanolayer`, which reaches ghcr.io mid-build. A second failure for the same reason is the signal to stop and report.

## Point CI at the pinned versions

No workflow states a tool version of its own: CI reads them from `devcontainer.json`, in one of two shapes. Say which you chose and why.

**Native jobs plus a container job — the default.** *(Untested — no repo runs this shape yet. The first run is its test: report what it took.)* Each job installs its tools with setup actions and passes them the versions read from `devcontainer.json`. One further job runs the check suite inside the container, triggered by changes under `.devcontainer/` and by a weekly schedule. It is what verifies a devcontainer update, and what notices an unpinned part breaking without a commit:

```yaml
- uses: actions/checkout@v7
- uses: devcontainers/ci@v0.3
  with:
    runCmd: <the repo check command>
```

Reading the versions is the unsolved part. The file is JSONC, and `jq` fails on the comments this skill has you write (`parse error: Invalid numeric literal`). `devcontainer read-configuration --workspace-folder .` prints the parsed configuration as plain JSON and is the candidate; it has not been run for this. Don't strip the comments with a pattern.

**Everything inside the container.** Take this shape when a native install would be hand-rolled, which means a tool with no setup action that accepts a version (`shfmt`, `scrut`): downloading tools by hand in a workflow is the second install path this skill removes. The check job is then only the container step above, on every push and pull request. This shape is proven in two repos.

Either way, delete every setup step that states its own version and any hand-rolled tool download. Treat the first real CI run as part of the job.

## Then document it

Following the target repo's own conventions, record two things: that versions are pinned in `devcontainer.json` and bumped there rather than in the workflow, and the feature priority order, for whoever adds the next tool. If the container shares host credentials or config read-write, say so wherever someone decides whether to open it — that is a convenience, not a sandbox.

This skill is not yet battle-tested. At the end of the run, say briefly whether any instruction above was ambiguous, wrong, or needed a workaround. If none was and I corrected nothing during the run, log it instead: from the session repo's working directory run `~/.claude/bin/skill-refiner.sh <this skill's name> log run`, then commit the `HISTORY.md` line it names, alone, in the repo that holds the skill. Where that script does not exist, skip the logging.
