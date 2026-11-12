# failing-log contract

What `prioritize`'s SKILL.md reads from `failing-log.sh`: the filtered head of
one red PR's failing log, and the aborts when there is nothing to show. `gh` is
the stub in `lib/cli-stub.sh`, answering only the argv each scenario's `calls`
lists — with the output gh's own `--jq` would have left, since the stub runs no
filter.

```scrut
$ mkdir stub && ln -s "$TESTDIR/../../../../lib/cli-stub.sh" stub/gh && export PATH="$PWD/stub:$PATH"
```

## A red check

Only the lines naming an error or a failure survive, in log order, without the
timestamp and without the step column where gh could not resolve it. The run id
comes out of the failing check's link.

```scrut
$ CLI_STUB="$TESTDIR/red" "$TESTDIR/../failing-log.sh" me-user/alpha 12
build	src/App.fs(3,5): error FS0039: The value is not defined
test	Run tests	2 tests FAILED
build	##[error]Process completed with exit code 1.
```

## A log that never says error

The step still has to report something, so the raw log stands in.

```scrut
$ CLI_STUB="$TESTDIR/quiet" "$TESTDIR/../failing-log.sh" me-user/alpha 12
deploy	Waiting for the runner
deploy	Publish	The operation was canceled.
```

## A long log is cut at its head

Thirty lines, and the first thirty: the tail of a failed log is teardown.

```scrut
$ CLI_STUB="$TESTDIR/long" "$TESTDIR/../failing-log.sh" me-user/alpha 12 > out; wc -l < out; sed -n '1p;$p' out
30
test	Run tests	case 1 failed
test	Run tests	case 30 failed
```

## No failing check

```scrut
$ CLI_STUB="$TESTDIR/green" "$TESTDIR/../failing-log.sh" me-user/alpha 12 2>stderr
[1]
```

```scrut
$ cat stderr
no failing check with a run link on me-user/alpha#12
```

## A failing check whose link names no run

An external check links to its own service; `gh run view` is never asked.

```scrut
$ CLI_STUB="$TESTDIR/no-run" "$TESTDIR/../failing-log.sh" me-user/alpha 12 2>stderr
[1]
```

```scrut
$ cat stderr
no run id in the failing check's link on me-user/alpha#12: https://ci.example.test/builds/7
```

## gh itself failing

A PR that does not exist is gh's complaint, passed on — not "no failing check",
which would be an answer about a PR that was never looked at.

```scrut
$ CLI_STUB="$TESTDIR/no-pr" "$TESTDIR/../failing-log.sh" me-user/alpha 99 2>stderr
[1]
```

```scrut
$ cat stderr
failing-log: gh pr checks 99 -R me-user/alpha --json bucket,link --jq first(.[] | select(.bucket == "fail") | .link) failed: GraphQL: Could not resolve to a PullRequest with the number of 99. (repository.pullRequest)
```

## Usage

```scrut
$ "$TESTDIR/../failing-log.sh" me-user/alpha 2>stderr
[2]
```

```scrut
$ cat stderr
usage: failing-log.sh <owner/repo> <pr-number>
```
