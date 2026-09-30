#!/usr/bin/env bash
# Runs CI's gates locally and reports them against the change, so a commit sees what its own diff
# did: which changed lines no test covers, and which edits landed in one copy of duplicated code.
#
# Usage: .github/scripts/check.sh [<revision>]     (default HEAD: everything not yet committed)
#
# Fails when a gate CI enforces would fail: duplication ceiling, build, vocabulary check, a test --
# in-memory or store -- or the coverage floor. The changed-line findings are reported, never gated.
# It mirrors the `build` and `duplication` jobs in .github/workflows/ci.yml -- the suite list below
# must stay in step with the Test step there -- and runs the `stores` job's suites that the change
# affects (.github/scripts/stores.py decides which: any change to core or the shared fixtures affects
# all four).
#
# Store suites need Docker and their pinned images. When either is missing a suite is reported as not
# run, and its adapter's lines as not measured, rather than failing the check: CI runs it regardless,
# and pulling gigabytes with the output hidden is worse than saying so. Once a suite runs, its
# failures fail the check like any other test. There is deliberately no switch to skip them.
#
# Output is kept to what a reader acts on: dotnet's own logs are shown only for what failed.

set -uo pipefail
cd "$(git rev-parse --show-toplevel)" || exit 1

revision="${1:-HEAD}"
scripts=.github/scripts
logs=$(mktemp -d)
status=0

# Store suites run in the background; parallel arrays rather than an associative one, so this runs on
# the bash 3 macOS ships as well.
started=()
pids=()
trap 'for p in ${pids[@]+"${pids[@]}"}; do kill "$p" 2>/dev/null; done; rm -rf "$logs"' EXIT
# Without these, a signal (the hook's time limit, ^C) would end the script without the cleanup above.
trap 'exit 143' TERM
trap 'exit 130' INT

suites=(
    Trinity.Tests/Trinity.Tests.csproj
    tests/Trinity.Generator.Tests/Trinity.Generator.Tests.csproj
    tests/Trinity.Vocabulary.Tests/Trinity.Vocabulary.Tests.csproj
)

# The failing tests' names and messages, up to each stack trace; the log's tail if the run failed
# some other way (a crashed host prints no "Failed" block at all).
failures() {
    local excerpt
    excerpt=$(awk '/^  Failed /{p=1} /^  Stack Trace:/{p=0} p; /^Failed!/' "$1" | head -60)
    echo "${excerpt:-$(tail -20 "$1")}"
}

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

# Reports from an earlier run were compiled from older source; merged in, they would misplace every
# line that has moved since. Both directories start empty.
rm -rf coverage coverage-stores

# Store suites start now, before the in-memory suites, so the two overlap. They need only the build.
affected=()
while IFS= read -r store; do
    [ -n "$store" ] && affected+=("$store")
done < <(python3 "$scripts/stores.py" affected "$revision")

if [ ${#affected[@]} -gt 0 ]; then
    docker_missing=""
    timeout 10 docker info > /dev/null 2>&1 || docker_missing="Docker is not available"

    for store in "${affected[@]}"; do
        mkdir -p "coverage-stores/$store"
        why="$docker_missing"
        if [ -z "$why" ]; then
            for image in $(python3 "$scripts/stores.py" images "$store"); do
                if ! docker image inspect "$image" > /dev/null 2>&1; then
                    why="image $image is not pulled; docker pull $image"
                    break
                fi
            done
        fi
        if [ -n "$why" ]; then
            echo "not run: $why" > "coverage-stores/$store/outcome"
            continue
        fi

        timeout 300 dotnet test "tests/Trinity.Tests.$store/Trinity.Tests.$store.csproj" -c Release --no-build \
            --nologo --collect "Code Coverage;Format=Cobertura" --results-directory "coverage-stores/$store" \
            > "$logs/store-$store.txt" 2>&1 &
        started+=("$store")
        pids+=("$!")
    done
fi

echo
echo "## Tests"
for suite in "${suites[@]}"; do
    log="$logs/test-$(basename "$suite" .csproj).txt"
    if dotnet test "$suite" -c Release --no-build --nologo \
            --collect "Code Coverage;Format=Cobertura" --results-directory ./coverage > "$log" 2>&1; then
        grep -E '^Passed!' "$log"
    else
        failures "$log"
        status=1
    fi
done

if [ ${#affected[@]} -gt 0 ]; then
    echo
    echo "## Store suites"
    for store in "${affected[@]}"; do
        if [ -f "coverage-stores/$store/outcome" ]; then
            echo "$store: $(cat "coverage-stores/$store/outcome")"
        fi
    done
    for i in "${!started[@]}"; do
        store="${started[$i]}"
        log="$logs/store-$store.txt"
        wait "${pids[$i]}"
        code=$?
        if [ "$code" -eq 0 ]; then
            echo success > "coverage-stores/$store/outcome"
            echo "$store: $(grep -E '^Passed!' "$log")"
        elif [ "$code" -eq 124 ]; then
            echo "timed out after 300 s" > "coverage-stores/$store/outcome"
            echo "$store: timed out after 300 s -- a container that never became ready, or a hang."
            status=1
        else
            echo failure > "coverage-stores/$store/outcome"
            echo "$store:"
            failures "$log"
            status=1
        fi
    done
    pids=()
fi

echo
echo "## Coverage"
python3 "$scripts/coverage.py" ./coverage --stores ./coverage-stores --diff "$revision" || status=1

echo
if [ "$status" -eq 0 ]; then
    echo "All gates pass."
else
    echo "A gate failed; see above."
fi
exit "$status"
