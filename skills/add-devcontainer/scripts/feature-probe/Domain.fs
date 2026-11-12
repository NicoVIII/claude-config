/// What feature-probe is asked, what a registry can answer, and what it reports.
module Domain

/// What devcontainer.json holds, less `ghcr.io/` and the tag:
/// `devcontainers/features/dotnet`.
type FeatureRef =
    { Owner: string
      Repo: string
      Feature: string }

type Command =
    | Probe of FeatureRef list
    | Mine
    | Base

type UsageError =
    | NoArguments
    | NotAFeatureRef of string

type Request = { Url: string; Bearer: string option }

/// `Refused` is the server speaking, and can be an answer about the feature;
/// `Unreachable` is the network, and never is.
type Reply =
    | Answered of body: string
    | Refused of status: int
    | Unreachable of reason: string

type Fetch = Request -> Reply

type Tags =
    | Published of string list
    | NotPublished

/// `options` is the manifest's own JSON, compacted: its keys are whatever the
/// feature's author chose, so there is nothing to name as a type.
type Manifest =
    | Manifest of version: string * options: string option
    | NoManifest of url: string

type Probed =
    { Ref: FeatureRef
      Tags: Tags
      Manifest: Manifest }

/// Why a run stops without an answer.
type Failure =
    | NoAnswer of url: string * reason: string
    | UnexpectedStatus of url: string * status: int
    | Malformed of url: string * problem: string
