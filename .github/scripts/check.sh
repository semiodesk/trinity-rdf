#!/usr/bin/env bash
# Runs CI's gates locally and reports them against the change, so a commit sees what its own diff
# did: which changed lines no test covers, and which edits landed in one copy of duplicated code.
#
# Usage: .github/scripts/check.sh [<revision>]     (default HEAD: everything not yet committed)
#
# Fails when a gate CI enforces would fail: duplication ceiling, build, vocabulary check, a test, or
# the coverage floor. The changed-line findings are reported, never gated. It mirrors the `build` and
# `duplication` jobs in .github/workflows/ci.yml -- the suite list below must stay in step with the
# Test step there -- but not the `stores` job, which needs Docker and minutes per backend.
#
# Output is kept to what a reader acts on: dotnet's own logs are shown only for what failed.

set -uo pipefail
cd "$(git rev-parse --show-toplevel)" || exit 1

revision="${1:-HEAD}"
scripts=.github/scripts
logs=$(mktemp -d)
trap 'rm -rf "$logs"' EXIT
status=0

suites=(
    Trinity.Tests/Trinity.Tests.csproj
    tests/Trinity.Generator.Tests/Trinity.Generator.Tests.csproj
    tests/Trinity.Vocabulary.Tests/Trinity.Vocabulary.Tests.csproj
)

# First, because it needs no build: its findings arrive even when the build is what fails.
echo "## Duplication"
python3 "$scripts/duplication.py" --diff "$revision" || status=1

echo
echo "## Build"
if ! dotnet build Semiodesk.Trinity.sln -c Release -nologo -v q > "$logs/build.txt" 2>&1; then
    grep -E ': error ' "$logs/build.txt" | sort -u | head -20
    echo "Build failed; nothing further can run."
    exit 1
fi
echo "Build succeeded."

if ! dotnet run --project Trinity.Vocabulary.Cli/Trinity.Vocabulary.Cli.csproj -c Release --no-build -- \
        Trinity.Tests/Ontologies/vocabularies.json --check > "$logs/vocab.txt" 2>&1; then
    cat "$logs/vocab.txt"
    echo "Committed vocabularies are stale; regenerate them with trinity-vocab."
    status=1
fi

echo
echo "## Tests"
rm -rf coverage
for suite in "${suites[@]}"; do
    if dotnet test "$suite" -c Release --no-build --nologo \
            --collect "Code Coverage;Format=Cobertura" --results-directory ./coverage > "$logs/test.txt" 2>&1; then
        grep -E '^Passed!' "$logs/test.txt"
    else
        # Each failing test's name and message, up to its stack trace; the log's tail if the run
        # failed some other way (a crashed host prints no "Failed" block at all).
        failures=$(awk '/^  Failed /{p=1} /^  Stack Trace:/{p=0} p; /^Failed!/' "$logs/test.txt" | head -60)
        echo "${failures:-$(tail -20 "$logs/test.txt")}"
        status=1
    fi
done

echo
echo "## Coverage"
python3 "$scripts/coverage.py" ./coverage --diff "$revision" || status=1

echo
if [ "$status" -eq 0 ]; then
    echo "All gates pass."
else
    echo "A gate failed; see above."
fi
exit "$status"
