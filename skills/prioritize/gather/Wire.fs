/// gh's wire shapes, mirroring exactly what the `--json` field lists ask for.
///
/// This file changes when GitHub changes; Domain.fs changes when the skill's
/// judgement changes. Field names match the JSON keys verbatim, so the two
/// snake_case exceptions below are the API's spelling, not a style choice.
module Wire

open System.Text.Json.Serialization

type Owner = { login: string }

type Author =
    { login: string
      [<JsonPropertyName "is_bot">]
      isBot: bool }

/// The author shape for paths that only ever compare logins. Issue comments
/// genuinely carry nothing else, and search hits are weight 1 regardless of who
/// opened them. A separate type rather than an optional field, so requiring
/// is_bot where it is actually used stays honest — the Rust version defaulted it
/// instead, and so claimed a field gh does not send on comments.
type AuthorLogin = { login: string }

type BranchRef = { name: string }

type Repo =
    { name: string
      owner: Owner
      pushedAt: string
      defaultBranchRef: BranchRef option }

type Check = { conclusion: string option }

type Pull =
    { number: int
      title: string
      author: Author
      updatedAt: string
      statusCheckRollup: Check list option }

type Issue = { number: int; author: Author }

type Comment = { author: AuthorLogin }

type IssueDetail =
    { title: string
      author: Author
      createdAt: string
      comments: Comment list }

type Run = { conclusion: string option }

type Advisory = { severity: string }

type Alert =
    { [<JsonPropertyName "security_advisory">]
      securityAdvisory: Advisory }

type RepoName = { nameWithOwner: string }

type SearchPull =
    { number: int
      title: string
      repository: RepoName
      author: AuthorLogin
      updatedAt: string }

type User = { login: string }
