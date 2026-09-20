# Skill History

2026-09-13 · claude-config · 686 words · created
2026-09-13 · tcg-card-collector · 686 words · retro minor: first run: PR query pulled 8.4KB of check detail; deferred: latest-run-per-workflow ignores in-progress runs, green-and-unmerged PR criterion ranks stale bot bumps above shipped bugs
2026-09-13 · tcg-card-collector · 655 words · fix small: trim PR query to one conclusion set per PR; drop Untested paragraph the footer already covers
2026-09-13 · tcg-card-collector · 655 words · retro major: recommended a lone stale Dependabot bump as tier 1 over shipped bugs (user rejected); guessed on in-progress latest CI run — both deferred mechanisms from first retro recurred
2026-09-13 · tcg-card-collector · 701 words · fix big: bot bumps rank only as a pile of 3+ (not per PR, not as own PR); CI judged by latest completed run
2026-09-13 · tcg-card-collector · 701 words · retro minor: tier-4 pick hinged on unstated hard-vs-soft blocker weighting (deferred: blocker weighting in tier 4 ranking); ≤40-issue body fetch pulled 38.7KB and forced a re-fetch; milestones API fallback skipped as dead
2026-09-13 · tcg-card-collector · 714 words · fix big: issue bodies never fetched in bulk: defects via gh issue view, cross-references via a jq scan; drop dead milestones API fallback
2026-09-16 · tcg-card-collector · 714 words · retro minor: runner-up rule contradicted the stop-at-first-tier rule (forced reading lower tiers); deferred: non-CI workflow runs (Dependabot Updates) counted as red CI, tier-3 tie-break for mutually cross-referencing issues
2026-09-16 · tcg-card-collector · 728 words · fix small: runner-ups may read lower tiers when the winning tier has fewer than three candidates
2026-09-18 · tcg-card-collector · 728 words · retro minor: milestones API re-invoked for a due date the issue list already carries; tier-2 issue query still unprojected at 16.7KB; deferred: tier-2 gate is silent on error-path defects that do surface an error message
2026-09-18 · tcg-card-collector · 749 words · fix small: project the tier-2 issue list with a jq (16.7KB → 4.2KB) and carry dueOn in it; point tier 3 at that column instead of the milestones API
2026-09-18 · tcg-card-collector · 722 words · retro minor: tier-3 tie-break left no in-milestone edge (sole blocker #89 already landed) so ranking fell to unlogged severity judgement; tier-2 gate ambiguous on #51, an error path that warns and leaves wrong state; milestones API re-invoked one fix later for milestone enumeration, not a due date
2026-09-18 · tcg-card-collector · 779 words · fix big: tier 3 ranks on cross-references from any open issue minus landed work, then blast radius and age; tier-2 gate drops error paths whose trigger is itself a failure; milestone column stated as the whole relevant set
2026-09-20 · tcg-card-collector · 779 words · retro minor: tier-2 label clause ambiguous when repo has a bug label no open issue carries (two readings, different winners); milestones API re-invoked a third retro running, this time for the description; deferred: tier-3 body reads unbounded at 13.9KB, issue bodies overtaken by code they cite
2026-09-20 · tcg-card-collector · 851 words · fix big: the repo bug label, where it exists, is authoritative over issue text; tier 3 reads the winning milestone description and the report names the promise it serves
