# Editing a standard

Read this before changing anything under `~/.claude/standards/`. That folder is a repository of its own, `github.com/NicoVIII/standards`, mounted in `~/.claude` as a git submodule, so an edit there is two commits in two repositories.

1. Put the submodule on its branch. A submodule is often left on a detached HEAD, where a commit belongs to no branch and is lost at the next update:

   ```sh
   pin=$(git -C ~/.claude rev-parse HEAD:standards)
   git -C ~/.claude/standards switch main
   git -C ~/.claude/standards merge --ff-only "$pin"
   ```

   If the merge refuses, `main` and the pin have diverged: stop and say so. If it reports `Already up to date`, `main` may be ahead of the pin — a fresh clone starts it at `origin/main` — and `git -C ~/.claude/standards log --oneline "$pin"..main` lists what the moved pin will carry besides the edit. Name those commits in the `~/.claude` commit of step 3.
2. Edit by the rules in `~/.claude/standards/README.md`, and commit with `git -C ~/.claude/standards`. Markdown only, so there are no checks: review the diff.
3. Stage the moved pin with `git -C ~/.claude add standards`, into the `~/.claude` commit that belongs to the same change — the `CLAUDE.md` line of a new standard, a rule that moved out of `CLAUDE.md`. Where nothing else changed there, the pin is a commit of its own.

A later `git submodule update`, or a pull with `--recurse-submodules`, detaches the HEAD again and leaves `main` where it was, unpushed commits included; step 1 brings it back. `--merge` is not used: on a `main` with an unpushed commit it makes a merge commit in the standards and leaves `standards` in `~/.claude` showing a pin nobody asked for.

Never push either one. Pushing `~/.claude` before the standards would publish a pin nobody else can fetch; `push.recurseSubmodules=check` (README, After cloning) makes that push refuse.

A project that carries the standards at `docs/standards/` is never edited there: that copy is the version the project conforms to. Change the standard in `~/.claude/standards/`; the project takes it by moving its own pin.
