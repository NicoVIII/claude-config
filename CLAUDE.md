# Personal preferences

Personal — never copy them into project repos or force them on contributors. Project-level instructions (AGENTS.md) win where they conflict.

## Interaction

- Ask before assuming and proceeding in a direction that may be wrong; surface ambiguities before starting.
- Be critical and factual; no sycophancy. Push back when something is wrong or questionable, regardless of who said it. If something is unclear or uncertain, say so directly.
- Keep output lean: cut filler and ceremonial phrasing, don't recap what a diff or earlier message already shows, and quote the decisive lines of errors instead of full dumps. Trim wording, never content — caveats and stated uncertainty always survive the trim.
- Anything written on my behalf where others read it — issues, PR/review comments, discussions — ends with a short agent marker, e.g. "🤖 Written by code assistant". Commits are covered by Co-Authored-By; code never gets a marker.

## Commits

- Commit proactively after each independently meaningful change — don't wait to be asked. The full check suite must pass before every commit.
- When a follow-up refines the change just committed, amend that commit instead of stacking a new one — but only commits made this session, and only if `git branch -r --contains HEAD` is empty. A distinct capability or unrelated concern gets its own commit even in the same files; ambiguous mid-iteration cases default to amend. Adding paragraphs to a just-committed artifact is iteration, not a new concern.
- Before committing in a repo you don't know, infer the workflow from history rather than defaulting to a branch. Ask only when the signal is genuinely mixed.
- Commit messages explain WHY — reasoning, trade-offs, non-obvious constraints — not WHAT; the diff carries the what. Never conventional commits (`feat:`, `fix:`, `chore(scope):`) — a history full of them isn't consent, only an explicit project instruction is. A plain `area:` prefix is not a conventional commit.
- Never push, and don't ask about pushing.
- Landing a PR: squash when it has one commit or only throwaway messages (bot text, fixups); otherwise rebase-merge so the commits survive. Don't infer the method from how past PRs landed.
- A fresh git worktree lacks gitignored build inputs (dependencies, generated code): provision it the way the repo's setup does before building, or missing modules read as breakage.

## Issues

- Finishing work that a tracked issue describes includes closing it — comment referencing the commit(s) and what was verified, don't leave it for me to notice and close. A partial fix says explicitly what's left and stays open.

## Code style

- Before writing new code, exhaust reuse in this order: existing helper/pattern in the codebase → stdlib → native platform feature → already-installed dependency. Only then write it — and keep it minimal.
- Bug fixes target the root cause, not the reported symptom: check the other callers of the function you're touching — one fix in the shared function beats a guard per caller.
- Prefer small, composable, single-purpose functions. A `// this block does X` comment is a trigger to extract a named function instead. Skip extraction only when it would reduce clarity: helpers needing many threaded parameters, or one-shot blocks that add pure indirection.
- Order files top-to-bottom F#-style: every definition references only things defined above it. Exceptions only for circular dependencies.
- Comments explain WHY, not WHAT — if a reader could infer it from types and names, cut it. Carve-outs where prose is warranted: doc comments on public APIs; type-lossy seams where the signature can't express the contract; short orientation labels in long functions.

## Documentation

- Every change that alters how something is used, placed, or behaves gets a docs pass before its commit: find the docs describing the touched area — READMEs (humans: how to run/operate it), AGENTS.md (agents: at the appropriate directory level), skills — and update each whose reader would now be wrong or missing something. A new user-facing option belongs in the README even when an agent-facing doc already covers it.
- An AGENTS.md is in context only where a CLAUDE.md imports it — verify the import exists before putting anything there. Where imported, include only what can't be inferred from the code.
- Docs people read — READMEs, release notes, feature descriptions — earn their length. Lead with what the reader has to act on, state each constraint once, and cut background they did not ask for. Prefer trimming to a note that the long version lives elsewhere.
