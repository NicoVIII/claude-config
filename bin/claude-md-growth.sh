#!/usr/bin/env bash
# Reports how far CLAUDE.md has grown since the last compaction pass, and
# whether the next one is due.
#
# Growth, not size: a pass has cut 45 to 85 words whatever the file weighed, so
# no absolute size is reachable by cutting and a fixed limit, once passed, stays
# passed and says nothing.
#
# Git is the only record of a pass, so the baseline is read from it rather than
# kept in a ledger a pass would have to remember to update. That leaves one
# blind spot: a pass that found nothing to cut commits nothing, and the report
# reads the same after it as before.
#
# Shell, where references/helpers.md asks for an F# project, under the exception
# it names: nothing here is parsed and nothing parses the output.
set -euo pipefail

# Words of growth since the last pass that make the next one due. The history
# cannot pick this number, since the yield of a pass does not scale with the
# file; it is a judgement about how often a pass is worth a session.
readonly trigger=150

# A commit that cut at least this many words was a pass. Every pass so far cut
# 45 or more, and shrinkage that came with an ordinary edit stayed at 12 or
# under.
readonly pass_cut=20

config=${1:-${HOME}/.claude}

# Through arithmetic because BSD `wc` pads its count with spaces.
count_words() {
	local count
	count=$(wc -w)
	echo "$((count))"
}

words_at() {
	local text
	text=$(git -C "${config}" show "$1:CLAUDE.md")
	count_words <<<"${text}"
}

# The commit that created the file cut nothing, whatever its parent held.
words_before() {
	if git -C "${config}" cat-file -e "$1^:CLAUDE.md" 2>/dev/null; then
		words_at "$1^"
	else
		echo 0
	fi
}

commits=$(git -C "${config}" log --format=%h -- CLAUDE.md)
if [[ -z ${commits} ]]; then
	echo "CLAUDE.md has no history in ${config}" >&2
	exit 1
fi

pass=
baseline=
while IFS= read -r commit; do
	after=$(words_at "${commit}")
	before=$(words_before "${commit}")
	if ((before - after >= pass_cut)); then
		pass=${commit}
		baseline=${after}
		break
	fi
done <<<"${commits}"

# The working tree, not HEAD: the caller has usually just edited the file and
# not committed it yet.
current=$(count_words <"${config}/CLAUDE.md")

if [[ -z ${pass} ]]; then
	echo "CLAUDE.md: ${current} words — no compaction pass in its history to measure growth from"
	exit 0
fi

growth=$((current - baseline))
report=$(printf 'CLAUDE.md: %d words, %+d since %s left it at %d' "${current}" "${growth}" "${pass}" "${baseline}")
if ((growth >= trigger)); then
	echo "${report} — over the +${trigger} trigger — run /claude-md-compact"
else
	echo "${report} — under the +${trigger} trigger"
fi
