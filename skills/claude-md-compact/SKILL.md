---
name: claude-md-compact
description: Shrink CLAUDE.md — cut rules the rest of the file already decides, move repo-scoped ones down into an AGENTS.md or a skill. Use when CLAUDE.md has grown, when I say my global preferences are too long or cost too much, or after a stretch of sessions has been adding rules to it.
---

Reduce what loads into every session in every project, without losing a preference.

## Scope

`~/.claude/CLAUDE.md` only. Removal only — never add a rule in this pass; note what you spot and let a later session add it. `/skill-compact` keeps its passes separate for this reason, and CLAUDE.md has the same failure mode: additions made alongside a removal cancel it.

The pass must end with **fewer words in CLAUDE.md than it started**. A rule moved into an AGENTS.md or a SKILL.md counts — those load in fewer sessions.

Compaction passes have no run log of their own. Git is the log:

```sh
git -C ~/.claude log --format='%h %s' -- CLAUDE.md
git -C ~/.claude blame -- CLAUDE.md
```

For the size curve, which shows what past passes achieved:

```sh
git -C ~/.claude log --format=%h -- CLAUDE.md | tac | while read c; do
  printf '%s %4s %s\n' "$c" "$(git -C ~/.claude show $c:CLAUDE.md | wc -w)" \
    "$(git -C ~/.claude log -1 --format=%s $c)"; done
```

Don't build a ledger beside the file — that command reconstructs every baseline. Aim for ~500 words; it's an anchor for judgement, not a gate.

## Check where a destination loads before proposing a move

`~/.claude/AGENTS.md` loads **only in sessions inside `~/.claude`**. A rule that fires in every repo cannot move there — the move reads as deduplication and silently drops the rule everywhere else. When the same idea appears in CLAUDE.md and an AGENTS.md, the redundant copy is usually the narrower one, so the deletion belongs at that end.

Destinations, by what needs the rule:

- **A project's own AGENTS.md** — only that repo needs it.
- **`~/.claude/AGENTS.md`** — it only fires while working on `~/.claude` itself.
- **A SKILL.md** — only one workflow needs it (`a3cd73c` moved the attribution norm into `author-skill` this way; still the only reduction in the file's history).
- **Deletion** — nothing else needs it. Last resort, and the only irreversible one.

## Find candidates

Ranked by how well the evidence holds:

1. **A rule the rest of the file already decides.** Strongest, because it is provable: name the other bullets that force the same outcome. They are often in a different section — the check-suite requirement under Commits is what makes a non-compiling commit impossible, not anything in Code style.
2. **A rule that states its ruling and then illustrates it.** The same proof as 1, inside one bullet: name the clause that already decides it and cut the worked example — but only where the ruling survives alone. A counterweight whose whole value *is* its examples is not this ("skip extraction only when it would reduce clarity" is a blank check without them).
3. **A rule scoped to one repo or one workflow.** Move it; see above.
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

Record declined candidates and why in the commit message, so the next pass doesn't re-derive them. Run the repo's check suite, and commit CLAUDE.md together with any file a rule moved into — a pass of pure trims touches nothing else.

---

This skill is not yet battle-tested: if any instruction above was ambiguous, wrong, or needed a workaround, say so briefly at the end of the run.
