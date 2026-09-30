"""Which lines a change touches, for reports about the change rather than about the whole codebase.

A change is everything between a revision and the working tree, untracked files included:

- Locally the revision is HEAD, i.e. whatever is not committed yet. Untracked files count because a
  pre-commit check runs before `git add x && git commit` has staged x, and because the tests the
  report is about ran against the working tree, not the index.
- On a pull request CI checks out a merge commit whose first parent is the base branch, so HEAD^1
  is exactly the pull request's change.

A change has two sides and each report needs a different one. Coverage asks about lines as they are
now, so it wants the *new* side. Duplication asks whether the change edited one copy of code that
was duplicated *before* it -- and an edit to one copy is precisely what stops the two matching, so
only the *old* side still shows the clone the edit landed in.

Paths are repo-relative with forward slashes, the form both jscpd and git print.
"""
import json
import os
import re
import subprocess
from collections import namedtuple

Hunk = namedtuple("Hunk", "old_path old_start old_count new_path new_start new_count")

_HUNK = re.compile(r"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@")


def git(*args, text=False, **kwargs):
    """Run git and return its output; bytes, or with text=True a string.

    Text is decoded as UTF-8 with replacement, never in the locale's encoding and never strictly: a
    diff carries file *content*, and one changed line in a non-UTF-8 file (Documentation/api/index.md
    has a cp1252 en-dash) otherwise raised UnicodeDecodeError -- crashing a report that is meant never
    to gate, and failing the ceiling check that runs in the same script. Only paths and line numbers
    are read from the output, so a replaced character costs nothing.
    """
    if text:
        kwargs.update(encoding="utf-8", errors="replace")
    # Always from the top level. `git ls-files` prints paths relative to the working directory while
    # diff output is root-relative, so a script run from a subdirectory silently dropped every file
    # and reported 0.0% coverage with nothing pointing at paths.
    kwargs.setdefault("cwd", repo_root())
    return subprocess.run(["git", "-c", "core.quotePath=false", *args],
                          capture_output=True, check=True, **kwargs).stdout


_root = None


def repo_root():
    global _root
    if _root is None:
        _root = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, check=True,
                               encoding="utf-8").stdout.strip()
    return _root


def untracked_files():
    """Untracked files git does not ignore, repo-relative. A pre-commit check runs before `git add`."""
    return [p for p in git("ls-files", "--others", "--exclude-standard", "-z", text=True).split("\0") if p]


def known_files():
    """Every file git knows about: tracked, or untracked and not ignored."""
    listed = git("ls-files", "--cached", "--others", "--exclude-standard", "-z", text=True)
    return {p for p in listed.split("\0") if p}


def _glob(pattern):
    """A .jscpd.json ignore glob as a regex: `**/` spans directories, `*` stays within one."""
    out, i = "", 0
    while i < len(pattern):
        if pattern.startswith("**/", i):
            out, i = out + "(?:.*/)?", i + 3
        elif pattern.startswith("**", i):
            out, i = out + ".*", i + 2
        elif pattern[i] == "*":
            out, i = out + "[^/]*", i + 1
        else:
            out, i = out + re.escape(pattern[i]), i + 1
    return re.compile(out + r"\Z")


_non_product = None


def is_product_source(path):
    """Product C#, by the same rule the duplication scan uses: a .cs file .jscpd.json does not ignore.

    Read from .jscpd.json rather than kept beside it. The copy that used to live here was "kept in
    step" with that file and had already drifted from it -- it lacked `**/*.g.cs` -- so coverage and
    duplication could disagree about what product code is.
    """
    global _non_product
    if _non_product is None:
        with open(os.path.join(repo_root(), ".jscpd.json"), encoding="utf-8") as handle:
            _non_product = [_glob(g) for g in json.load(handle).get("ignore", [])]
    return path.endswith(".cs") and not any(g.match(path) for g in _non_product)


def _path(header):
    path = header[4:].rstrip("\t")
    return None if path == "/dev/null" else path[2:]


def hunks(revision):
    """Every hunk between `revision` and the working tree. Untracked files are one all-new hunk."""
    old_path = new_path = None
    diff = git("diff", "--unified=0", "--no-color", "--no-ext-diff",
               "--src-prefix=a/", "--dst-prefix=b/", revision, text=True)

    # A hunk's content is exactly old_count + new_count lines, and those are consumed as content:
    # a removed line reading `-- x` is `--- x` in the diff, and taken for a file header it renamed the
    # file mid-diff, so every later hunk in it was matched against the wrong base file. Headers are
    # recognised only between a `diff --git` line and the first hunk.
    in_header = False
    content = 0
    for line in diff.splitlines():
        if content:
            if line.startswith("\\"):  # "\ No newline at end of file" is not counted
                continue
            content -= 1
            continue

        if line.startswith("diff --git "):
            in_header = True
            old_path = new_path = None
        elif in_header and line.startswith("--- "):
            old_path = _path(line)
        elif in_header and line.startswith("+++ "):
            new_path = _path(line)
        else:
            match = _HUNK.match(line)
            if match:
                in_header = False
                count = lambda g: int(g) if g is not None else 1
                hunk = Hunk(old_path, int(match.group(1)), count(match.group(2)),
                            new_path, int(match.group(3)), count(match.group(4)))
                content = hunk.old_count + hunk.new_count
                yield hunk

    root = repo_root()
    for path in untracked_files():
        try:
            with open(os.path.join(root, path), encoding="utf-8", errors="replace") as handle:
                count = sum(1 for _ in handle)
        except OSError:
            continue
        if count:
            yield Hunk(None, 0, 0, path, 1, count)


def added_lines(all_hunks):
    """New side: file -> the lines, as they are now, that the change added or modified."""
    lines = {}
    for h in all_hunks:
        if h.new_path and h.new_count:
            lines.setdefault(h.new_path, set()).update(range(h.new_start, h.new_start + h.new_count))
    return lines


def touched_old_lines(hunk):
    """Old side of one hunk: the lines it removed or modified, as they were at the revision.

    A pure insertion removes nothing, so it touches the two lines either side of where it went in --
    adding a missing check inside one copy of duplicated code is the commonest one-sided fix.
    """
    if not hunk.old_path:
        return set()
    if hunk.old_count:
        return set(range(hunk.old_start, hunk.old_start + hunk.old_count))
    return {n for n in (hunk.old_start, hunk.old_start + 1) if n > 0}


def ranges(lines, joinable=lambda gap: False):
    """Collapse line numbers into (first, last) runs.

    `joinable(n)` says whether line n may sit inside a run without being in it -- a blank line or a
    brace between two uncovered statements should not split them into two findings.
    """
    runs = []
    for n in sorted(lines):
        if runs and all(joinable(g) for g in range(runs[-1][1] + 1, n)):
            runs[-1][1] = n
        else:
            runs.append([n, n])
    return [tuple(r) for r in runs]


# GitHub shows at most 10 warning annotations per step; the job summary lists everything.
MAX_ANNOTATIONS = 10


def annotate(kind, path, first, last, title, message):
    """Emit a GitHub workflow annotation, which a pull request shows inline on its diff."""
    if os.environ.get("GITHUB_ACTIONS") == "true":
        print(f"::{kind} file={path},line={first},endLine={last},title={title}::{message}")
