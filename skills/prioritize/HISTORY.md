# Skill History

2026-07-30 · claude-config · 504 words · created
2026-08-02 · claude-config · 504 words · retro minor: Shortlist floor of 5 forced a padded item the Rank rules disqualify; also unpatched: drill-down scoped per-repo while the root cause spanned repos, and failing-log.sh emitted raw ANSI
2026-08-02 · claude-config · 505 words · fix small: Drop the shortlist floor and cap at 5, so the length bound stops contradicting "No padding"
2026-08-23 · claude-config · 505 words · retro minor: CI column printed blank for an in-progress run, since gather tests conclusion for null while gh sends ""; also unpatched: ALERTS gives severities but no package, so ranking an alert needs an undocumented gh api drill-down
2026-08-23 · claude-config · 505 words · fix small: Treat an empty conclusion as in-progress so the CI column stops going blank, and drop gh log timestamps and the unresolved step column from the drill-down
2026-08-23 · claude-config · 518 words · fix small: Scope the description to cross-repo and read-only, so the new groom skill wins the single-repo backlog trigger
