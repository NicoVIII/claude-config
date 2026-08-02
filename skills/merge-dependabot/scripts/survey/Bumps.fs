/// Reading what a Dependabot PR bumps, and by how much, out of its prose.
///
/// Pure, and the one part of this tool with no gh call in it — which is why it
/// carries the test suite: both regressions the run log records (nuget's
/// `Updated [x]` phrasing, pre-1.0 minors read as minor) landed here.
module Bumps

open System
open System.Text.RegularExpressions
open Domain

/// Dependabot's own phrasings for one dependency line. npm-style bodies say
/// ``Updates `x` from A to B``, nuget-style say `Updated [x](url) from A to B`,
/// and the title says `Bump x from A to B` — the last is the fallback for a PR
/// whose body is release notes only.
let private bodyPattern =
    Regex(
        @"^\s*Update[sd]\s+(?:`(?<name>[^`]+)`|\[(?<name>[^\]]+)\]\((?<home>[^)]*)\)|(?<name>\S+))\s+from\s+(?<from>\S+)\s+to\s+(?<to>\S+)",
        RegexOptions.Multiline
    )

let private titlePattern =
    Regex(@"\bbump\s+(?:`(?<name>[^`]+)`|(?<name>\S+))\s+from\s+(?<from>\S+)\s+to\s+(?<to>\S+)", RegexOptions.IgnoreCase)

/// A version's (major, minor), tolerating a `v` prefix, a pre-release suffix and
/// a missing minor. None where either component is not a number, which is what
/// makes an exotic version Unclear rather than silently Patch.
let private majorMinor (version: string) =
    let core = version.TrimStart('v').Split([| '-'; '+' |]).[0]
    let parts = core.Split '.'

    let part index =
        if index >= parts.Length then Some 0
        else
            match Int32.TryParse parts[index] with
            | true, value -> Some value
            | _ -> None

    match part 0, part 1 with
    | Some major, Some minor -> Some(major, minor)
    | _ -> None

let level (fromVersion: string) (toVersion: string) =
    match majorMinor fromVersion, majorMinor toVersion with
    | Some(fromMajor, fromMinor), Some(toMajor, toMinor) ->
        if fromMajor <> toMajor then Major
        elif fromMajor = 0 && fromMinor <> toMinor then Major
        elif fromMinor <> toMinor then Minor
        else Patch
    | _ -> Unclear $"cannot compare {fromVersion} -> {toVersion}"

/// Sortable form of a version, for deciding which of two PRs on one dependency
/// is the survivor. Shorter lists compare below longer ones with equal prefixes,
/// which is the wanted order for `1.2` against `1.2.1`.
let private ordinal (version: string) =
    version.TrimStart('v').Split([| '-'; '+' |]).[0].Split '.'
    |> Array.toList
    |> List.map (fun part ->
        match Int32.TryParse part with
        | true, value -> value
        | _ -> -1)

/// Prose runs the version into the sentence around it often enough to matter.
let private trimVersion (version: string) = version.Trim([| '.'; ','; ';'; ')'; '`' |])

let private matches (pattern: Regex) (text: string) =
    pattern.Matches text
    |> Seq.map (fun hit ->
        let group (name: string) =
            let value = hit.Groups[name]
            if value.Success && value.Value <> "" then Some value.Value else None

        let fromVersion = trimVersion hit.Groups["from"].Value
        let toVersion = trimVersion hit.Groups["to"].Value

        { name = hit.Groups["name"].Value
          fromVersion = fromVersion
          toVersion = toVersion
          level = level fromVersion toVersion
          home = group "home" })
    |> Seq.distinctBy (fun bump -> bump.name, bump.fromVersion, bump.toVersion)
    |> Seq.toList

/// The body is authoritative for a grouped PR — the title only names the group.
/// Falling back to the title costs nothing when the body already answered.
let parse (body: string) (title: string) =
    match matches bodyPattern body with
    | [] -> matches titlePattern title
    | found -> found

/// Worst member wins, and a PR nothing parsed out of is Unclear rather than
/// absent — a bump level this cannot state must not read as a safe one.
let worst (bumps: Bump list) =
    bumps
    |> List.sortBy (fun bump -> Level.rank bump.level)
    |> List.tryHead
    |> Option.map (fun bump -> bump.level)
    |> Option.defaultValue (Unclear "no Update(s|d) line in the body or title")

/// Two open bot PRs on one dependency: the lower target is superseded by the
/// higher. Grouped PRs take part through every member they share, and the
/// furthest-ahead rival wins when several qualify.
let supersededBy (others: (int * Bump list) list) (bumps: Bump list) =
    let ourTarget name =
        bumps
        |> List.tryFind (fun bump -> bump.name = name)
        |> Option.map (fun bump -> ordinal bump.toVersion)

    others
    |> List.collect (fun (other, theirs) ->
        theirs
        |> List.choose (fun their ->
            let target = ordinal their.toVersion

            match ourTarget their.name with
            | Some ours when ours < target -> Some(other, target)
            | _ -> None))
    |> List.sortByDescending snd
    |> List.tryHead
    |> Option.map fst
