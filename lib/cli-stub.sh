#!/usr/bin/env bash
# Stands in for an external command on PATH in the contract tests: symlinked
# under the command's name, it looks its call up in ${CLI_STUB}/calls: one call
# per line, the command and its argv joined by single spaces, a tab, then the
# fixture to answer with. A fixture named `*.exit<N>` goes to stderr with exit
# N, which is how gh reports a feature switched off for a repo (1).
#
# gh is the only command stubbed today. It stays keyed on its own name rather
# than hardcoding gh because that costs nothing, but a tool that only needs
# HTTP should not shell out to curl to get stubbed here: it fakes the
# HttpMessageHandler in-process instead, as feature-probe does.
#
# A call the list does not name fails loudly rather than answering with
# nothing, so a changed or dropped flag breaks the test: 0492e84's missing
# --paginate would have failed here, where a decode-and-classify test sees it
# not at all.
#
# A call listed more than once answers its lines in order, the last one
# repeating, for the queries a tool asks again until GitHub settles (survey's
# mergeStateStatus). The position is kept in ${CLI_STUB_STATE}, which only such
# a scenario has to set. Tools run their calls concurrently, but never the
# same one twice at once, so one counter per call needs no lock.
set -euo pipefail

stub=${CLI_STUB:?set CLI_STUB to a scenario directory}
call="$(basename "$0") $*"

answers=()
while IFS=$'\t' read -r listed fixture; do
	if [[ ${listed} == "${call}" ]]; then
		answers+=("${fixture}")
	fi
done <"${stub}/calls"

if ((${#answers[@]} == 0)); then
	printf 'cli stub: unexpected call: %s\n' "${call}" >&2
	exit 99
fi

index=0
if ((${#answers[@]} > 1)); then
	state=${CLI_STUB_STATE:?set CLI_STUB_STATE to a writable directory: ${call} has several answers}
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
if [[ ${fixture} =~ \.exit([0-9]+)$ ]]; then
	cat "${stub}/${fixture}" >&2
	exit "${BASH_REMATCH[1]}"
fi
cat "${stub}/${fixture}"
