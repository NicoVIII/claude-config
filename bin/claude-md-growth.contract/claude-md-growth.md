# claude-md-growth contract

What `instructions-retro` and a person read from `claude-md-growth.sh`: one
line saying how far `CLAUDE.md` stands above where the last compaction pass
left it, and whether the next pass is due.

Everything runs against a throwaway config repo passed as the argument, never
the real one. `seq N` writes a file of N words. Only the commit hash varies,
so those lines are matched by pattern.

```scrut
$ export GIT_CONFIG_GLOBAL=/dev/null GIT_CONFIG_SYSTEM=/dev/null GIT_AUTHOR_NAME=t GIT_AUTHOR_EMAIL=t@example.invalid GIT_COMMITTER_NAME=t GIT_COMMITTER_EMAIL=t@example.invalid GROWTH="$TESTDIR/../claude-md-growth.sh" && git init --quiet config && cd config && touch README.md && git add README.md && git commit --quiet -m init
```

## A file nothing has committed

There is no history to read a pass from, and a size alone is not the answer
that was asked for. A repo with no commit at all never gets this far: git
aborts on it by itself.

```scrut
$ seq 100 > CLAUDE.md && "$GROWTH" "$PWD" 2>stderr
[1]
```

```scrut
$ cat stderr
CLAUDE.md has no history in .*/config (regex)
```

## A history without a pass

The commit that creates the file cuts nothing, and neither does growth.

```scrut
$ git add CLAUDE.md && git commit --quiet -m create && seq 160 > CLAUDE.md && git commit --quiet -am grow && "$GROWTH" "$PWD"
CLAUDE.md: 160 words — no compaction pass in its history to measure growth from
```

## Under the trigger

A commit that cuts 20 words or more is a pass, and growth is measured from
what it left.

```scrut
$ seq 130 > CLAUDE.md && git commit --quiet -am cut && seq 140 > CLAUDE.md && git commit --quiet -am grow && "$GROWTH" "$PWD"
CLAUDE.md: 140 words, \+10 since [0-9a-f]+ left it at 130 — under the \+150 trigger (regex)
```

## A small cut is not a pass

Shrinkage that comes with an ordinary edit leaves the baseline where the last
pass put it, and the file can end up below it.

```scrut
$ seq 125 > CLAUDE.md && git commit --quiet -am reword && "$GROWTH" "$PWD"
CLAUDE.md: 125 words, -5 since [0-9a-f]+ left it at 130 — under the \+150 trigger (regex)
```

## Over the trigger

It fires at exactly 150 words of growth, and it counts the working tree: the
edit that crossed the line is usually not committed yet.

```scrut
$ seq 279 > CLAUDE.md && "$GROWTH" "$PWD"
CLAUDE.md: 279 words, \+149 since [0-9a-f]+ left it at 130 — under the \+150 trigger (regex)
```

```scrut
$ seq 280 > CLAUDE.md && "$GROWTH" "$PWD"
CLAUDE.md: 280 words, \+150 since [0-9a-f]+ left it at 130 — over the \+150 trigger — run /claude-md-compact (regex)
```

## The newest pass is the baseline

```scrut
$ git commit --quiet -am grow && seq 200 > CLAUDE.md && git commit --quiet -am cut && "$GROWTH" "$PWD"
CLAUDE.md: 200 words, \+0 since [0-9a-f]+ left it at 200 — under the \+150 trigger (regex)
```
