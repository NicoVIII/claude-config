---
name: add-dependabot
description: Set up or repair a repo's dependabot.yml. Use when I ask to add or set up Dependabot, enable dependency updates on a repo, watch a project's dependencies, group or cut down Dependabot PR noise, or fix a dependabot.yml that misses an ecosystem.
---

Configure Dependabot version updates for one repository: inventory what the repo actually depends on, write `.github/dependabot.yml`, and validate it — since nothing local will tell you it is wrong.

Scope: the config file only. Triaging and merging the PRs it produces is `merge-dependabot`; verifying one risky bump is `verify-bump`. This skill does not enable security updates or the dependency graph (repository settings, not this file) and does not add `ignore` rules speculatively — add one only for a constraint the user states or the repo demonstrates, and say which it was.

Assume nothing about the repo's language or layout; read it first.

## Why this needs a skill at all

Every failure mode here is silent. A misspelled ecosystem, a glob matching no directory, an ecosystem you never listed — none of them error. They all present as "no PRs appeared," which is indistinguishable from "nothing to update." Budget your care accordingly: the validation step is the point, not a formality.

## Inventory the ecosystems

List the repo's tracked files (`git ls-files`) rather than walking the filesystem — a walk wanders into vendored, ignored, and foreign-checkout directories, and a manifest found there produces a config entry for something the repo does not build.

Map each manifest to an ecosystem and record the **directory holding it** — Dependabot only reads manifests in directories you point it at. Two that are easy to miss because nothing in the source tree looks like a dependency: `.github/workflows/*.yml` → `github-actions` (always at `/`), and `.devcontainer/devcontainer.json` → `devcontainers`.

Confirm every string against the schema's own enum rather than memory — the list changes, and a wrong string is one of the silent failures:

```bash
curl -sSL https://json.schemastore.org/dependabot-2.0.json |
  python3 -c "import json,sys; print(json.load(sys.stdin)['definitions']['package-ecosystem-values']['enum'])"
```

Report the inventory before writing, and name anything you are deliberately leaving out.

## Point each ecosystem at its directories

One `directory: /` when the manifests sit at the root. Otherwise use `directories:` with a glob per level the manifests actually occupy (`/bin/*`, `/packages/*/*`).

**Only `*` is documented; `**` is not.** Do not use `**` — if it fails to expand, that ecosystem is watched not at all, with no error anywhere. Prefer several explicit single-star globs over one clever pattern, and tell the user that a project added at a deeper level later needs its level added here.

## Group by update level, not by ecosystem

Give every ecosystem **two** groups — minor/patch, then major — with `patterns: ["*"]` in both:

```yaml
groups:
  nuget:
    patterns: ["*"]
    update-types: [minor, patch]
  nuget-major:
    patterns: ["*"]
    update-types: [major]
```

Dependabot puts an update in the first group whose patterns *and* `update-types` both match, so identical patterns are disambiguated purely by level.

One catch-all group per ecosystem is fewer PRs on paper and is the wrong trade: `merge-dependabot` classifies a grouped PR at its **worst** member, so a single major would flag the whole batch and leave the safe minors sitting unmerged behind it. The split costs at most one extra PR per ecosystem per interval and keeps the routine half auto-mergeable. Ungrouped (one PR per dependency) is never the answer when the user has asked for fewer PRs.

Default the schedule to `weekly` unless the user says otherwise.

## Say what Dependabot cannot see

Do not let the config imply coverage it does not have. The recurring case: for `devcontainers`, Dependabot reads the Feature *references* (`ghcr.io/…/dotnet:2`), but pinned tool versions live in Feature **options** (`"version": "10.0.110"`) — arbitrary key/values it cannot read as versions. Where those references are pinned to a major tag, that entry fires only on a new Feature major. Include it anyway (it costs nothing and is the only automated watch there), but state plainly that the pinned versions remain a manual bump — especially in a repo where `add-devcontainer` made `devcontainer.json` the single home for every version.

## Validate before committing

There is no local Dependabot linter, so do both:

1. Parse the YAML (`python3 -c "import yaml;yaml.safe_load(open('.github/dependabot.yml'))"`) — catches syntax, nothing semantic.
2. Check every key and enum value against the schema fetched above. Keys live under `definitions`; resolve the `$ref` from `properties.updates.items`.

Neither proves Dependabot accepts the file. Say so, and tell the user where the real verdict appears: the repo's **Insights → Dependency graph → Dependabot** tab lists each config entry with its last-checked time and surfaces parse errors. *(Untested: not exercised in the session this skill came from — confirm the tab's wording before relying on it.)*

Comment the file: which decisions were deliberate, and what is *not* covered. Commit it following the repo's own workflow. The config is code and carries no agent marker.

## Stop and report

- The repo has no manifests Dependabot supports → say so and write nothing.
- A `dependabot.yml` already exists → treat this as an edit; change only what is missing or wrong, and never silently drop an existing `ignore` rule (it encodes a constraint you cannot see).
- The user wants security updates, auto-merge automation, or a private registry → out of scope; report what they would need instead.

*(Untested: the two-group split was designed from `merge-dependabot`'s classification rule, not observed across real PRs — the first repo to run this should check whether the grouped PRs actually arrive split by level.)*

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
