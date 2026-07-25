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
test:
    dotnet run --project bin/runlog.tests

# The single entry point lefthook and CI both call, so neither can drift from
# the other. Add new checks here, never to the workflow.
check: typecheck test
