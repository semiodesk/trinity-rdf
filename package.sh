#!/usr/bin/env bash
# Pack all six packages (Release) into ./artifacts.
#   ./package.sh      -> 2.0.0         (release, no suffix)
#   ./package.sh 3    -> 2.0.0-rc.3    (prerelease)
set -euo pipefail

if [ $# -gt 1 ] || { [ $# -eq 1 ] && ! [[ "$1" =~ ^[1-9][0-9]*$ ]]; }; then
    echo "usage: $0 [rc-number]   (a positive integer, e.g. 3 -> rc.3)" >&2
    exit 2
fi

# Always passed, empty for a release: the global property overrides a VersionSuffix
# inherited from the environment, so no stray label can end up in a release package.
suffix="-p:VersionSuffix=${1:+rc.$1}"

cd "$(dirname "$0")"

rm -rf ./artifacts

dotnet pack Trinity/Trinity.csproj                   -c Release -o ./artifacts "$suffix"
dotnet pack Trinity.Virtuoso/Trinity.Virtuoso.csproj -c Release -o ./artifacts "$suffix"
dotnet pack Trinity.Fuseki/Trinity.Fuseki.csproj     -c Release -o ./artifacts "$suffix"
dotnet pack Trinity.GraphDB/Trinity.GraphDB.csproj   -c Release -o ./artifacts "$suffix"
dotnet pack Trinity.Oxigraph/Trinity.Oxigraph.csproj -c Release -o ./artifacts "$suffix"
dotnet pack Trinity.Vocabulary.Cli/Trinity.Vocabulary.Cli.csproj -c Release -o ./artifacts "$suffix"
