/// The one place a request leaves the process.
module Http

open System.Net.Http
open System.Net.Http.Headers
open System.Threading.Tasks
open Domain

/// The handler is the seam the tests fake: everything from here down — the
/// headers included — is then what a real request would have carried.
let client (handler: HttpMessageHandler) =
    let client = new HttpClient(handler)
    // api.github.com answers 403 to a request that names no client.
    client.DefaultRequestHeaders.UserAgent.ParseAdd "feature-probe"
    client

let fetch (client: HttpClient) (request: Request) =
    use message = new HttpRequestMessage(HttpMethod.Get, request.Url)

    request.Bearer
    |> Option.iter (fun token -> message.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token))

    try
        use response = client.SendAsync(message).GetAwaiter().GetResult()

        if response.IsSuccessStatusCode then
            Answered(response.Content.ReadAsStringAsync().GetAwaiter().GetResult())
        else
            Refused(int response.StatusCode)
    with
    | :? HttpRequestException as error -> Unreachable error.Message
    | :? TaskCanceledException -> Unreachable "timed out"
