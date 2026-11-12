module Program

open System.Net.Http

[<EntryPoint>]
let main argv =
    use handler = new HttpClientHandler()
    let outcome = Run.run handler (List.ofArray argv)
    outcome.Out |> List.iter (printfn "%s")
    outcome.Err |> List.iter (eprintfn "%s")
    outcome.Exit
