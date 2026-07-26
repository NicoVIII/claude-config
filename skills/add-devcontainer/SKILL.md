---
name: add-devcontainer
description: Add a devcontainer to a repository so its toolchain is pinned in exactly one place and CI runs inside it — inventory what the repo needs, map each tool to a devcontainer feature by maintainership, pin exact versions, prove the built image honors them, then point CI at the container. Use when I ask to add or set up a devcontainer, containerize a repo's dev environment, pin a project's toolchain, make CI and my machine use the same tool versions, or stop CI and local builds drifting apart.
---

Give a repo a devcontainer that *is* its toolchain definition: every version pinned in `devcontainer.json`, and CI running the repo's own check suite inside that container, so no second install path exists to drift from it.

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

Check my own set (3) before falling to devcontainers-extra — it is small and easy to forget:

```sh
curl -fsSL https://api.github.com/repos/NicoVIII/devcontainer-features/contents/src | jq -r '.[] | select(.type=="dir") | .name'
```

Two checks per candidate feature, both of which have bitten:

**Is it actually published?** A feature directory on GitHub is not an OCI artifact. Verify before writing it into the config, or the build fails late:

```sh
ghcr_tags() {
	local repo=$1 tok
	tok=$(curl -fsSL "https://ghcr.io/token?scope=repository:${repo}:pull&service=ghcr.io" | jq -r .token)
	printf '%-58s ' "${repo}"
	curl -fsSL -H "Authorization: Bearer ${tok}" "https://ghcr.io/v2/${repo}/tags/list" | jq -rc '.tags' || echo "NOT PUBLISHED"
}
ghcr_tags devcontainers-extra/features/shellcheck
```

**Can it pin a version?** Read the manifest's `options`. Some features expose none at all — `anthropics/devcontainer-features/claude-code` is `options: {}`, so it always installs latest and cannot be pinned:

```sh
curl -fsSL https://raw.githubusercontent.com/<owner>/<repo>/main/src/<feature>/devcontainer-feature.json | jq -c '{id,version,options}'
```

Do not trust the `proposals` list to tell you what a version option accepts — devcontainers-extra features advertise only `["latest"]` and still honored exact versions (`shellcheck` 0.11.0, `shfmt` 3.13.1, `ripgrep` 14.1.1). Test rather than infer.

When a needed tool can only be pinned by dropping to a lower-priority source, surface the conflict instead of deciding alone: the priority order and exact pinning genuinely conflict, and which one yields is the user's call.

## Write the config

`devcontainer.json` with `image` plus `features` — no Dockerfile, unless a tool reached rung 5 above. Base image tags cannot be listed from MCR's manifest endpoint (it 404s without auth); use the tag list:

```sh
curl -fsSL "https://mcr.microsoft.com/v2/devcontainers/base/tags/list" | jq -r '.tags[]' | grep debian
```

Group the features by provenance with a comment per group, so the next reader can see the priority order was applied rather than guess. State in a comment that versions live here and nowhere else.

`postCreateCommand` needs two things:

```json
"postCreateCommand": "git config --global --add safe.directory ${containerWorkspaceFolder} && <hook install>"
```

`safe.directory` is not optional. The workspace bind mount keeps the host's ownership, so wherever the container user's uid differs from the owner's — which is the normal case on a CI runner — git refuses the checkout with "detected dubious ownership" and every recipe that starts with `git ls-files` fails. Append the repo's hook installer (`lefthook install`, `pre-commit install`) only if it is idempotent.

Make it committable:

- If the repo's `.gitignore` is an allowlist, `.devcontainer/` is a new top-level path and needs an explicit un-ignore entry, or the whole directory stays invisible. Check with `git status --porcelain` and `git ls-files .devcontainer` — not by eye.
- Track `devcontainer-lock.json`. Building generates it, and it pins each feature *implementation* by digest, so a feature republishing `:1` cannot silently change how a tool gets installed. Commit the refreshed lock whenever a feature changes.

## Prove it, do not assume it

Check free disk before building — images run to gigabytes, and exhaustion surfaces far from its cause (a full disk showed up as `just` failing to create a temp dir with "No space left on device", which reads like a recipe bug):

```sh
df -h /; docker system df
```

Then build and verify the versions actually landed. A feature accepting a version option is not evidence it honored it:

```sh
devcontainer build --workspace-folder . --image-name <name>:verify
docker run --rm <name>:verify bash -lc 'dotnet --version; just --version; shellcheck --version | awk "/^version:/{print \$2}"'
```

Report it as a pinned-vs-actual table. Any mismatch is a finding, not a rounding error.

Finally run the repo's real check suite inside the container, which is the only test that matters:

```sh
devcontainer up --workspace-folder .
devcontainer exec --workspace-folder . bash -lc '<the repo check command>'
```

Clean up afterwards: remove the images and containers *you* created, and nothing else — a dev machine's other images and volumes are not yours to prune. There is no `devcontainer down`; stop it with `docker rm -f <containerId>` using the id `devcontainer up` printed.

If a feature install fails with a network timeout — devcontainers-extra features use `nanolayer`, which reaches ghcr.io mid-build — retry once before redesigning anything; one such failure here was transient. If it repeats, curl the ghcr.io token endpoint from inside a container to tell a real outage apart from a broken feature.

## Point CI at the container

Preferred, and what makes the sync structural rather than clerical:

```yaml
- uses: actions/checkout@v7
- uses: devcontainers/ci@v0.3
  with:
    runCmd: <the repo check command>
```

Then delete the now-redundant `setup-*` steps and any hand-rolled tool downloads — leaving them is what recreates the second source of truth this skill exists to remove.

**Untested fallback.** If the repo's CI is a build matrix, or the image build would dominate a short run, rebuilding the container per job may cost more than it is worth. Then keep native runners and read the versions out of `devcontainer.json` instead, so they still live in one file:

```sh
jq -r '.features["ghcr.io/devcontainers/features/dotnet:2"].version' .devcontainer/devcontainer.json
```

This branch has never been exercised — only the preferred one has. Say so when using it.

Two dead ends, so they are not rediscovered: `build.cacheFrom` is valid only with a Dockerfile, so it cannot speed up an `image`-plus-`features` config; and consuming a prebuilt image means `"image": "ghcr.io/…"` with the `features` block removed (its config rides in the `devcontainer.metadata` label), which moves the versions into a separate build config and destroys the single-source-of-truth property. Do not take either without asking.

## Then document it

Following the target repo's own conventions, record two things: that versions are pinned in `devcontainer.json` and bumped there rather than in the workflow, and the feature priority order, for whoever adds the next tool. If the container shares host credentials or config read-write, say so wherever someone decides whether to open it — that is a convenience, not a sandbox.

## Stop and report instead of continuing when

- A tool has no feature at any rung and a Dockerfile would be needed — that is a design change, so confirm it.
- The priority order and exact pinning conflict, as they do for an unpinnable feature.
- The build fails twice for the same reason after a retry.
- Verification shows a version mismatch you cannot explain.

Verified in the session that produced this skill: the local build, the pinned-vs-actual table (nine tools), the in-container check suite, the `safe.directory` failure, and the transient nanolayer timeout. **Never verified: the `devcontainers/ci` workflow actually running on GitHub** — only its local equivalent. Treat the first real CI run as part of the job.

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
