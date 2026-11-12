/// A whole run, short of the console: what the tests drive.
module Run

open System.Net.Http
open Domain

type Outcome =
    { Exit: int
      Out: string list
      Err: string list }

let private probeAll (fetch: Fetch) (refs: FeatureRef list) =
    List.foldBack
        (fun ref rest ->
            match Registry.probe fetch ref, rest with
            | Ok probed, Ok lines -> Ok(Render.probed probed @ lines)
            | Error failure, _ -> Error failure
            | _, Error failure -> Error failure)
        refs
        (Ok [])

/// Nothing reaches stdout unless every request was answered: a report cut
/// short by the network would read as a complete one about fewer features.
let run (handler: HttpMessageHandler) (argv: string list) =
    match Cli.parse argv with
    | Error usage ->
        { Exit = 2
          Out = []
          Err = [ $"feature-probe: %s{Render.usageError usage}" ] }
    | Ok command ->
        use client = Http.client handler
        let fetch = Http.fetch client

        let lines =
            match command with
            | Probe refs -> probeAll fetch refs
            | Mine -> Registry.mineFeatures fetch
            | Base -> Registry.baseTags fetch

        match lines with
        | Ok out -> { Exit = 0; Out = out; Err = [] }
        | Error failure ->
            { Exit = 1
              Out = []
              Err = [ $"feature-probe: %s{Render.failure failure}" ] }
