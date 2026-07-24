#!/usr/bin/env bash
#
# Deterministic gather pass for the `prioritize` skill.
#
# Everything here is a fixed gh/jq pipeline with no judgement in it, so it runs
# as code rather than as prose the model has to reproduce correctly each time.
# It prints a digest of roughly 2 KB in place of the ~30 KB of raw JSON the
# queries return, keeping the model's context for ranking.
#
# Rows that only feed a count (self-authored issues, green bot PRs) are counted
# here and never printed individually. Rows that need a human judgement call
# land in the ATTENTION block.
#
# Any unexpected gh failure aborts. Under-reporting looks identical to good news
# in this output, so a partial digest is worse than no digest.

set -euo pipefail

# gh's list commands default to --limit 30 and truncate without saying so.
LIMIT=200
PARALLEL=8

# Most of the work happens inside command substitutions, where a plain `exit`
# would only kill the subshell and let the caller carry on with an empty result
# — which then reports as "no open PRs" or trips a bogus limit warning. Signal
# the top-level shell so the first real error is also the last line printed.
trap 'exit 1' TERM
fail() {
	printf 'gather.sh: %s\n' "$*" >&2
	kill -TERM $$ 2>/dev/null
	exit 1
}

# Runs a gh command. A failure whose output contains $tolerated returns 1
# quietly — a per-repo feature being switched off is a fact, not an error.
# Every other failure is fatal.
gh_try() {
	local tolerated=$1
	shift
	local out rc=0
	out=$("$@" 2>&1) || rc=$?
	if [ "$rc" -eq 0 ]; then
		printf '%s' "$out"
		return 0
	fi
	if [ -n "$tolerated" ] && printf '%s' "$out" | grep -qF -- "$tolerated"; then
		return 1
	fi
	fail "$(printf '%s ' "$@")failed (exit $rc): $out"
}

# A count at the limit means the query truncated and the digest is incomplete.
check_truncation() {
	local count=$1 what=$2
	[ "${count:-0}" -lt "$LIMIT" ] || fail "$what returned $count rows at the $LIMIT limit; raise LIMIT"
}

# Open PRs: count them all, surface only those failing CI or written by a human.
# Bot PRs that are green are cluster-line material and never listed.
pr_signal() {
	local repo=$1 json count
	json=$(gh_try '' gh pr list -R "$repo" --limit "$LIMIT" \
		--json number,title,author,isDraft,statusCheckRollup,updatedAt)
	count=$(printf '%s' "$json" | jq 'length')
	check_truncation "$count" "$repo PRs"

	printf '%s' "$json" | jq -r --arg repo "$repo" '
		.[]
		| ([.statusCheckRollup[]?.conclusion] | map(select(. == "FAILURE")) | length) as $fails
		| select($fails > 0 or (.author.is_bot | not))
		| "ATT\tpr    \($repo)#\(.number)\t\(.author.login)\t\(if $fails > 0 then "FAILING-CI" else "human" end)\t\(.updatedAt[0:10])\t\(.title[0:64])"
	'
	printf '%s' "$count"
}

# Open issues. Only issues someone else opened can put me on the hook, and
# whether one is answered needs the comment thread — updatedAt does not show it
# (a label edit bumps that too). That lookup is one call per external issue,
# which is a handful, so it happens here rather than being guessed later.
issue_signal() {
	local repo=$1 json count
	json=$(gh_try 'has disabled issues' gh issue list -R "$repo" --limit "$LIMIT" \
		--json number,title,author,labels,updatedAt) || json='[]'
	count=$(printf '%s' "$json" | jq 'length')
	check_truncation "$count" "$repo issues"

	local n
	for n in $(printf '%s' "$json" | jq -r --arg me "$ME" '.[] | select(.author.login != $me) | .number'); do
		local detail
		detail=$(gh_try '' gh issue view "$n" -R "$repo" --json title,author,createdAt,comments)
		printf '%s' "$detail" | jq -r --arg repo "$repo" --arg n "$n" --arg me "$ME" '
			([.comments[].author.login] | map(select(. == $me)) | length) as $mine
			| "ATT\tissue \($repo)#\($n)\t\(.author.login)\t\(if $mine > 0 then "answered" else "UNANSWERED" end) \(.comments | length) replies\t\(.createdAt[0:10])\t\(.title[0:64])"
		'
	done
	printf '%s' "$count"
}

# Health of the default branch. Resolved per repo because `gh run list --limit 1`
# without a branch returns the newest run on any branch, which reads as healthy
# while main is red.
ci_signal() {
	local repo=$1 branch=$2
	[ -n "$branch" ] && [ "$branch" != "null" ] || {
		printf 'no-branch'
		return
	}
	gh_try '' gh run list -R "$repo" --branch "$branch" --limit 1 \
		--json conclusion --jq '.[0].conclusion // "no-runs"'
}

# Severities, not a bare count: one critical outranks a pile of lows.
# A 403 carrying "Dependabot alerts are disabled" is that repo's setting. gh
# appends a boilerplate hint about admin:repo_hook scope to any 403; it is noise,
# and the other repos in this same run answering with data disproves it.
alert_signal() {
	local repo=$1 out
	out=$(gh_try 'Dependabot alerts are disabled' gh api "repos/$repo/dependabot/alerts?state=open" \
		--jq '[.[].security_advisory.severity] | group_by(.) | map("\(.[0]):\(length)") | join(" ")') || {
		printf 'disabled'
		return
	}
	printf '%s' "${out:--}"
}

# One repo, all four signals. Runs as a child process so the repos can go in
# parallel; every line is tagged so the parent can split table rows from
# attention rows after the interleaving.
emit_one() {
	local repo=$1 branch=$2 pushed=$3
	local prs issues ci alerts attention

	attention=$(pr_signal "$repo") || fail "PR signal failed for $repo"
	prs=${attention##*$'\n'}
	[ "$prs" = "$attention" ] || printf '%s\n' "${attention%$'\n'*}"

	attention=$(issue_signal "$repo") || fail "issue signal failed for $repo"
	issues=${attention##*$'\n'}
	[ "$issues" = "$attention" ] || printf '%s\n' "${attention%$'\n'*}"

	ci=$(ci_signal "$repo" "$branch")
	alerts=$(alert_signal "$repo")

	printf 'ROW\t%s\t%s\t%s\t%s\t%s\t%s\n' "$repo" "$prs" "$issues" "$ci" "$alerts" "${pushed:0:10}"
}

# PRs awaiting my review elsewhere. `gh repo list` only enumerates repos I own,
# so a review request in someone else's or an org's repo never reaches the loop
# above. These are top priority by definition.
review_requests() {
	gh_try '' gh search prs --review-requested=@me --state=open --limit 100 \
		--json number,title,repository,author,isDraft,updatedAt |
		jq -r '.[] | "ATT\treview \(.repository.nameWithOwner)#\(.number)\t\(.author.login)\tREVIEW-REQUESTED\t\(.updatedAt[0:10])\t\(.title[0:64])"'
}

main() {
	local repos
	repos=$(gh_try '' gh repo list --no-archived --source --limit "$LIMIT" \
		--json name,owner,pushedAt,defaultBranchRef \
		--jq '.[] | "\(.owner.login)/\(.name)\t\(.defaultBranchRef.name // "")\t\(.pushedAt)"')
	[ -n "$repos" ] || fail 'gh repo list returned nothing'
	check_truncation "$(printf '%s\n' "$repos" | wc -l)" 'repo list'

	local all
	all=$(
		{
			printf '%s\n' "$repos" |
				xargs -P "$PARALLEL" -I{} bash "$0" --one {}
			review_requests
		} | sort
	) || fail 'a per-repo gather failed; its error is above this line'

	printf 'REPO\tPR\tISSUE\tCI\tALERTS\tPUSHED\n'
	printf '%s\n' "$all" | sed -n 's/^ROW\t//p'
	printf '\nATTENTION (everything else is cluster-line material)\n'
	local att
	att=$(printf '%s\n' "$all" | sed -n 's/^ATT\t//p')
	printf '%s\n' "${att:-none}"
}

# Child mode: one tab-separated "repo<TAB>branch<TAB>pushedAt" record.
if [ "${1:-}" = '--one' ]; then
	IFS=$'\t' read -r repo branch pushed <<<"$2"
	ME=${ME:?child invoked without ME}
	emit_one "$repo" "$branch" "$pushed"
	exit 0
fi

ME=$(gh_try '' gh api user --jq .login)
export ME
main
