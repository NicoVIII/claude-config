# feature-probe contract

What `add-devcontainer`'s SKILL.md reads from `feature-probe.sh`: the tags and
pinnable options per feature ref, the two answers for a feature that is not
there, and the two listings. `curl` is the stub in `lib/cli-stub.sh`, answering
only the argv each scenario's `calls` lists; `jq` is the real one.

```scrut
$ mkdir stub && ln -s "$TESTDIR/../../../../lib/cli-stub.sh" stub/curl && export PATH="$PWD/stub:$PATH"
```

## Probing feature refs

A ref is taken with or without `ghcr.io/` and a tag. The manifest is cut down
to its version and options, an empty `options` being the finding that a feature
cannot be pinned. A registry answering with an HTTP error means not published,
whether it refuses the token or grants one and then has no tag list, and a
missing manifest is reported with the URL that was tried; neither stops the
refs after it. curl's own complaint on stderr is left out here.

```scrut
$ CLI_STUB="$TESTDIR/probe" "$TESTDIR/../feature-probe.sh" ghcr.io/devcontainers/features/dotnet:2 acme/features/ghost acme/features/half anthropics/devcontainer-features/claude-code 2>/dev/null
== devcontainers/features/dotnet
tags     ["1","2","2.4","2.4.1","latest"]
manifest {"version":"2.4.1","options":{"version":{"type":"string","proposals":["latest","lts","10.0"],"default":"latest"}}}
== acme/features/ghost
tags     NOT PUBLISHED — no such artifact on ghcr.io
manifest NO MANIFEST at https://raw.githubusercontent.com/acme/features/main/src/ghost/devcontainer-feature.json — wrong path, or not a src/<feature> layout
== acme/features/half
tags     NOT PUBLISHED — no such artifact on ghcr.io
manifest {"version":"0.1.0","options":{}}
== anthropics/devcontainer-features/claude-code
tags     ["1","1.0","latest"]
manifest {"version":"1.0.5","options":{}}
```

## An unreachable registry aborts

Any curl failure but an HTTP error is the network, and must not read as an
answer about the feature.

```scrut
$ CLI_STUB="$TESTDIR/unreachable" "$TESTDIR/../feature-probe.sh" acme/features/ghost 2>stderr
== acme/features/ghost
tags      (no-eol)
[1]
```

```scrut
$ cat stderr
curl: (6) Could not resolve host: ghcr.io
feature-probe: ghcr.io unreachable (curl 6)
```

## My features

Directories under `src/` only.

```scrut
$ CLI_STUB="$TESTDIR/mine" "$TESTDIR/../feature-probe.sh" --mine
ghcr.io/nicoviii/devcontainer-features/scrut
ghcr.io/nicoviii/devcontainer-features/just
```

## Base image tags

The newest exact-version tag per distro, compared as versions (2.1.10 after
2.1.8); floating tags are not candidates.

```scrut
$ CLI_STUB="$TESTDIR/base" "$TESTDIR/../feature-probe.sh" --base
2.1.10-bookworm
2.1.8-trixie
2.1.8-ubuntu24.04
```

## Usage

```scrut
$ "$TESTDIR/../feature-probe.sh" 2>&1
feature-probe: usage: feature-probe.sh <feature-ref>... | --mine | --base
[1]
```

```scrut
$ "$TESTDIR/../feature-probe.sh" dotnet 2>&1
feature-probe: not a feature ref: dotnet
[1]
```
