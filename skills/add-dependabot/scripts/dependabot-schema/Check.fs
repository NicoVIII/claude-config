/// Every check the schema can settle without asking GitHub.
module Check

open Config
open Domain
open Schema

let private unknownKeys site allowed node =
    Set.difference (keys node) allowed
    |> Seq.sort
    |> Seq.map (fun key -> UnknownKey(site, key))
    |> List.ofSeq

/// `directories` and `directory` are alternatives, so both feed one list.
let private directories entry =
    match tryGet "directories" entry with
    | Some(Sequence items) -> items |> List.choose tryScalar
    | _ -> tryGet "directory" entry |> Option.bind tryScalar |> Option.toList

let private groups entry =
    match tryGet "groups" entry with
    | Some(Mapping named) -> Map.toList named
    | _ -> []

let private update (schema: Constraints) index entry =
    let ecosystem = entry |> tryGet "package-ecosystem" |> Option.bind tryScalar

    let site =
        match ecosystem with
        | Some name -> Site $"updates[%d{index}] (%s{name})"
        | None -> Site $"updates[%d{index}]"

    match entry with
    | Mapping _ ->
        [ yield! unknownKeys site schema.UpdateKeys entry

          for key in schema.RequiredUpdateKeys do
              if (tryGet key entry).IsNone then
                  yield MissingRequired(site, key)

          match ecosystem with
          | Some name when not (List.contains name schema.Ecosystems) -> yield UnknownEcosystem(site, name)
          | _ -> ()

          // These two live in the update definition's `allOf` conditionals
          // rather than its `required`, so the key check above cannot reach
          // them — and an entry missing either is one of the silent failures.
          if (tryGet "schedule" entry).IsNone && (tryGet "multi-ecosystem-group" entry).IsNone then
              yield MissingSchedule site

          match [ for key in [ "directory"; "directories" ] do
                      if (tryGet key entry).IsSome then key ] with
          | [ _ ] -> ()
          | found -> yield DirectoryChoice(site, found)

          for glob in directories entry do
              if glob.Contains "**" then
                  yield UndocumentedGlob(site, glob)

          for name, spec in groups entry do
              let groupSite =
                  let (Site where) = site
                  Site $"%s{where} group '%s{name}'"

              match spec with
              | Mapping _ -> yield! unknownKeys groupSite schema.GroupKeys spec
              | _ -> yield NotAMapping groupSite ]
    | _ -> [ NotAMapping site ]

let config (schema: Constraints) root =
    match root with
    | Mapping _ ->
        [ yield! unknownKeys (Site "root") schema.RootKeys root

          match tryGet "version" root |> Option.bind tryScalar with
          | Some "2" -> ()
          | found -> yield WrongVersion(defaultArg found "absent")

          match tryGet "updates" root with
          | Some(Sequence(_ :: _ as entries)) -> yield! entries |> List.mapi (update schema) |> List.concat
          | _ -> yield NoUpdates ]
    | _ -> [ RootNotAMapping ]
