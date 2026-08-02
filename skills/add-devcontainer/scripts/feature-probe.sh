#!/usr/bin/env bash
# Ask the registries what exists, before a feature ref is written into a config.
#
#   feature-probe.sh <feature-ref>...  published tags + pinnable options
#   feature-probe.sh --mine            features in NicoVIII/devcontainer-features
#   feature-probe.sh --base            mcr.microsoft.com/devcontainers/base tags
#
# A feature ref is what devcontainer.json holds, with or without ghcr.io/ and a
# tag: ghcr.io/devcontainers/features/dotnet:2
set -euo pipefail

die() {
	echo "feature-probe: $*" >&2
	exit 1
}

for cmd in curl jq; do
	command -v "${cmd}" >/dev/null || die "needs ${cmd}"
done

# Curl exit 22 is "the server answered with an HTTP error" — for GHCR that means
# no such published artifact, which a feature directory on GitHub is not. Any
# other failure is the network, and must not read as an answer about the feature.
ghcr_curl() {
	local status=0
	curl -fsSL "$@" || status=$?
	case ${status} in
	0) ;;
	22) echo NOT_PUBLISHED ;;
	*) die "ghcr.io unreachable (curl ${status})" ;;
	esac
}

# GHCR hands out a pull token per repository, even for public ones.
ghcr_tags() {
	local repo=$1 token
	token=$(ghcr_curl "https://ghcr.io/token?scope=repository:${repo}:pull&service=ghcr.io")
	[[ ${token} != NOT_PUBLISHED ]] || {
		echo "NOT PUBLISHED — no such artifact on ghcr.io"
		return
	}
	token=$(jq -r .token <<<"${token}")
	ghcr_curl -H "Authorization: Bearer ${token}" "https://ghcr.io/v2/${repo}/tags/list" |
		jq -rc '.tags'
}

# The manifest is the only statement of what the feature can pin. Its
# options.version.proposals understates what is accepted, so treat a missing
# version option as the finding, not a narrow proposals list.
feature_options() {
	local owner=$1 repo=$2 feature=$3 url
	url="https://raw.githubusercontent.com/${owner}/${repo}/main/src/${feature}/devcontainer-feature.json"
	curl -fsSL "${url}" | jq -c '{version, options}' ||
		echo "NO MANIFEST at ${url} — wrong path, or not a src/<feature> layout"
}

probe_feature() {
	local ref=${1#ghcr.io/} owner repo feature
	ref=${ref%:*}
	IFS=/ read -r owner repo feature <<<"${ref}"
	[[ -n ${feature:-} ]] || die "not a feature ref: $1"
	echo "== ${ref}"
	printf 'tags     '
	ghcr_tags "${owner}/${repo}/${feature}"
	printf 'manifest '
	feature_options "${owner}" "${repo}" "${feature}"
}

case ${1:-} in
"")
	die "usage: feature-probe.sh <feature-ref>... | --mine | --base"
	;;
--mine)
	curl -fsSL https://api.github.com/repos/NicoVIII/devcontainer-features/contents/src |
		jq -r '.[] | select(.type == "dir") | "ghcr.io/nicoviii/devcontainer-features/" + .name'
	;;
--base)
	curl -fsSL https://mcr.microsoft.com/v2/devcontainers/base/tags/list | jq -r '.tags[]'
	;;
*)
	for ref in "$@"; do
		probe_feature "${ref}"
	done
	;;
esac
