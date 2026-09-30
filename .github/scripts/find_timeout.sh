# Sourced by .github/scripts/check.sh and .claude/hooks/pre-commit.sh.
#
# find_timeout prints the path of a GNU `timeout` that accepts --kill-after, and fails if there is
# none. Stock macOS has no `timeout`; Homebrew's coreutils installs it as `gtimeout`. BusyBox's
# accepts no long options, which the probe rejects rather than let --kill-after fail later.
find_timeout() {
    local candidate path
    for candidate in timeout gtimeout; do
        path=$(command -v "$candidate" 2> /dev/null) || continue
        if "$path" --kill-after=1 5 true 2> /dev/null; then
            echo "$path"
            return 0
        fi
    done
    return 1
}
