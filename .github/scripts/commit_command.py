#!/usr/bin/env python3
"""Decide whether a shell command creates a commit, and in which working tree.

Usage (from .claude/hooks/pre-commit.sh): commit_command.py < hook-input.json

  exit 0  the command commits; stdout lists the working trees' top levels, one per line
  exit 1  the command does not commit
  exit 2  could not decide; stdout says why. The hook blocks: a commit must never pass unchecked,
          and a blocked command can be rephrased, while an unchecked commit cannot be taken back.

This is the only place the decision is made. It used to be two matchers that disagreed -- the
hook's `if` filter (prefix matching, so `git -C <dir> commit` never reached the hook at all) and a
text search (which matched `git commit` inside quotes and heredocs, and missed `/usr/bin/git
commit` and options taking a separate argument). So the command is tokenized, not searched:

- it is split into simple commands at `;`, `&&`, `||`, `|`, `&`, parentheses and newlines outside
  quotes; heredoc bodies are removed first, since they are data;
- `$(...)` and backticks, and the string passed to `bash -c`/`sh -c`, are followed as commands;
- leading assignments and wrappers (`env`, `command`, `exec`, `nice`, `nohup`, `time`) are skipped;
- argv[0] must be `git` by basename, its global options are skipped -- including those taking a
  separate argument (-C, -c, --git-dir, --work-tree, --namespace, ...) -- and the first remaining
  word is the subcommand.

Which commands count: `git commit`, and the --continue forms of merge, cherry-pick, revert, rebase
and am, which commit a conflict resolution already in the working tree. The plain forms produce
their tree only when they run, after a PreToolUse hook; there is nothing yet to check.

Which tree: the hook input's `cwd`, moved by any `cd`/`pushd` earlier in the command and by git's
`-C` (cumulative) or `--work-tree`, resolved with `git rev-parse --show-toplevel`. Judging the main
checkout instead let a broken commit in another worktree through whenever the main one was clean.

Known limits, deliberately not followed: a subcommand spelled through a variable (`git $sub`),
aliases, functions and eval. A command that cannot be tokenized at all -- unbalanced quotes, which
bash rejects as well -- is undecidable (exit 2) if it mentions a commit, and otherwise not one.
"""
import json
import os
import re
import shlex
import subprocess
import sys

CONTINUE_COMMITS = {"merge", "cherry-pick", "revert", "rebase", "am"}
WRAPPERS = {"env", "command", "exec", "nice", "nohup", "time", "builtin"}
SHELLS = {"bash", "sh", "zsh", "dash"}
SEPARATORS = {";", "&&", "||", "|", "|&", "&", "(", ")", "\n", ";;", "{", "}", "!"}
REDIRECTS = {">", ">>", "<", "<<<", ">&", "<&", "&>", "&>>", ">|", "<>"}

# Global options of git that take their value as the next word.
GIT_OPTIONS_WITH_VALUE = {"-C", "-c", "--git-dir", "--work-tree", "--namespace", "--super-prefix",
                          "--config-env", "--attr-source"}

_ASSIGNMENT = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*=")
_HEREDOC = re.compile(r"(?<!<)<<(-?)[ \t]*(['\"]?)([A-Za-z_][A-Za-z0-9_]*)\2")


class Undecidable(Exception):
    pass


def strip_heredocs(command):
    """The command without heredoc bodies: they are input to a command, not commands."""
    out, pending = [], []
    for line in command.split("\n"):
        if pending:
            strip_tabs, delimiter = pending[0]
            if (line.lstrip("\t") if strip_tabs else line) == delimiter:
                pending.pop(0)
            continue
        out.append(line)
        pending += [(dash == "-", word) for dash, _, word in _HEREDOC.findall(line)]
    return "\n".join(out)


def substitutions(text):
    r"""Commands inside `$(...)` and backticks, outermost first.

    Scanned in the raw text, not in shlex's words: shlex removes the quotes, and the quotes decide.
    Bash runs a substitution outside quotes and inside double quotes, never inside single quotes --
    `--body "... \`git commit\`"` really does commit, `--body '... \`git commit\`'` does not.
    """
    found, i, quote = [], 0, None
    while i < len(text):
        c = text[i]
        if quote == "'":
            quote = None if c == "'" else quote
            i += 1
        elif c == "\\":
            i += 2
        elif c == "'" and quote is None:
            quote = "'"
            i += 1
        elif c == '"':
            quote = None if quote == '"' else '"'
            i += 1
        elif text.startswith("$(", i) and not text.startswith("$((", i):
            depth, j = 1, i + 2
            while j < len(text) and depth:
                depth += {"(": 1, ")": -1}.get(text[j], 0)
                j += 1
            found.append(text[i + 2:j - 1])
            i = j
        elif c == "`":
            j = text.find("`", i + 1)
            if j < 0:
                break
            found.append(text[i + 1:j])
            i = j + 1
        else:
            i += 1
    return found


def tokens(command):
    # A newline outside quotes separates commands, so it is punctuation here, not whitespace.
    lexer = shlex.shlex(command, posix=True, punctuation_chars=";&|()<>\n")
    lexer.whitespace = " \t\r"
    lexer.whitespace_split = True
    lexer.commenters = ""
    return list(lexer)


def simple_commands(words):
    command = []
    skip_next = False
    for word in words:
        if skip_next:
            skip_next = False
            continue
        # shlex joins adjacent punctuation (`;\n`, `&&`, `|&`), so separators are recognised by shape.
        if word in SEPARATORS or (word and set(word) <= set(";&|()\n")):
            if command:
                yield command
            command = []
        elif word in REDIRECTS or re.fullmatch(r"\d*[<>]+&?\d*", word):
            skip_next = not re.fullmatch(r"\d*[<>]&\d+", word)  # `2>&1` has no separate target
        else:
            command.append(word)
    if command:
        yield command


def commits(command, cwd, depth=0):
    """The directories in which `command` would create a commit (unresolved, possibly several)."""
    if depth > 5:
        raise Undecidable("the command nests shells or substitutions too deeply to follow")
    text = strip_heredocs(command)
    try:
        words = tokens(text)
    except ValueError as error:
        raise Undecidable(f"it could not be parsed ({error})")

    found, here = [], cwd
    for inner in substitutions(text):
        found += commits(inner, here, depth + 1)

    for argv in simple_commands(words):
        while argv and (_ASSIGNMENT.match(argv[0]) or argv[0] in WRAPPERS):
            argv = argv[1:]
            while argv and argv[0].startswith("-") and argv[0] != "-":  # e.g. `env -i`, `nice -n 5`
                argv = argv[1:] if argv[0] not in ("-n", "-u") else argv[2:]
        if not argv:
            continue
        name = os.path.basename(argv[0])

        if name in ("cd", "pushd"):
            target = next((a for a in argv[1:] if not a.startswith("-")), os.path.expanduser("~"))
            here = os.path.normpath(os.path.join(here, os.path.expanduser(target)))
        elif name in SHELLS and "-c" in argv[1:-1]:
            found += commits(argv[argv.index("-c") + 1], here, depth + 1)
        elif name in ("git", "git.exe"):
            where = git_commit_tree(argv[1:], here)
            if where:
                found.append(where)
    # An unquoted $(...) is seen twice -- by the substitution scan and as parenthesised words.
    return list(dict.fromkeys(found))


def git_commit_tree(args, here):
    """If `git <args>` commits, the directory it commits in; otherwise None."""
    work_tree = None
    i = 0
    while i < len(args):
        arg = args[i]
        if arg in GIT_OPTIONS_WITH_VALUE:
            if i + 1 >= len(args):
                return None
            value = args[i + 1]
            if arg == "-C":
                here = os.path.normpath(os.path.join(here, os.path.expanduser(value)))
            elif arg == "--work-tree":
                work_tree = os.path.normpath(os.path.join(here, os.path.expanduser(value)))
            i += 2
        elif arg.startswith("--work-tree="):
            work_tree = os.path.normpath(os.path.join(here, os.path.expanduser(arg.split("=", 1)[1])))
            i += 1
        elif arg.startswith("-"):
            i += 1  # a flag, or --option=value
        else:
            break
    else:
        return None

    subcommand, rest = args[i], args[i + 1:]
    if subcommand == "commit" or (subcommand in CONTINUE_COMMITS and "--continue" in rest):
        return work_tree or here
    return None


def top_level(directory):
    result = subprocess.run(["git", "-C", directory, "rev-parse", "--show-toplevel"],
                            capture_output=True, encoding="utf-8", errors="replace")
    if result.returncode != 0:
        raise Undecidable(f"{directory} is not inside a git working tree")
    return result.stdout.strip()


def main():
    try:
        hook = json.load(sys.stdin)
        command = hook.get("tool_input", {}).get("command")
        if not isinstance(command, str):
            raise Undecidable("the hook input carries no command string")
        cwd = hook.get("cwd") or os.getcwd()
        if not isinstance(cwd, str):
            raise Undecidable("the hook input's cwd is not a string")

        try:
            directories = commits(command, cwd)
        except Undecidable:
            if "commit" in command or "--continue" in command:
                raise
            return 1
        if not directories:
            return 1

        trees = []
        for directory in directories:
            tree = top_level(directory)
            if tree not in trees:
                trees.append(tree)
        print("\n".join(trees))
        return 0
    except Undecidable as reason:
        print(reason)
        return 2
    except (ValueError, AttributeError) as error:  # malformed hook input
        print(f"the hook input could not be read ({error})")
        return 2


if __name__ == "__main__":
    sys.exit(main())
