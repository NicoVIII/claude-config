/// gh's wire shapes, mirroring exactly what the `--json` field lists ask for.
///
/// This file changes when GitHub changes; Domain.fs changes when the skill's
/// judgement changes. Field names match the JSON keys verbatim.
module Wire

/// A rollup entry as GitHub's CheckRun spells it. A legacy StatusContext entry
/// carries `context`/`state` instead and aborts the decode by name — every repo
/// here is Actions-only, and a hand-written converter would buy a path nothing
/// walks at the cost of the strictness invariant (abd4b0f).
type Check =
    { name: string
      /// Absent while the check is still running.
      conclusion: string option
      detailsUrl: string option }

type Pull =
    { number: int
      title: string
      statusCheckRollup: Check list option }

/// Separate from Body because mergeability has to be asked for repeatedly and
/// the body does not — see Signals.mergeState.
type Mergeability = { mergeStateStatus: string }

type Body = { body: string }

type MergeMethods =
    { squashMergeAllowed: bool
      mergeCommitAllowed: bool
      rebaseMergeAllowed: bool }
