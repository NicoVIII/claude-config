/// The only mapping from a finding back to text.
module Render

open Domain

let private at (Site where) = where

let finding =
    function
    | RootNotAMapping -> "root: not a mapping"
    | WrongVersion found -> $"root: version is %s{found}, must be 2"
    | NoUpdates -> "root: no 'updates' entries"
    | NotAMapping site -> $"%s{at site}: not a mapping"
    | UnknownKey(site, key) -> $"%s{at site}: unknown key '%s{key}'"
    | MissingRequired(site, key) -> $"%s{at site}: missing required key '%s{key}'"
    | MissingSchedule site -> $"%s{at site}: no 'schedule'"
    | DirectoryChoice(site, []) -> $"%s{at site}: neither 'directory' nor 'directories'"
    | DirectoryChoice(site, found) -> $"""%s{at site}: %s{String.concat " and " found} — exactly one"""
    | UnknownEcosystem(site, value) -> $"%s{at site}: '%s{value}' is not a package-ecosystem"
    | UndocumentedGlob(site, glob) -> $"%s{at site}: '%s{glob}' uses '**', which is undocumented"
