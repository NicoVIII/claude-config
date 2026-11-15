/// Check is the helper's whole verdict and pure over the parsed file and the
/// schema's constraints, so each finding is a table row here rather than a
/// fetch of the live schema per case. The YAML is flow style to keep a case on
/// one line; Config.parse reads it into the same tree as block style.
module CheckTests

open Expecto
open Domain
open Schema

/// A stand-in for what Schema.fetch reduces the published schema to: only the
/// names these cases need, so a finding below is traceable to one entry here.
let private schema =
    { RootKeys = set [ "version"; "updates"; "registries" ]
      UpdateKeys =
        set
            [ "package-ecosystem"
              "directory"
              "directories"
              "schedule"
              "groups"
              "multi-ecosystem-group" ]
      RequiredUpdateKeys = [ "package-ecosystem" ]
      GroupKeys = set [ "patterns"; "update-types" ]
      Ecosystems = [ "nuget"; "npm" ] }

let private check yaml = Config.parse yaml |> Check.config schema

/// A config whose one update entry has exactly the given fields.
let private withEntry fields =
    $"{{version: 2, updates: [{{%s{fields}}}]}}"

let private valid =
    "package-ecosystem: nuget, directory: /, schedule: {interval: weekly}"

let private nuget = Site "updates[0] (nuget)"

let tests =
    testList
        "Check.config"
        [ test "a config the schema allows has no findings" {
              // Arrange
              let yaml =
                  withEntry (
                      "package-ecosystem: nuget, directories: [/bin/*, /lib/*], schedule: {interval: weekly}, "
                      + "groups: {nuget: {patterns: ['*'], update-types: [minor, patch]}}"
                  )

              // Act & Assert
              Expect.equal (check yaml) [] "findings"
          }

          test "a multi-ecosystem-group stands in for the schedule" {
              // Act
              let findings =
                  check (withEntry "package-ecosystem: nuget, directory: /, multi-ecosystem-group: shared")

              // Assert
              Expect.equal findings [] "findings"
          }

          testList
              "root"
              [ test "a root that is not a mapping is the only finding" {
                    Expect.equal (check "[version, updates]") [ RootNotAMapping ] "findings"
                }

                test "a version other than 2 is named" {
                    Expect.equal
                        (check $"{{version: 1, updates: [{{%s{valid}}}]}}")
                        [ WrongVersion "1" ]
                        "findings"
                }

                test "an absent version reads as absent" {
                    Expect.equal (check $"{{updates: [{{%s{valid}}}]}}") [ WrongVersion "absent" ] "findings"
                }

                test "an empty or absent updates list is reported" {
                    for yaml in [ "{version: 2, updates: []}"; "{version: 2}" ] do
                        Expect.equal (check yaml) [ NoUpdates ] yaml
                }

                test "unknown root keys are reported in sorted order" {
                    // Act
                    let findings = check $"{{version: 2, zeta: 1, alpha: 1, updates: [{{%s{valid}}}]}}"

                    // Assert
                    Expect.equal
                        findings
                        [ UnknownKey(Site "root", "alpha"); UnknownKey(Site "root", "zeta") ]
                        "findings"
                } ]

          testList
              "update entry"
              [ test "an entry that is not a mapping is reported by index alone" {
                    Expect.equal (check "{version: 2, updates: [nuget]}") [ NotAMapping(Site "updates[0]") ] "findings"
                }

                test "an unknown key is reported at its entry" {
                    Expect.equal (check (withEntry (valid + ", interval: weekly"))) [ UnknownKey(nuget, "interval") ] "findings"
                }

                test "a missing required key is reported at an entry named by index" {
                    // Act
                    let findings = check (withEntry "directory: /, schedule: {interval: weekly}")

                    // Assert
                    Expect.equal findings [ MissingRequired(Site "updates[0]", "package-ecosystem") ] "findings"
                }

                test "an ecosystem the schema does not list is reported" {
                    // Act
                    let findings = check (withEntry "package-ecosystem: nugget, directory: /, schedule: {interval: weekly}")

                    // Assert
                    Expect.equal findings [ UnknownEcosystem(Site "updates[0] (nugget)", "nugget") ] "findings"
                }

                test "an entry with neither schedule nor multi-ecosystem-group is reported" {
                    Expect.equal (check (withEntry "package-ecosystem: nuget, directory: /")) [ MissingSchedule nuget ] "findings"
                }

                test "directory and directories are exactly one of two" {
                    // Arrange
                    let cases =
                        [ "package-ecosystem: nuget, schedule: {interval: weekly}", []
                          valid + ", directories: [/bin/*]", [ "directory"; "directories" ] ]

                    // Act & Assert
                    for fields, found in cases do
                        Expect.equal (check (withEntry fields)) [ DirectoryChoice(nuget, found) ] fields
                }

                test "a '**' glob is reported from either directory key" {
                    // Arrange
                    let cases =
                        [ "directory: /src/**"; "directories: [/bin/*, /src/**]" ]

                    // Act & Assert
                    for directory in cases do
                        let fields = $"package-ecosystem: nuget, schedule: {{interval: weekly}}, %s{directory}"
                        Expect.equal (check (withEntry fields)) [ UndocumentedGlob(nuget, "/src/**") ] fields
                }

                test "every entry is checked and named by its own index" {
                    // Act
                    let findings =
                        check $"{{version: 2, updates: [{{%s{valid}}}, {{package-ecosystem: npm, directory: /}}]}}"

                    // Assert
                    Expect.equal findings [ MissingSchedule(Site "updates[1] (npm)") ] "findings"
                } ]

          testList
              "group"
              [ test "an unknown key is reported at its group" {
                    // Act
                    let findings = check (withEntry (valid + ", groups: {all: {patterns: ['*'], update-type: [major]}}"))

                    // Assert
                    Expect.equal findings [ UnknownKey(Site "updates[0] (nuget) group 'all'", "update-type") ] "findings"
                }

                test "a group that is not a mapping is reported" {
                    // Act
                    let findings = check (withEntry (valid + ", groups: {all: ['*']}"))

                    // Assert
                    Expect.equal findings [ NotAMapping(Site "updates[0] (nuget) group 'all'") ] "findings"
                } ] ]
