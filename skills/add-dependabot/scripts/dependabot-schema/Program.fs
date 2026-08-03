module Program

[<EntryPoint>]
let main argv =
    match List.ofArray argv with
    | [ "ecosystems" ] ->
        (Schema.fetch ()).Ecosystems |> List.iter (printfn "%s")
        0
    | [ "check"; path ] when System.IO.File.Exists path ->
        let findings = Check.config (Schema.fetch ()) (Config.load path)
        findings |> List.iter (Render.finding >> printfn "%s")

        // The last line is the verdict, so a truncated run cannot read as a pass.
        if List.isEmpty findings then
            printfn "matches the schema"
            0
        else
            printfn $"%d{List.length findings} finding(s)"
            1
    | [ "check"; path ] ->
        eprintfn $"no such file: %s{path}"
        2
    | _ ->
        eprintfn "usage: dependabot-schema ecosystems | check <file>"
        2
