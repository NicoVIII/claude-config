/// What add-devcontainer's SKILL.md reads from feature-probe, matched whole.
/// Run.run is everything but the console, so only Program's three lines of
/// printing go unexercised.
module RunTests

open Expecto
open FakeRegistry

let private token (repository: string) =
    $"https://ghcr.io/token?scope=repository:%s{repository}:pull&service=ghcr.io"

let private tagList (repository: string) =
    $"https://ghcr.io/v2/%s{repository}/tags/list"

let private manifest (owner: string) (repo: string) (feature: string) =
    $"https://raw.githubusercontent.com/%s{owner}/%s{repo}/main/src/%s{feature}/devcontainer-feature.json"

let private granted = Body """{"token":"pull-token"}"""

let private run (listed: Listed list) (argv: string list) =
    let handler = new Handler(listed)
    Run.run handler argv, handler

let private dotnetManifest =
    """{
  "id": "dotnet",
  "version": "2.4.1",
  "name": "Dotnet CLI",
  "options": {
    "version": {
      "type": "string",
      "proposals": ["latest", "lts", "10.0"],
      "default": "latest"
    }
  }
}"""

let tests =
    testList
        "Run"
        [ test "reports tags and pinnable options per ref, and both answers for a feature that is not there" {
              // Arrange — `ghost` is refused its token, which is what the real
              // registry does; `half` is granted one and then has no tag list,
              // the case 0dc5dc7 found crashing the shell version.
              let listed =
                  [ anonymous (token "devcontainers/features/dotnet") granted
                    bearing
                        "pull-token"
                        (tagList "devcontainers/features/dotnet")
                        (Body """{"name":"devcontainers/features/dotnet","tags":["1","2","2.4","2.4.1","latest"]}""")
                    anonymous (manifest "devcontainers" "features" "dotnet") (Body dotnetManifest)
                    anonymous (token "acme/features/ghost") (Status 403)
                    anonymous (manifest "acme" "features" "ghost") (Status 404)
                    anonymous (token "acme/features/half") granted
                    bearing "pull-token" (tagList "acme/features/half") (Status 404)
                    anonymous
                        (manifest "acme" "features" "half")
                        (Body """{"id":"half","version":"0.1.0","options":{}}""")
                    anonymous (token "acme/features/bare") granted
                    bearing "pull-token" (tagList "acme/features/bare") (Body """{"tags":["latest"]}""")
                    anonymous (manifest "acme" "features" "bare") (Body """{"id":"bare","version":"1.0.0+build"}""") ]

              // Act — a ref is taken with or without `ghcr.io/` and a tag
              let outcome, _ =
                  run
                      listed
                      [ "ghcr.io/devcontainers/features/dotnet:2"
                        "acme/features/ghost"
                        "acme/features/half"
                        "acme/features/bare" ]

              // Assert
              Expect.equal
                  outcome
                  { Exit = 0
                    Err = []
                    Out =
                      [ "== devcontainers/features/dotnet"
                        """tags     ["1","2","2.4","2.4.1","latest"]"""
                        """manifest {"version":"2.4.1","options":{"version":{"type":"string","proposals":["latest","lts","10.0"],"default":"latest"}}}"""
                        "== acme/features/ghost"
                        "tags     NOT PUBLISHED — no such artifact on ghcr.io"
                        "manifest NO MANIFEST at https://raw.githubusercontent.com/acme/features/main/src/ghost/devcontainer-feature.json — wrong path, or not a src/<feature> layout"
                        "== acme/features/half"
                        "tags     NOT PUBLISHED — no such artifact on ghcr.io"
                        """manifest {"version":"0.1.0","options":{}}"""
                        "== acme/features/bare"
                        """tags     ["latest"]"""
                        """manifest {"version":"1.0.0+build","options":null}""" ] }
                  "the whole report"
          }

          test "an unreachable host is never an answer about the feature, whichever host it is" {
              // Arrange — the manifest host is the one the shell version let
              // read as NO MANIFEST
              let ghcrDown = [ anonymous (token "acme/features/ghost") (Down "no such host") ]

              let rawDown =
                  [ anonymous (token "devcontainers/features/dotnet") granted
                    bearing "pull-token" (tagList "devcontainers/features/dotnet") (Body """{"tags":["2"]}""")
                    anonymous (manifest "devcontainers" "features" "dotnet") (Down "no such host") ]

              // Act
              let ghcr, _ = run ghcrDown [ "acme/features/ghost" ]
              let raw, _ = run rawDown [ "devcontainers/features/dotnet" ]

              // Assert — nothing on stdout: half a report reads as a whole one
              Expect.equal
                  ghcr
                  { Exit = 1
                    Out = []
                    Err =
                      [ "feature-probe: https://ghcr.io/token?scope=repository:acme/features/ghost:pull&service=ghcr.io unreachable: no such host" ] }
                  "ghcr.io down"

              Expect.equal
                  raw
                  { Exit = 1
                    Out = []
                    Err =
                      [ "feature-probe: https://raw.githubusercontent.com/devcontainers/features/main/src/dotnet/devcontainer-feature.json unreachable: no such host" ] }
                  "raw.githubusercontent.com down"
          }

          test "a refusal that says nothing about the feature stops the run" {
              // Arrange — a rate limit and a server error are not "not published"
              let limited = [ anonymous (token "acme/features/ghost") (Status 429) ]

              let broken =
                  [ anonymous (token "acme/features/ghost") (Status 403)
                    anonymous (manifest "acme" "features" "ghost") (Status 500) ]

              // Act
              let limitedOutcome, _ = run limited [ "acme/features/ghost" ]
              let brokenOutcome, _ = run broken [ "acme/features/ghost" ]

              // Assert
              Expect.equal
                  (limitedOutcome.Exit, limitedOutcome.Out, limitedOutcome.Err)
                  (1,
                   [],
                   [ "feature-probe: https://ghcr.io/token?scope=repository:acme/features/ghost:pull&service=ghcr.io answered HTTP 429" ])
                  "429 from the token endpoint"

              Expect.equal
                  (brokenOutcome.Exit, brokenOutcome.Out, brokenOutcome.Err)
                  (1,
                   [],
                   [ "feature-probe: https://raw.githubusercontent.com/acme/features/main/src/ghost/devcontainer-feature.json answered HTTP 500" ])
                  "500 from the manifest host"
          }

          test "an answer of the wrong shape stops the run instead of reading as empty" {
              // Arrange
              let listed =
                  [ anonymous (token "acme/features/odd") granted
                    bearing "pull-token" (tagList "acme/features/odd") (Body """{"name":"acme/features/odd"}""") ]

              // Act
              let outcome, _ = run listed [ "acme/features/odd" ]

              // Assert
              Expect.equal (outcome.Exit, outcome.Out) (1, []) "no report"

              Expect.stringStarts
                  (String.concat "\n" outcome.Err)
                  "feature-probe: https://ghcr.io/v2/acme/features/odd/tags/list answered something unreadable: "
                  "names the request"
          }

          test "--mine lists the directories under src/, naming a client to api.github.com" {
              // Arrange
              let listed =
                  [ anonymous
                        "https://api.github.com/repos/NicoVIII/devcontainer-features/contents/src"
                        (Body
                            """[{"name":"scrut","type":"dir"},{"name":"README.md","type":"file"},{"name":"just","type":"dir"}]""") ]

              // Act
              let outcome, handler = run listed [ "--mine" ]

              // Assert — without a User-Agent the API answers 403
              Expect.equal
                  outcome
                  { Exit = 0
                    Err = []
                    Out =
                      [ "ghcr.io/nicoviii/devcontainer-features/scrut"
                        "ghcr.io/nicoviii/devcontainer-features/just" ] }
                  "directories only"

              Expect.equal (List.ofSeq handler.UserAgents) [ "feature-probe" ] "one request, naming its client"
          }

          test "--base lists the newest exact-version tag per distro" {
              // Arrange — 2.1.10 after 2.1.8 is a version comparison, and the
              // floating tags are not candidates
              let listed =
                  [ anonymous
                        "https://mcr.microsoft.com/v2/devcontainers/base/tags/list"
                        (Body
                            """{"name":"devcontainers/base","tags":["latest","ubuntu","2","2.1","2.1.7-bookworm","2.1.8-bookworm","2.1.10-bookworm","2.1.8-trixie","1.0.9-ubuntu24.04","2.1.8-ubuntu24.04","dev-bookworm"]}""") ]

              // Act
              let outcome, _ = run listed [ "--base" ]

              // Assert
              Expect.equal
                  outcome
                  { Exit = 0
                    Err = []
                    Out = [ "2.1.10-bookworm"; "2.1.8-trixie"; "2.1.8-ubuntu24.04" ] }
                  "one tag per distro"
          }

          test "a call that asks for nothing recognisable is a usage error, before any request" {
              // Arrange
              let nothingListed = []

              // Act
              let none, _ = run nothingListed []
              let notARef, _ = run nothingListed [ "devcontainers/features/dotnet"; "dotnet" ]

              // Assert
              Expect.equal
                  none
                  { Exit = 2
                    Out = []
                    Err = [ "feature-probe: usage: feature-probe.sh <feature-ref>... | --mine | --base" ] }
                  "no arguments"

              Expect.equal
                  notARef
                  { Exit = 2
                    Out = []
                    Err = [ "feature-probe: not a feature ref: dotnet" ] }
                  "one bad ref among good ones"
          } ]
