# gather contract

What `prioritize`'s SKILL.md reads from `gather.sh`: the whole digest, the
abort on gh output it cannot decode, and the exit codes. `gh` is the stub in
`lib/gh-stub.sh`, answering only the argv each scenario's `calls` lists.

```scrut
$ mkdir stub && ln -s "$TESTDIR/../../../../lib/gh-stub.sh" stub/gh && export PATH="$PWD/stub:$PATH"
```

## The digest

Bot PRs drop out unless failing, a running check is not a failure, an
empty-string conclusion reads as in-progress, alerts from every page are
counted by severity, and a disabled feature is a value rather than an error.

```scrut
$ GH_STUB="$TESTDIR/digest" "$TESTDIR/../gather.sh"
REPO            PR  ISSUE  CI           ALERTS                    PUSHED
me-user/alpha    4      3  in-progress  critical:1 high:2 low:1   2026-11-01
me-user/beta     0      0  no-branch    disabled                  2026-10-02

ATTENTION (everything else is cluster-line material)
review other-org/gamma#5  colleague       REVIEW-REQUESTED      2026-11-03  Please review
issue  me-user/alpha#22   stranger        MINE-LAST 2 replies   2026-08-01  I answered last
issue  me-user/alpha#21   stranger        UNANSWERED 0 replies  2026-09-01  Nobody answered this
pr     me-user/alpha#13   contributor     human                 2026-10-15  A human change whose title is long enough that the digest has t…
pr     me-user/alpha#12   app/dependabot  FAILING-CI            2026-10-21  Bump red
```

## A field gh no longer sends aborts the run

Nothing reaches stdout: a partial digest reads as a clean one.

```scrut
$ GH_STUB="$TESTDIR/missing-field" "$TESTDIR/../gather.sh" 2>stderr
[1]
```

```scrut
$ cat stderr
gather: gh repo list --no-archived --source --limit 200 --json name,owner,pushedAt,defaultBranchRef returned JSON this script cannot read (JSON deserialization for type 'Wire+Repo' was missing required properties including: 'pushedAt'.); the fields it asks for have probably changed
```
