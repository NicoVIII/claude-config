---
name: add-dependabot
description: Set up or repair a repo's dependabot.yml. Use when I ask to add or set up Dependabot, enable dependency updates on a repo, watch a project's dependencies, group or cut down Dependabot PR noise, or fix a dependabot.yml that misses an ecosystem.
---

Configure Dependabot version updates for one repository: inventory what the repo actually depends on, write `.github/dependabot.yml`, and validate it — since nothing local will tell you it is wrong.

Scope: the config file only. Triaging and merging the PRs it produces is `merge-dependabot`; verifying one risky bump is `verify-bump`. This skill does not enable security updates or the dependency graph (repository settings, not this file) and does not add `ignore` rules speculatively — add one only for a constraint the user states or the repo demonstrates, and say which it was.

Assume nothing about the repo's language or layout; read it first.

## Inventory the ecosystems

List the repo's tracked files (`git ls-files`) rather than walking the filesystem, which wanders into vendored, ignored and foreign-checkout directories and yields entries for things the repo does not build.

Map each manifest to an ecosystem and record the **directory holding it** — Dependabot only reads manifests in directories you point it at. Two that are easy to miss because nothing in the source tree looks like a dependency: `.github/workflows/*.yml` → `github-actions` (always at `/`), and `.devcontainer/devcontainer.json` → `devcontainers`.

Confirm every string against the schema's own enum rather than memory — the list changes, and a wrong string fails silently:

```bash
dotnet run --project ~/.claude/skills/add-dependabot/scripts/dependabot-schema -- ecosystems
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

Not one catch-all group per ecosystem, the standard advice: `merge-dependabot` classifies a grouped PR at its **worst** member, so one major would strand every safe minor behind it. Ungrouped (one PR per dependency) is never the answer when the user has asked for fewer PRs.

Default the schedule to `weekly` unless the user says otherwise.

## Say what Dependabot cannot see

Do not let the config imply coverage it does not have. The recurring case: for `devcontainers`, Dependabot reads the Feature *references* (`ghcr.io/…/dotnet:2`) but not the tool versions pinned in Feature **options** (`"version": "10.0.110"`), which are arbitrary key/values. Include the entry anyway — it costs nothing and is the only automated watch there — and state plainly that those pinned versions remain a manual bump.

## Validate before committing

There is no local Dependabot linter, so check the file against the published schema — YAML syntax, every key and ecosystem string, the two requirements hidden in its `allOf` (a `schedule`, and exactly one of `directory`/`directories`), and any `**` glob:

```bash
dotnet run --project ~/.claude/skills/add-dependabot/scripts/dependabot-schema -- check .github/dependabot.yml
```

It exits non-zero having listed its findings, and a clean run still does not prove Dependabot accepts the file. Say so, and tell the user where the real verdict appears: the repo's **Insights → Dependency graph → Dependabot** tab lists each config entry with its last-checked time and surfaces parse errors. *(Untested: not exercised in the session this skill came from — confirm the tab's wording before relying on it.)*

Comment the file: which decisions were deliberate, and what is *not* covered. Commit it following the repo's own workflow. The config is code and carries no agent marker.

## Stop and report

- The repo has no manifests Dependabot supports → say so and write nothing.
- A `dependabot.yml` already exists → treat this as an edit; change only what is missing or wrong, and never silently drop an existing `ignore` rule (it encodes a constraint you cannot see).
- The user wants security updates, auto-merge automation, or a private registry → out of scope; report what they would need instead.

*(Untested: the two-group split was designed from `merge-dependabot`'s classification rule, not observed across real PRs — the first repo to run this should check whether the grouped PRs actually arrive split by level.)*

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
