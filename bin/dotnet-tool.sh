#!/usr/bin/env bash
# Runs one of this repo's F# tools, named by its project directory, from
# wherever the caller stands.
#
# Every tool is reached through a wrapper beside its project rather than a
# `dotnet run` line in the SKILL.md that calls it, for two reasons.
#
# `dotnet run --project` builds in the caller's working directory, and MSBuild's
# IncrementalClean then deletes the previous build's runtimeconfig.json — so the
# next invocation from anywhere else dies trying to launch a self-contained app.
# Building in the project directory and executing the assembly directly is what
# leaves the caller's cwd alone, which `skill-refiner log` reads to name the
# session's repo and `survey` reads to pick the repo it surveys.
#
# And the invocation a skill names is a contract with that skill's prose. Naming
# a wrapper keeps that contract one path, so a tool that grows, moves or is
# rewritten in another language changes here rather than in every SKILL.md that
# calls it.
set -euo pipefail

project=$(cd "$1" && pwd)
shift

# No project sets AssemblyName, so the assembly is the directory's name.
name=$(basename "${project}")
output=${project}/bin/Debug/net10.0
assembly=${output}/${name}.dll

# The directories the assembly is built from: the project's own, plus any
# project it references. lib/gh is the only such reference in the repo and has
# none of its own, so resolving one level is enough — without it, editing
# lib/gh/Gh.fs leaves gather and survey silently running the previous build.
references=$(sed -n 's/.*ProjectReference Include="\([^"]*\)".*/\1/p' "${project}"/*.fsproj)

watched=("${project}")
if [[ -n ${references} ]]; then
	while IFS= read -r reference; do
		watched+=("$(cd "${project}/$(dirname "${reference}")" && pwd)")
	done <<<"${references}"
fi

# The runtimeconfig is checked beside the assembly, not assumed to be with it:
# an IncrementalClean from some other build takes that file and leaves the dll,
# which is the shape this whole script exists for.
build=no
if [[ ! -f ${assembly} || ! -f ${output}/${name}.runtimeconfig.json ]]; then
	build=yes
else
	# .fsproj too, not just sources: a new Compile entry or a bumped package
	# changes what the assembly should contain without touching any watched .fs.
	changed=$(find "${watched[@]}" \( -name '*.fs' -o -name '*.fsproj' \) -newer "${assembly}" -print -quit)
	if [[ -n ${changed} ]]; then
		build=yes
	fi
fi

# To stderr: stdout carries what the tool prints, and a caller reading it should
# not have to skip a build banner first.
if [[ ${build} == yes ]]; then
	(cd "${project}" && dotnet build --nologo --verbosity quiet) >&2
fi

exec dotnet "${assembly}" "$@"
