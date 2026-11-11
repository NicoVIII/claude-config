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
set -euo pipefail

stub=${GH_STUB:?set GH_STUB to a scenario directory}
call="$*"
while IFS=$'\t' read -r argv fixture; do
	[[ ${argv} == "${call}" ]] || continue
	if [[ ${fixture} == *.err ]]; then
		cat "${stub}/${fixture}" >&2
		exit 1
	fi
	cat "${stub}/${fixture}"
	exit 0
done <"${stub}/calls"

printf 'gh stub: unexpected call: gh %s\n' "${call}" >&2
exit 99
