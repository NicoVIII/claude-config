#!/usr/bin/env bash
# Stands in for gh on PATH in the contract tests. Each call's argv is looked up
# in ${GH_STUB}/calls: one call per line, the argv joined by single spaces, a
# tab, then the fixture to answer with. A `.err` fixture goes to stderr with
# exit 1, which is how gh reports a feature switched off for a repo.
#
# An argv the list does not name fails loudly rather than answering with
# nothing, so a changed or dropped flag breaks the test: 0492e84's missing
# --paginate would have failed here, where a decode-and-classify test sees it
# not at all.
#
# An argv listed more than once answers its lines in order, the last one
# repeating, for the queries a tool asks again until GitHub settles (survey's
# mergeStateStatus). The position is kept in ${GH_STUB_STATE}, which only such
# a scenario has to set. Tools run their gh calls concurrently, but never the
# same argv twice at once, so one counter per argv needs no lock.
set -euo pipefail

stub=${GH_STUB:?set GH_STUB to a scenario directory}
call="$*"

answers=()
while IFS=$'\t' read -r argv fixture; do
	if [[ ${argv} == "${call}" ]]; then
		answers+=("${fixture}")
	fi
done <"${stub}/calls"

if ((${#answers[@]} == 0)); then
	printf 'gh stub: unexpected call: gh %s\n' "${call}" >&2
	exit 99
fi

index=0
if ((${#answers[@]} > 1)); then
	state=${GH_STUB_STATE:?set GH_STUB_STATE to a writable directory: gh ${call} has several answers}
	key=$(printf '%s' "${call}" | sha1sum)
	counter=${state}/${key%% *}
	if [[ -f ${counter} ]]; then
		index=$(<"${counter}")
	fi
	echo "$((index + 1))" >"${counter}"
	last=$((${#answers[@]} - 1))
	if ((index > last)); then
		index=${last}
	fi
fi

fixture=${answers[index]}
if [[ ${fixture} == *.err ]]; then
	cat "${stub}/${fixture}" >&2
	exit 1
fi
cat "${stub}/${fixture}"
