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
# So settings.json runs this hook for every Bash command, with no `if` filter: that filter's
# permission-rule prefix matching never matched `git -C <dir> commit`, which therefore bypassed the
# hook entirely, and a second matcher that disagrees with the first is how that happened. A
# command that is not a commit costs one `case` statement below.
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
# The tree checked is the one the command commits in -- the hook input's cwd, moved by `cd` and
# git's -C/--work-tree -- not $CLAUDE_PROJECT_DIR. Judging the main checkout let a broken commit in
# another worktree through whenever the main one was clean, and blocked a clean one when it was not.
# Each tree is checked by its own check.sh, i.e. by the gates as they are in that tree.
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

# The check is bounded below the hook's own limit, which needs a GNU timeout. It is the hook's own
# prerequisite, not the check's, so its absence blocks (rule 2) and says what to install.
. "$hooks/../../.github/scripts/find_timeout.sh" || block "The pre-commit hook could not load find_timeout.sh."
timeout_cmd=$(find_timeout) ||
    block "The pre-commit hook needs GNU timeout to keep the check below its own time limit, and found none (on macOS: brew install coreutils, which installs it as gtimeout). The commit was blocked rather than let through unchecked."

# $decision lists the working trees the command commits in, one per line.
context=""
while IFS= read -r tree; do
    [ -n "$tree" ] || continue
    if [ ! -f "$tree/.github/scripts/check.sh" ]; then
        # Another repository altogether: this project's gates do not apply there. Said, not silent.
        context+="Not checked: $tree has no .github/scripts/check.sh, so it is not a working tree of this project."$'\n'
        continue
    fi
    cd "$tree" || block "The pre-commit hook could not enter $tree, so the commit was blocked unchecked."

    changed=$( { git diff --name-only HEAD; git ls-files --others --exclude-standard; } 2>/dev/null | grep -v '\.md$')
    if [ -z "$changed" ]; then
        context+="Pre-commit checks skipped in $tree: no change outside Markdown, and Markdown is not measured."$'\n'
        continue
    fi

    report=$("$timeout_cmd" --kill-after=15 840 bash .github/scripts/check.sh 2>&1)
    status=$?

    if [ "$status" -eq 124 ] || [ "$status" -eq 137 ]; then
        printf 'Pre-commit checks in %s did not finish within 840 s, so the commit was blocked rather than let through unchecked. Output so far:\n\n%s\n' \
            "$tree" "$report" >&2
        exit 2
    fi
    if [ "$status" -ne 0 ]; then
        printf 'Pre-commit checks in %s failed, so the commit was blocked. Fix what is reported below and commit again.\n\n%s\n' \
            "$tree" "$report" >&2
        exit 2
    fi
    context+="Pre-commit checks passed in $tree. Review the changed-line findings before relying on this commit:"$'\n\n'"$report"$'\n'
done <<< "$decision"

[ -z "$context" ] && exit 0
printf '%s' "$context" | python3 -c '
import json, sys
print(json.dumps({"hookSpecificOutput": {"hookEventName": "PreToolUse", "additionalContext": sys.stdin.read()}}))'
