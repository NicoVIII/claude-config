# skill-refiner contract

What the skill-authoring skills read from `skill-refiner.sh`: the entry `log`
echoes and the file it leaves, the maturity block, the ratio report with its
growth trace, and the usage errors.

The wrapper's binary sits in the real config tree, so a name it cannot find
elsewhere resolves into this repo's own `skills/`. Everything below therefore
runs in a throwaway session repo against skills in its `.claude/skills`, the
tree a name resolves in first — nothing here reads or writes a real log. Only
the date of a fresh entry and the scratch path vary, and only those lines are
matched by pattern.

```scrut
$ git init --quiet demo-repo && cp -r "$TESTDIR/project" demo-repo/.claude && cd demo-repo && export REFINER="$TESTDIR/../skill-refiner.sh"
```

## Before any table exists

A project nothing has logged in has no README beside its skills, so the rating
states the footer it calls for instead of a row to edit.

```scrut
$ "$REFINER" grown maturity
grown: log supports 🟢 Usable — no README table where its skills live
  5 runs; 3 since the last major retro (2 reviewed since the last big fix, across 2 repos)
  at this rating the SKILL.md carries no feedback footer
  🛡️ Battle-tested needs ~5 reviewed runs since the last major retro or big fix, across 2+ repos
```

## The ratio report

The headline measures against the last compaction and fires at exactly 1.50x,
the trace gives every recorded size its delta and clause, the floor line
measures against the lowest deliberate size, and a list marker inside a code
fence is not a rule.

```scrut
$ "$REFINER" grown ratio
grown: 24 words, 1.50x its baseline of 16 (last compaction) — over the 1.5x trigger — run /skill-compact before this grows further
  growth trace:
    2026-01-01 · created · 20 words
    2026-01-02 · retro minor · 22 words (+2) — guessed the format
    2026-01-03 · fix small · 26 words (+4) — pinned the format
    2026-01-04 · compacted · 10 words (-16) — merged two rules
    2026-01-05 · retro major · 12 words (+2) — aborted on a missing step
    2026-01-06 · fix big · 18 words (+6) — added the missing step
    2026-01-07 · compacted · 16 words (-2) — cut the rationale
    2026-01-08 · run · 16 words (+0)
    2026-01-09 · retro clean · 20 words (+4)
    2026-01-10 · retro minor · 24 words (+4) — one stale reference
  2.40x its lowest baseline of 10 (2026-01-04) — the floor has risen 6 words over 1 cycle since
  3 rules at 8 words each
```

A log that predates creation seeding is measured against its first entry, and
a skill named by its directory is reported under the path as given.

```scrut
$ "$REFINER" .claude/skills/steady ratio
.claude/skills/steady: 10 words, 1.00x its baseline of 10 (earliest logged entry) — under the 1.5x trigger
  growth trace:
    2026-02-01 · retro clean · 10 words
    2026-02-02 · retro minor · 10 words (+0) — wording unclear once
    2026-02-03 · retro clean · 10 words (+0)
    2026-02-04 · fix small · 10 words (+0) — reworded
    2026-02-05 · retro clean · 10 words (+0)
    2026-02-06 · retro clean · 10 words (+0)
  2 rules at 5 words each
```

```scrut
$ "$REFINER" young ratio
young: 6 words, never logged — no baseline to compare against
  no rules across 6 words
```

## Logging

The first entry in a project seeds the README beside its skills and names the
skills already sitting there without a row.

```scrut
$ "$REFINER" young log creation
logged to .*/demo-repo/\.claude/skills/young/HISTORY\.md: (regex)
  \d{4}-\d\d-\d\d · demo-repo · 6 words · created (regex)
  seeded .*/demo-repo/\.claude/skills/README\.md — commit it with the skill (regex)
  2 other skills beside it, with no row yet: grown, steady
    `skill-refiner <name> maturity` rates each; its summary and suggested model are yours to judge
```

A seeded table has no rows, and a skill with nothing logged gets one at 🚧 WIP.

```scrut
$ "$REFINER" young maturity
young: log holds no runs — README says unlisted, and 🚧 WIP is where a skill with nothing logged belongs
  0 runs; 0 since the last major retro (0 reviewed since the last big fix)
  add a README row at 🚧 WIP, with the skill's summary and a suggested model judged from what its most demanding mode needs (pick-model's tiers), not the unlisted-skill default; at this rating the SKILL.md carries the feedback footer, verbatim from references/skill-footer.md
  🧪 Experimental needs one run logged
```

```scrut
$ "$REFINER" young log run
logged to .*/demo-repo/\.claude/skills/young/HISTORY\.md: (regex)
  \d{4}-\d\d-\d\d · demo-repo · 6 words · run (regex)
```

```scrut
$ "$REFINER" young log retro minor 'guessed where the footer goes'
logged to .*/demo-repo/\.claude/skills/young/HISTORY\.md: (regex)
  \d{4}-\d\d-\d\d · demo-repo · 6 words · retro minor: guessed where the footer goes (regex)
```

A fix is logged after the edit, so it measures the new size and reports the
accretion without the trace.

```scrut
$ echo '- Ask first.' >> .claude/skills/young/SKILL.md && "$REFINER" young log fix small 'said where the footer goes'
logged to .*/demo-repo/\.claude/skills/young/HISTORY\.md: (regex)
  \d{4}-\d\d-\d\d · demo-repo · 9 words · fix small: said where the footer goes (regex)
young: 9 words, 1.50x its baseline of 6 (creation) — over the 1.5x trigger — run /skill-compact before this grows further
  1 rule of 9 words
```

```scrut
$ "$REFINER" young log compacted 'merged the two footer rules'
logged to .*/demo-repo/\.claude/skills/young/HISTORY\.md: (regex)
  \d{4}-\d\d-\d\d · demo-repo · 9 words · compacted: merged the two footer rules (regex)
```

The file those leave is the shape `references/skill-conventions.md` has a
caller append by hand when the script cannot run.

```scrut
$ cat .claude/skills/young/HISTORY.md
# Skill History

\d{4}-\d\d-\d\d · demo-repo · 6 words · created (regex)
\d{4}-\d\d-\d\d · demo-repo · 6 words · run (regex)
\d{4}-\d\d-\d\d · demo-repo · 6 words · retro minor: guessed where the footer goes (regex)
\d{4}-\d\d-\d\d · demo-repo · 9 words · fix small: said where the footer goes (regex)
\d{4}-\d\d-\d\d · demo-repo · 9 words · compacted: merged the two footer rules (regex)
```

The origin baseline is fixed once a change is logged against it.

```scrut
$ "$REFINER" young log creation 2>&1
skill-refiner: HISTORY.md already has changes logged — the origin baseline is fixed
[1]
```

## Maturity against a listed row

The log wins over the row, and crossing 🟢 Usable moves the footer with it.

```scrut
$ echo '| [`grown`](grown/SKILL.md) | summary | Sonnet | 🧪 Experimental |' >> .claude/skills/README.md && "$REFINER" grown maturity
grown: log supports 🟢 Usable — README says 🧪 Experimental
  5 runs; 3 since the last major retro (2 reviewed since the last big fix, across 2 repos)
  update the README row to 🟢 Usable, and remove the feedback footer from its SKILL.md
  🛡️ Battle-tested needs ~5 reviewed runs since the last major retro or big fix, across 2+ repos
```

A row the log agrees with names no edit, and the top rung no next bar.

```scrut
$ echo '| [`steady`](steady/SKILL.md) | summary | Sonnet | 🛡️ Battle-tested |' >> .claude/skills/README.md && "$REFINER" steady maturity
steady: log supports 🛡️ Battle-tested — README says 🛡️ Battle-tested
  5 runs; 5 since the last major retro (5 reviewed since the last big fix, across 2 repos)
```

## Usage errors

Every one exits 1 with nothing on stdout.

```scrut
$ "$REFINER" 2>stderr
[1]
```

```scrut
$ cat stderr
usage: skill-refiner <skill | path/to/skill> <log|maturity|ratio> [args]
  e.g. skill-refiner grilling maturity
```

```scrut
$ "$REFINER" grown rate 2>&1
skill-refiner: unknown command 'rate' (expected log, maturity or ratio)
[1]
```

A typo'd skill is named as such before the command is looked at.

```scrut
$ "$REFINER" no-such-skill-here maturity 2>&1
skill-refiner: no skill named 'no-such-skill-here'
[1]
```

```scrut
$ "$REFINER" grown log 2>&1
usage: skill-refiner <skill> log <creation | run | retro … | fix … | compacted <clause>>
[1]
```

```scrut
$ "$REFINER" grown log retro 2>&1
usage: skill-refiner <skill> log retro <clean | minor <clause> | major <clause>>
  e.g. skill-refiner grilling log retro minor 'footer placement guessed'
[1]
```

```scrut
$ "$REFINER" grown log fix small 2>&1
usage: skill-refiner <skill> log fix <small|big> <clause>
  e.g. skill-refiner grilling log fix small 'footer placement pinned down'
[1]
```

```scrut
$ "$REFINER" grown log compacted 2>&1
usage: skill-refiner <skill> log compacted <clause>
  e.g. skill-refiner grilling log compacted 'merged the two retraction rules'
[1]
```

A blank clause is refused, and nothing was appended by any of the above.

```scrut
$ "$REFINER" grown log retro major ' ' 2>&1
skill-refiner: the clause is empty — say what happened, so a later retro can match a recurrence against it
[1]
```

```scrut
$ diff "$TESTDIR/project/skills/grown/HISTORY.md" .claude/skills/grown/HISTORY.md
```
