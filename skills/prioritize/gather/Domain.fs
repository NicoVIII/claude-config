/// What the digest is about, independent of how gh spells it.
///
/// Every value that reaches the reader as text is a case here, and Render.fs
/// holds the mapping back to strings — so the vocabulary SKILL.md documents
/// lives in one place, and adding a case breaks the render match until it is
/// handled.
module Domain

/// Who spoke last on an issue someone else opened. Not "did I ever reply": an
/// issue I answered and they came back to is still on me, one sitting on my own
/// reply is not.
type Turn =
    | Unanswered
    | TheirsLast
    | MineLast

/// Why an open PR needs a decision rather than just a count.
type PrState =
    | FailingCi
    | HumanAuthored

type CiStatus =
    | NoBranch
    | NoRuns
    | InProgress
    | Concluded of conclusion: string

/// GitHub's own vocabulary, so unlike the rest of this file the text belongs to
/// the value rather than to the presentation — `name` echoes back what the API
/// said, and `rank` exists because one critical outranks a pile of lows.
type Severity =
    | Critical
    | High
    | Medium
    | Low
    | Unrecognised of string

module Severity =
    let parse =
        function
        | "critical" -> Critical
        | "high" -> High
        | "medium" -> Medium
        | "low" -> Low
        | other -> Unrecognised other

    let name =
        function
        | Critical -> "critical"
        | High -> "high"
        | Medium -> "medium"
        | Low -> "low"
        | Unrecognised other -> other

    let rank =
        function
        | Critical -> 0
        | High -> 1
        | Medium -> 2
        | Low -> 3
        | Unrecognised _ -> 4

type Alerts =
    | Disabled
    | Clean
    | Counts of (Severity * int) list

/// What kind of attention a row needs, carrying the state that goes with it.
/// These were two loose strings before; they are correlated — a review request
/// has no turn, an issue thread always does.
type Kind =
    | Review
    | IssueThread of turn: Turn * replies: int
    | PullRequest of PrState

/// One line of the ATTENTION block: something a human has to weigh, as opposed
/// to something that only needs counting.
type Attention =
    { kind: Kind
      target: string
      who: string
      date: string
      title: string }

type Row =
    { repo: string
      pulls: int
      issues: int
      ci: CiStatus
      alerts: Alerts
      pushed: string }

type Report = { row: Row; attention: Attention list }
