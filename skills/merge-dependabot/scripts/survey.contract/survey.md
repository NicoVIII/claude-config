# survey contract

What `merge-dependabot`'s SKILL.md reads from `survey.sh`: the merge method,
one labelled block per open Dependabot PR, the empty case, and the aborts.
`gh` is the stub in `lib/gh-stub.sh`, answering only the argv each scenario's
`calls` lists.

```scrut
$ mkdir stub state && ln -s "$TESTDIR/../../../../lib/gh-stub.sh" stub/gh && export PATH="$PWD/stub:$PATH" GH_STUB_STATE="$PWD/state"
```

## The survey

Mergeability is asked again while GitHub answers UNKNOWN (#1 gets UNKNOWN,
then CLEAN), a check still running is pending, a NEUTRAL or SKIPPED one is not
red, a diff touching only lockfiles is a stale refresh, a body with no
dependency line falls back to the title, the body's release links come first
and the dependency's home only without them, and a PR taking the same
dependency further supersedes the other.

```scrut
$ GH_STUB="$TESTDIR/survey" "$TESTDIR/../survey.sh"
merge-method: merge

PR #1  LEVEL=patch  CI=green  MERGE=clean  FILES=manifest
  title: Bump lodash from 4.17.20 to 4.17.21
  bump: lodash 4.17.20 -> 4.17.21 (patch)
  superseded-by: #4

PR #2  LEVEL=major  CI=red  MERGE=blocked  FILES=manifest
  title: Bump the actions group with 2 updates
  bump: actions/checkout 4 -> 5 (major)
  bump: actions/setup-dotnet 4.0.0 -> 4.1.0 (minor)
  failing: check https://example.test/runs/2
  notes: https://github.com/actions/checkout/releases
  notes: https://github.com/actions/setup-dotnet/releases

PR #3  LEVEL=patch  CI=pending  MERGE=dirty  FILES=lock-only(packages.lock.json packages.lock.json)
  title: Bump Expecto from 10.2.1 to 10.2.3
  bump: Expecto 10.2.1 -> 10.2.3 (patch)
  notes: https://github.com/haf/expecto

PR #4  LEVEL=patch  CI=green  MERGE=clean  FILES=manifest
  title: Bump lodash from 4.17.20 to 4.17.23
  bump: lodash 4.17.20 -> 4.17.23 (patch)
```

## No open PRs

Nothing beyond the PR list is asked: the merge method only matters once there
is something to merge.

```scrut
$ GH_STUB="$TESTDIR/none" "$TESTDIR/../survey.sh"
no open dependabot PRs
```

## A repo allowing no merge method aborts

```scrut
$ GH_STUB="$TESTDIR/no-method" "$TESTDIR/../survey.sh" 2>stderr
[1]
```

```scrut
$ cat stderr
survey: this repo allows no merge method; nothing here can be merged
```

## A legacy status check aborts

A StatusContext rollup entry has no `name`; decoding it strictly is the
invariant abd4b0f chose over a converter nothing walks.

```scrut
$ GH_STUB="$TESTDIR/status-context" "$TESTDIR/../survey.sh" 2>stderr
[1]
```

```scrut
$ cat stderr
survey: gh pr list --author app/dependabot --state open --limit 200 --json number,title,statusCheckRollup returned JSON this script cannot read (JSON deserialization for type 'Wire+Check' was missing required properties including: 'name', 'conclusion', 'detailsUrl'.); the fields it asks for have probably changed
```
