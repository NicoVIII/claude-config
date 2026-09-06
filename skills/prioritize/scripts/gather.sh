#!/usr/bin/env bash
# The path prioritize names for its Gather step. See bin/dotnet-tool.sh for why
# the tools are reached this way, and not through `dotnet run`.
set -euo pipefail

here=$(dirname "${BASH_SOURCE[0]}")
exec "${here}/../../../bin/dotnet-tool.sh" "${here}/gather" "$@"
