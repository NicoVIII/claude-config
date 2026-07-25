/// What the survey is about, independent of how gh spells it.
///
/// Every value that reaches the reader as text is a case here, and Render.fs
/// holds the mapping back to strings — so the vocabulary SKILL.md classifies on
/// lives in one place, and adding a case breaks the render match until it is
/// handled.
module Domain

/// How far a bump moves. Pre-1.0 minors are Major because `0.27 -> 0.28` breaks
/// as freely as `1.0 -> 2.0` does.
type Level =
    | Patch
    | Minor
    | Major
    /// A version pair this cannot compare, or a PR whose body and title yield
    /// no `Update(s|d) x from A to B` line at all. Ranked with Major, so the
    /// parse regressing — as it did on nuget's phrasing (fd9763a) — flags PRs
    /// rather than waving them through as patches.
    | Unclear of why: string

module Level =
    /// Worst first: a grouped PR is as risky as its riskiest member.
    let rank =
        function
        | Unclear _ -> 0
        | Major -> 1
        | Minor -> 2
        | Patch -> 3

/// One dependency line out of a Dependabot body or title.
type Bump =
    { name: string
      fromVersion: string
      toVersion: string
      level: Level
      /// The project link markdown-style bodies carry, as the fallback when the
      /// body holds no release-notes or changelog URL.
      home: string option }

/// GitHub's `mergeStateStatus`. Only Dirty decides anything on its own — it is
/// the needs-rebase signal — but the rest are echoed rather than collapsed,
/// because "why can't this merge" is the question the reader is asking.
type MergeState =
    | Clean
    | Dirty
    | Blocked
    | Behind
    | Unstable
    | Draft
    | OtherState of string

module MergeState =
    let parse =
        function
        | "CLEAN" -> Clean
        | "DIRTY" -> Dirty
        | "BLOCKED" -> Blocked
        | "BEHIND" -> Behind
        | "UNSTABLE" -> Unstable
        | "DRAFT" -> Draft
        | other -> OtherState other

/// A check with no conclusion yet is pending, not red. Anything that concluded
/// outside the three known-good states is red, including conclusions GitHub
/// adds after this was written.
type Ci =
    | Green
    | Pending
    | Red of failing: (string * string option) list

/// Whether the diff backs up what the body claims. A body can name a bump the
/// committed diff no longer contains, so a PR touching nothing but lockfiles is
/// a stale refresh to classify from its diff (fd9763a).
type Files =
    | Manifest
    | LockOnly of names: string list

type Method =
    | Squash
    | Merge
    | Rebase

type Pr =
    { number: int
      title: string
      level: Level
      bumps: Bump list
      merge: MergeState
      ci: Ci
      files: Files
      /// Another open bot PR taking the same dependency further.
      supersededBy: int option
      notes: string list }
