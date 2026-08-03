/// What can be wrong with a dependabot.yml, and where.
module Domain

/// The file's own path to a finding: `updates[1] (nuget)`.
type Site = Site of string

type Finding =
    | RootNotAMapping
    | WrongVersion of found: string
    | NoUpdates
    | NotAMapping of Site
    | UnknownKey of Site * key: string
    | MissingRequired of Site * key: string
    | MissingSchedule of Site
    | DirectoryChoice of Site * found: string list
    | UnknownEcosystem of Site * value: string
    | UndocumentedGlob of Site * glob: string
