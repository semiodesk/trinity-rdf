#!/usr/bin/env bash
# Claude Code PreToolUse hook, wired in .claude/settings.json for `git commit`: runs the local gates
# (.github/scripts/check.sh) before Claude commits, and puts the result in front of Claude either way.
#
# - A failing gate blocks the commit. Exit 2 is Claude Code's "deny", and what is written to stderr
#   is handed to Claude as the reason, so it sees what failed rather than just that something did.
# - A passing run still returns the report, as additional context. The changed-line findings -- an
#   uncovered line, an edit that reached one copy of duplicated code -- never fail a gate, and they
#   are the part most worth acting on before the commit is made.
#
# A change that touches only Markdown skips the run: nothing it could affect is measured.

cd "${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel)}" || exit 0

changed=$( { git diff --name-only HEAD; git ls-files --others --exclude-standard; } 2>/dev/null | grep -v '\.md$')
[ -z "$changed" ] && exit 0

report=$(.github/scripts/check.sh 2>&1)
status=$?

if [ "$status" -ne 0 ]; then
    printf 'Pre-commit checks failed, so the commit was blocked. Fix what is reported below and commit again.\n\n%s\n' \
        "$report" >&2
    exit 2
fi

printf '%s' "$report" | python3 -c '
import json, sys
report = sys.stdin.read()
print(json.dumps({"hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "additionalContext": "Pre-commit checks passed. Review the changed-line findings before relying on this commit:\n\n" + report,
}}))'
