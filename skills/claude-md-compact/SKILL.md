---
name: claude-md-compact
description: Shrink CLAUDE.md — cut rules the rest of the file already decides, move scoped ones down into a standard, an AGENTS.md or a skill. Use when CLAUDE.md has grown, when I say my global preferences are too long or cost too much, or after a stretch of sessions has been adding rules to it.
---

Reduce what loads into every session in every project, without losing a preference.

## Scope

`~/.claude/CLAUDE.md` only. Removal only — never add a rule in this pass; note what you spot and let a later session add it. `/skill-compact` keeps its passes separate for this reason, and CLAUDE.md has the same failure mode: additions made alongside a removal cancel it.

The pass must never end with **more words in CLAUDE.md than it started**. A rule moved into a standard, an imported AGENTS.md or a SKILL.md counts as removed — those load in fewer sessions. A move that opens a new standard also adds that standard's line to CLAUDE.md, and the line counts against the pass. When no candidate carries evidence, say so and commit nothing: a cut made only to have one is the failure this pass guards against. `~/.claude/bin/claude-md-growth.sh` reads a pass from the commit it left, so after one that committed nothing it still reports the file over its trigger — say that too.

Compaction passes have no run log of their own. Git is the log:

```sh
git -C ~/.claude log --format='%h %s' -- CLAUDE.md
git -C ~/.claude blame -- CLAUDE.md
```

Earlier passes recorded the candidates they declined, and why, in their commit bodies. Before looking for candidates, read the body of every commit in that list whose subject is a cut, trim or move:

```sh
git -C ~/.claude show -s --format=%B <hash>
```

A candidate an earlier pass declined needs evidence that pass did not have.

For the size curve, which shows what past passes achieved:

```sh
git -C ~/.claude log --format=%h -- CLAUDE.md | tac | while read c; do
  printf '%s %4s %s\n' "$c" "$(git -C ~/.claude show $c:CLAUDE.md | wc -w)" \
    "$(git -C ~/.claude log -1 --format=%s $c)"; done
```

Don't build a ledger beside the file — that command reconstructs every baseline.

## Check where a destination loads before proposing a move

An AGENTS.md is in context **only where a CLAUDE.md imports it** — check the import before proposing the move, or the rule lands somewhere nothing reads. A rule that fires in every repo cannot move to one even then: the move reads as deduplication and silently drops the rule everywhere else. When the same idea appears in CLAUDE.md and an AGENTS.md, the redundant copy is usually the narrower one, so the deletion belongs at that end.

Destinations, by what needs the rule:

- **A standard in `~/.claude/standards/`** — the rule holds in any repo, but only for one kind of work. It then reaches a session only when the session reads that standard on the strength of its line in CLAUDE.md, so check the line names the work the rule fires in, and write the rule by `~/.claude/standards/README.md`. Two kinds of rule stay: one that has to hold even when the standard goes unread — a prohibition whose breach is hard to undo — and one whose `blame` commit records it being added or reworded after a misfire. *(Untested — one standard exists and its line has been seen to trigger a read once; the second exclusion can go when `references/instructions-history.md` shows standards being read without an `Ignored` finding against their lines.)*
- **A project's own AGENTS.md** — only that repo needs it.
- **`~/.claude/.claude/CLAUDE.md`** — only for working on `~/.claude` itself; it loads in sessions started there and nowhere else.
- **A SKILL.md** — only one workflow needs it. Check the rule never applies outside that workflow: `a3cd73c` moved the attribution norm into `author-skill`, and `1122821` moved it back once issues filed on direct request went unmarked.
- **Deletion** — nothing else needs it. Last resort, and the only irreversible one.

## Find candidates

Ranked by how well the evidence holds:

1. **A rule the rest of the file already decides.** Strongest, because it is provable: name the other bullets that force the same outcome. They are often in a different section — the check-suite requirement under Commits is what makes a non-compiling commit impossible, not anything in Code style.
2. **A rule that states its ruling and then illustrates it.** The same proof as 1, inside one bullet: name the clause that already decides it and cut the worked example — but only where the ruling survives alone. A counterweight whose whole value *is* its examples is not this ("skip extraction only when it would reduce clarity" is a blank check without them).
3. **A rule scoped to one kind of work, one repo, or one workflow.** Move it; see above.
4. **A special case that never recurred.** `blame` names the commit and its message gives the reason; nothing resembling it in the log since. Weak on its own — a special case can be rare *and* load-bearing, so pair it with 1.

Don't cut a rule because the model running this pass would follow it unprompted. `/skill-compact` can use that test — a skill has one suggested model in the README table. CLAUDE.md has none: it loads for every model, including Haiku and Sonnet subagents spawned inside a session that started on Opus, and the harness prompt around it varies by model, surface, and Claude Code version — none of it versioned here, none of it observable for the sessions you are cutting on behalf of. What you would do unprompted is evidence about you, not about the file.

## Decide

You cannot observe that a rule is unnecessary: a session that went fine is equally consistent with the rule working and with it never having been needed. Present each candidate with its evidence in prose and take a free-form pick ("all", "1 and 3", "trim 2 instead") — the evidence is the decision.

The one real verdict comes later. This lists every line ever removed, so a rule that came back appears twice:

```sh
git -C ~/.claude log -p --reverse -- CLAUDE.md | grep -E '^-' | grep -v '^---'
```

A rule that was re-added is load-bearing — don't propose it a second time.

## Finish

Record declined candidates and why in the commit message, so the next pass doesn't re-derive them. Commit CLAUDE.md together with any file a rule moved into — a pass of pure trims touches nothing else. `~/.claude/.claude/CLAUDE.md` names the checks for what the commit stages; a markdown-only commit has none.
