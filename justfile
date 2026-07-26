default:
    @just --list

# Typecheck every F# project in the repo.
#
# The project list comes from the git index rather than a glob: ~/.claude is
# full of untracked state (session-env/, file-history/, other people's
# checkouts) that a `**/*.fsproj` walk would wander into. The index is exactly
# the allowlist .gitignore defines, and it already contains staged new projects,
# so a helper added in the same commit is checked before it lands.
typecheck:
    #!/usr/bin/env bash
    set -euo pipefail
    git ls-files -z '*.fsproj' | while IFS= read -r -d '' proj; do
        echo "==> $proj"
        dotnet build --nologo "$proj"
    done

# Run the test suites.
#
# Listed by hand, unlike typecheck's project list: a `*.tests` glob over the
# index would also match a suite in a foreign checkout that happened to be
# tracked here, and there are few enough of these to name.
test:
    dotnet run --project bin/runlog.tests
    dotnet run --project skills/merge-dependabot/survey.tests

# Lint and format-check every shell script in the repo.
#
# Same index-driven file list as typecheck, for the same reason, and `-r` so an
# empty list is a no-op rather than a tool invoked with no arguments.
#
# shellcheck runs with every optional check enabled (`-o all`), not just the
# default severity. The scripts here are few and short, so the style rules cost
# nothing to satisfy, and the opt-in ones are the ones with teeth: SC2312 is
# what caught `failing-log.sh` guarding a pipeline by an exit status `head`
# always made zero, leaving its fallback branch silently dead.
shell:
    #!/usr/bin/env bash
    set -euo pipefail
    git ls-files -z '*.sh' | xargs -0 -r shellcheck -o all -S style
    git ls-files -z '*.sh' | xargs -0 -r shfmt -d

# The single entry point lefthook and CI both call, so neither can drift from
# the other. Add new checks here, never to the workflow.
check: typecheck test shell
