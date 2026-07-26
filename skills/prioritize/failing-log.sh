#!/usr/bin/env bash
# Prints why one PR's CI is red, for the drill-down in SKILL.md.
#
# The lookup is fixed mechanics — failing check to run id to filtered log — so
# it lives here rather than as prose a model re-derives, and mis-derives, every
# run (eec9963 for that argument in full).
set -euo pipefail

if [[ $# -ne 2 ]]; then
	echo "usage: failing-log.sh <owner/repo> <pr-number>" >&2
	exit 2
fi
repo=$1
pr=$2

# The PR list carries no run id; the failing check's link is where one surfaces.
# gh exits non-zero merely because a check is red, which is the case we are here
# for — an empty link is the only real failure.
link=$(gh pr checks "${pr}" -R "${repo}" --json bucket,link --jq 'first(.[] | select(.bucket == "fail") | .link)' || true)
if [[ -z ${link} ]]; then
	echo "no failing check with a run link on ${repo}#${pr}" >&2
	exit 1
fi

# A link that carries no run id would otherwise reach `gh run view` as an empty
# argument and fail there, reporting the wrong thing.
if [[ ! ${link} =~ /runs/([0-9]+) ]]; then
	echo "no run id in the failing check's link on ${repo}#${pr}: ${link}" >&2
	exit 1
fi
run=${BASH_REMATCH[1]}
log=$(gh run view -R "${repo}" "${run}" --log-failed)

# The log ends in pages of git cleanup, so filter and take the head: tailing it
# shows the teardown instead of the failure. A step that fails without saying
# "error" still has to report something, so fall back to the head of the raw log.
# The filter has to be tested for emptiness rather than by exit status: `head`
# closing the pipeline reports success whether or not grep matched anything.
matched=$(grep -iE 'error|failed' <<<"${log}" || true)
if [[ -z ${matched} ]]; then
	matched=${log}
fi
head -30 <<<"${matched}"
