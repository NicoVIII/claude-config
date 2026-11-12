/// Stands in for every registry at the HttpMessageHandler, so a test runs the
/// real client above it and sees the request a registry would have seen.
///
/// It answers only the requests a test lists, URL and Authorization both. One
/// it does not know throws, and not as an HttpRequestException: a changed URL
/// or a dropped header has to fail the test, not read as an unreachable host.
module FakeRegistry

open System.Net
open System.Net.Http
open System.Threading.Tasks

type Canned =
    | Body of string
    | Status of int
    | Down of reason: string

type Listed =
    { Url: string
      Authorization: string option
      Answer: Canned }

let anonymous (url: string) (answer: Canned) =
    { Url = url
      Authorization = None
      Answer = answer }

let bearing (token: string) (url: string) (answer: Canned) =
    { Url = url
      Authorization = Some $"Bearer %s{token}"
      Answer = answer }

type Handler(listed: Listed list) =
    inherit HttpMessageHandler()

    member val UserAgents = ResizeArray<string>()

    override this.SendAsync(request, _) =
        let url = request.RequestUri.OriginalString

        let authorization =
            request.Headers.Authorization |> Option.ofObj |> Option.map string

        this.UserAgents.Add(string request.Headers.UserAgent)

        let known =
            listed
            |> List.tryFind (fun entry -> entry.Url = url && entry.Authorization = authorization)

        match known with
        | Some { Answer = Body body } ->
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK, Content = new StringContent(body)))
        | Some { Answer = Status status } -> Task.FromResult(new HttpResponseMessage(enum status))
        | Some { Answer = Down reason } -> raise (HttpRequestException reason)
        | None -> failwith $"unexpected request: %s{url} (authorization: %A{authorization})"
