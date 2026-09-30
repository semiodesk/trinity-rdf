#!/usr/bin/env bash
# Claude Code PreToolUse hook, wired in .claude/settings.json: runs the local gates
# (.github/scripts/check.sh) before Claude commits, and puts the result in front of Claude either way.
#
# - A failing gate blocks the commit. Exit 2 is Claude Code's "deny", and what is written to stderr
#   is handed to Claude as the reason, so it sees what failed rather than just that something did.
# - A passing run still returns the report, as additional context. The changed-line findings -- an
#   uncovered line, an edit that reached one copy of duplicated code -- never fail a gate, and they
#   are the part most worth acting on before the commit is made.
#
# Whether a command commits is decided in one place, .github/scripts/commit_command.py, from the
# command's tokens: its exit status is 0 (commits), 1 (does not) or anything else (could not tell).
# Three rules follow, and every exit below is one of them:
#
# 1. A command that does not commit is never blocked.
# 2. A commit is never let through silently. When the check cannot run -- the hook's own
#    prerequisites are missing, the command cannot be understood -- the commit is blocked with a
#    message naming what is missing: a blocked command can be rephrased or retried, an unchecked
#    commit cannot be taken back.
# 3. A missing *optional* tool of the check itself (Docker, npx) is check.sh's business: it reports
#    "not run: <reason>" and carries on, and CI still enforces those gates.
#
# The check runs under its own time limit, below the 900 s in settings.json, because a hook that
# outlives its timeout is killed and the tool call proceeds -- silently, with no report. A hang would
# otherwise turn into a commit nothing checked. Exceeding this limit blocks instead. `timeout`
# signals its whole process group, so the dotnet processes underneath stop too.
#
# A change that touches only Markdown skips the run: nothing it could affect is measured.

input=$(cat)

block() {
    printf '%s\n' "$1" >&2
    exit 2
}

# A cheap first cut, before starting python: a command that mentions neither `commit` nor
# `--continue` cannot be one this hook checks, and most commands end here.
case "$input" in
    *commit* | *--continue*) ;;
    *) exit 0 ;;
esac

command -v python3 > /dev/null 2>&1 ||
    block "The pre-commit hook needs python3 to tell whether this command commits, and python3 was not found. The command was blocked rather than let a commit through unchecked. Install python3, or disable the hook via /hooks."

hooks=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd) || block "The pre-commit hook could not locate itself."
decision=$(printf '%s' "$input" | python3 "$hooks/../../.github/scripts/commit_command.py")
case $? in
    0) ;;
    1) exit 0 ;;
    *) block "The pre-commit hook could not tell whether this command commits: ${decision:-python3 gave no reason}. It was blocked rather than risk an unchecked commit; run the commit as a plain \`git commit\` command." ;;
esac

cd "${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel)}" ||
    block "The pre-commit hook could not enter the project directory, so the commit was blocked unchecked."

changed=$( { git diff --name-only HEAD; git ls-files --others --exclude-standard; } 2>/dev/null | grep -v '\.md$')
[ -z "$changed" ] && exit 0

report=$(timeout --kill-after=15 840 .github/scripts/check.sh 2>&1)
status=$?

if [ "$status" -eq 124 ] || [ "$status" -eq 137 ]; then
    printf 'Pre-commit checks did not finish within 840 s, so the commit was blocked rather than let through unchecked. Output so far:\n\n%s\n' \
        "$report" >&2
    exit 2
fi

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
