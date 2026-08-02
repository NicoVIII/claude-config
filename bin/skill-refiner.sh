#!/usr/bin/env bash
# Runs skill-refiner from wherever the caller stands, for the skills that log,
# rate and measure through it.
#
# `dotnet run --project` builds in the caller's working directory, and MSBuild's
# IncrementalClean then deletes the previous build's runtimeconfig.json — so the
# next invocation from anywhere else dies trying to launch a self-contained app.
# Building in the project directory and executing the assembly directly is what
# leaves the caller's cwd alone, which `log` reads to name the session's repo.
set -euo pipefail

here=$(dirname "${BASH_SOURCE[0]}")
project=$(cd "${here}/skill-refiner" && pwd)
output=${project}/bin/Debug/net10.0
assembly=${output}/skill-refiner.dll

# The runtimeconfig is checked beside the assembly, not assumed to be with it:
# an IncrementalClean from some other build takes that file and leaves the dll,
# which is the shape this whole script exists for.
build=no
if [[ ! -f ${assembly} || ! -f ${output}/skill-refiner.runtimeconfig.json ]]; then
	build=yes
else
	changed=$(find "${project}" -name '*.fs' -newer "${assembly}" -print -quit)
	if [[ -n ${changed} ]]; then
		build=yes
	fi
fi

# To stderr: stdout carries the entry skill-refiner echoes back, and a caller
# reading it should not have to skip a build banner first.
if [[ ${build} == yes ]]; then
	(cd "${project}" && dotnet build --nologo --verbosity quiet) >&2
fi

exec dotnet "${assembly}" "$@"
