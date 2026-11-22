# Skill History

2026-07-30 · claude-config · 983 words · created
2026-08-02 · claude-config · 1047 words · retro major: size signal passed an 876-word draft that was ~45% motivation; user interrupted log creation to stop a permanently inflated origin baseline
2026-08-02 · claude-config · 1078 words · fix big: replaced the size-vs-longest comparison with a composition test: cut sentences that justify rather than instruct
2026-08-23 · claude-config · 1060 words · retro minor: log creation froze the origin baseline before the review loop closed — second sighting of the 2026-08-02 interrupt; also unpatched: no guidance for a new skill colliding with an existing skill trigger description
2026-08-23 · claude-config · 1049 words · fix big: Let log creation re-seed a creation-only log, so the baseline follows the review loop instead of freezing ahead of it
2026-09-06 · claude-config · 1189 words · fix big: the confirm-it-is-global handoff became a pick-the-tree step, so a project skill is created and logged in its own repo instead of being handed off
2026-09-13 · tcg-card-collector · 1223 words · retro minor: cat-for-review output not user-visible in the harness; deferred first sightings: logged-to path echoed the /mnt/host-claude mount, no format for marking untested rules, global commit checks only reachable via skill-conventions' repo.md link
2026-09-13 · tcg-card-collector · 1235 words · fix small: review step hands over path plus rule summary instead of cat, whose output the harness doesn't reliably show the user
2026-09-18 · tcg-card-collector · 1292 words · fix big: untested marks need the assumption and its confirmation inline; a bare not-yet-run path list is explicitly not one
2026-11-22 · Administration · 1354 words · retro minor: edit mode named no log event and its creation-only list omitted log creation (fixed); deferred, first sighting: review gate vs. an already-approved edit, committed before showing path and summary
2026-11-22 · Administration · 1294 words · fix big: edit mode now lists log creation as creation-only and names the fix entry an edit logs; footer removed on reaching Usable
2026-11-22 · claude-config · 1325 words · fix small: commit step: the hook, not just check, is what a ~/.claude commit owes
