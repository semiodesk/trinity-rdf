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
# The `if` filter in settings.json is only a cheap first cut, not the decision. Claude Code documents
# it as best-effort: a command using a shell variable it cannot resolve runs the hook regardless. It
# was seen firing on a compound command with no `git commit` in it -- and since a failing gate blocks
# the tool call, it would have refused an unrelated command. So the command is checked here as well,
# and anything that does not invoke `git ... commit` passes straight through.
#
# The check runs under its own time limit, below the 900 s in settings.json, because a hook that
# outlives its timeout is killed and the tool call proceeds -- silently, with no report. A hang would
# otherwise turn into a commit nothing checked. Exceeding this limit blocks instead. `timeout`
# signals its whole process group, so the dotnet processes underneath stop too.
#
# A change that touches only Markdown skips the run: nothing it could affect is measured.

python3 -c '
import json, re, sys
command = json.load(sys.stdin).get("tool_input", {}).get("command", "")
# git, then any global options (-C <dir>, -c <k=v>, --no-pager, ...), then the commit subcommand.
invocation = r"(?:^|[\s;&|(`])git(?:\s+(?:-[Cc]\s+\S+|--?[\w-]+(?:=\S+)?))*\s+commit(?![\w-])"
sys.exit(0 if re.search(invocation, command) else 1)
' || exit 0

cd "${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel)}" || exit 0

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
