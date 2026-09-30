#!/usr/bin/env bash
# Runs CI's gates locally and reports them against the change, so a commit sees what its own diff
# did: which changed lines no test covers, and which edits landed in one copy of duplicated code.
#
# Usage: .github/scripts/check.sh [<revision>]     (default HEAD: everything not yet committed)
#
# Fails when a gate CI enforces would fail: duplication ceiling, build, vocabulary check, a test --
# in-memory or store -- or the coverage floor. The changed-line findings are reported, never gated.
# It mirrors the `build`, `duplication` and `scripts` jobs in .github/workflows/ci.yml -- the suite list below
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
status=0

# One check per checkout at a time: two would share coverage/, coverage-stores/ and duplication/.
# The lock records this script's PID and, below, each store suite's. A run killed outright (the
# hook's time limit escalating to KILL) never reaches its EXIT trap, and its store suites -- each in
# its own `timeout` process group -- keep writing into coverage-stores/ for minutes; the next run,
# which starts by clearing that directory, would race them. So a stale lock is not just removed: its
# suites are stopped first. A recorded PID is only acted on while its command line is still ours,
# since PIDs are reused.
lock=.check-lock
ours() {
    # ps is absent or different on some platforms (Git Bash); fall back to "alive" there.
    local args
    args=$(ps -o args= -p "$1" 2> /dev/null) || { kill -0 "$1" 2> /dev/null; return; }
    case "$args" in *"$2"*) return 0 ;; *) return 1 ;; esac
}
if ! mkdir "$lock" 2> /dev/null; then
    owner=$(cat "$lock/pid" 2> /dev/null)
    if [ -n "$owner" ] && ours "$owner" check.sh; then
        echo "Another check is already running in this checkout (pid $owner); it shares this run's"
        echo "output directories, so this one did not start. Wait for it, or stop it, and run again."
        exit 1
    fi
    for p in $(cat "$lock/stores" 2> /dev/null); do
        ours "$p" "dotnet test" && kill "$p" 2> /dev/null && echo "Stopped store suite $p, left running by an earlier check."
    done
    rm -rf "$lock"
    mkdir "$lock" || { echo "Could not take the check lock at $lock."; exit 1; }
fi
echo $$ > "$lock/pid"
logs=$(mktemp -d)

# Store suites run in the background; parallel arrays rather than an associative one, so this runs on
# the bash 3 macOS ships as well.
started=()
pids=()
trap 'for p in ${pids[@]+"${pids[@]}"}; do kill "$p" 2>/dev/null; done; rm -rf "$logs" "$lock"' EXIT
# Without these, a signal (the hook's time limit, ^C) would end the script without the cleanup above.
trap 'exit 143' TERM
trap 'exit 130' INT

# Store suites run under a GNU `timeout` (see find_timeout.sh). Without one they are reported as not
# run for that reason -- it must not masquerade as a missing Docker, which is what `timeout 10 docker
# info` failing used to report.
. "$scripts/find_timeout.sh"
timeout_cmd=$(find_timeout) || timeout_cmd=""

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
echo "## Script tests"
if python3 -m unittest discover -s "$scripts" > "$logs/scripts.txt" 2>&1; then
    tail -1 "$logs/scripts.txt"
else
    grep -E '^(FAIL|ERROR):|Error:|^Ran ' "$logs/scripts.txt" | head -20
    status=1
fi

echo
echo "## Build"
if ! dotnet build Semiodesk.Trinity.sln -c Release -nologo -v q > "$logs/build.txt" 2>&1; then
    # Compiler errors if there are any; otherwise the log's tail. An SDK that cannot be found, an
    # MSBuild node crash or a missing dotnet print no ": error " line at all, and a bare "Build
    # failed" leaves whoever reads a blocked commit nothing to act on.
    errors=$(grep -E ': error ' "$logs/build.txt" | sort -u | head -20)
    echo "${errors:-$(tail -20 "$logs/build.txt")}"
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
    if [ -z "$timeout_cmd" ]; then
        docker_missing="GNU timeout is not installed (on macOS: brew install coreutils)"
    elif ! "$timeout_cmd" 10 docker info > /dev/null 2>&1; then
        docker_missing="Docker is not available"
    fi

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

        # --kill-after: a suite that ignores TERM would otherwise outlive its 300 s indefinitely.
        "$timeout_cmd" --kill-after=15 300 dotnet test "tests/Trinity.Tests.$store/Trinity.Tests.$store.csproj" \
            -c Release --no-build --nologo --collect "Code Coverage;Format=Cobertura" \
            --results-directory "coverage-stores/$store" > "$logs/store-$store.txt" 2>&1 &
        started+=("$store")
        pids+=("$!")
        echo "$!" >> "$lock/stores"
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
