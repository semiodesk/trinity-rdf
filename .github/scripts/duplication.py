#!/usr/bin/env python3
"""Detect clones with jscpd, summarize them per project, and enforce a duplication ceiling.

Usage: duplication.py [<ceiling-percent>] [--diff <revision>]

The measure is the share of product lines that sit in a clone at all -- either side of it. jscpd's
own headline figure counts roughly one side of each pair (5.4% here where this is 9.6%), which
halves exactly the thing worth seeing: every duplicated line has a twin that a fix must also reach.
That is how this codebase has regressed before -- a fix landing in one store adapter's copy and
not the other's, or in two of three IModel implementations -- so both copies count.

The ceiling sits a little above the current figure, not at it, for the reason the coverage floor
sits below: deleting unduplicated code raises the share without adding a single clone.

--diff reports two things about a change, as questions for its author rather than as a gate:

- Code it edited on one side of a clone only. The clones come from scanning the tree *at the
  revision*, not the current one: editing one copy is exactly what stops the two copies matching,
  so a scan of the result no longer contains the clone the edit landed in. The first version of
  this report scanned the result and was blind to every real edit; only comment-only edits (which
  the scan ignores) kept the clone intact and were found.
- New code that mostly repeats existing code, from the current scan.

jscpd is pinned exactly and run here, so CI and a local check (.github/scripts/check.sh) use the same
version: a newer tokenizer moves the number, and a gate should only move when the code does. What is
scanned lives in .jscpd.json.
"""
import argparse
import io
import json
import os
import subprocess
import sys
import tarfile
import tempfile
from collections import defaultdict

from changed_lines import added_lines, annotate, git, hunks, repo_root, touched_old_lines

CEILING = 10.5
JSCPD = "jscpd@4.3.0"
REPORT = os.path.join("duplication", "jscpd-report.json")

# GitHub shows at most 10 warning annotations per step; the job summary lists everything.
MAX_ANNOTATIONS = 10


class ScanError(Exception):
    pass


def scan(directory, config):
    """Run jscpd over `directory` with the given config and return its JSON report."""
    report = os.path.join(directory, REPORT)
    if os.path.exists(report):
        os.remove(report)

    # --silent drops jscpd's own console summary; the table below replaces it.
    result = subprocess.run(["npx", "--yes", JSCPD, "--silent", "--config", config, "."],
                            cwd=directory, capture_output=True, text=True)
    if result.returncode != 0 or not os.path.isfile(report):
        raise ScanError(f"jscpd failed (exit {result.returncode}) and wrote no report at {report}\n"
                        f"{result.stdout}{result.stderr}")

    with open(report, encoding="utf-8") as handle:
        return json.load(handle)


def scan_revision(revision, config):
    """Clones as they were at `revision`, from a scratch copy of that tree."""
    with tempfile.TemporaryDirectory() as base:
        with tarfile.open(fileobj=io.BytesIO(git("archive", "--format=tar", revision))) as tar:
            if hasattr(tarfile, "data_filter"):
                tar.extractall(base, filter="data")
            else:
                tar.extractall(base)
        return scan(base, config)["duplicates"]


def span(side):
    return set(range(side["start"], side["end"] + 1))


def location(side):
    return f"{side['name']}:{side['start']}-{side['end']}"


def changed_report(current_clones, revision, config):
    all_hunks = list(hunks(revision))
    out = [f"### Duplicated code the change touches (against {revision})", ""]

    # 1. One copy edited, judged against the clones the change started from.
    by_old_path = defaultdict(list)
    for h in all_hunks:
        if h.old_path:
            by_old_path[h.old_path].append(h)

    one_sided, both = [], 0
    try:
        base_clones = scan_revision(revision, config)
    except (subprocess.CalledProcessError, ScanError, tarfile.TarError) as error:
        base_clones = []
        out += [f"Could not scan {revision} for clones, so edits to one copy are not reported: {error}", ""]

    for clone in base_clones:
        sides = (clone["firstFile"], clone["secondFile"])
        edits = [[h for h in by_old_path.get(s["name"], ()) if touched_old_lines(h) & span(s)] for s in sides]
        if edits[0] and edits[1]:
            both += 1
        elif edits[0] or edits[1]:
            i = 0 if edits[0] else 1
            one_sided.append((clone["lines"], sides[i], sides[1 - i], edits[i]))

    # 2. New code that mostly repeats code that was already there, from the current clones.
    added = added_lines(all_hunks)
    introduced = []
    for clone in current_clones:
        for new, old in ((clone["firstFile"], clone["secondFile"]), (clone["secondFile"], clone["firstFile"])):
            if 2 * len(added.get(new["name"], set()) & span(new)) > len(span(new)):
                introduced.append((clone["lines"], new, old))
                break

    if not one_sided and not introduced and not both:
        out.append("The change touches no duplicated code.")
        return "\n".join(out)

    if one_sided:
        one_sided.sort(key=lambda c: c[0], reverse=True)
        out.append(f"Edited on one side only -- does each edit belong in the twin as well? "
                   f"(line numbers as of {revision})")
        out.append("")
        out.extend(f"- `{location(edited)}` edited; its twin `{location(other)}` was not"
                   for _, edited, other, _ in one_sided)
        out.append("")
    if introduced:
        introduced.sort(key=lambda c: c[0], reverse=True)
        out.append("New code that repeats existing code:")
        out.append("")
        out.extend(f"- `{location(new)}` repeats `{location(old)}`" for _, new, old in introduced)
        out.append("")
    if both:
        out.append(f"{both} clone(s) edited on both sides.")

    notes = []
    for _, edited, other, edits in one_sided:
        on = [h for h in edits if h.new_path]
        if on:
            first = max(1, min(h.new_start for h in on))
            last = max(h.new_start + max(h.new_count, 1) - 1 for h in on)
            notes.append((on[0].new_path, first, last,
                          f"This code also exists at {location(other)}, which this change does not touch."))
    for _, new, old in introduced:
        notes.append((new["name"], new["start"], new["end"], f"This new code repeats {location(old)}."))
    for path, first, last, message in notes[:MAX_ANNOTATIONS]:
        annotate("warning", path, first, last, "Duplicated code", message)

    return "\n".join(out).rstrip()


def main(ceiling, revision):
    root = repo_root()
    config = os.path.join(root, ".jscpd.json")

    try:
        report = scan(root, config)
    except ScanError as error:
        print(f"::error::{error}", file=sys.stderr)
        return 1

    sources = report["statistics"]["formats"].get("csharp", {}).get("sources", {})
    clones = report["duplicates"]

    # (file, line) in any clone, on either side. A set, because clones overlap: one file's
    # per-source count in the report can exceed its length.
    cloned = set()
    for clone in clones:
        for side in (clone["firstFile"], clone["secondFile"]):
            cloned.update((side["name"], line) for line in span(side))

    per_project = defaultdict(lambda: [0, 0])
    for name, stats in sources.items():
        per_project[name.split("/")[0]][1] += stats["lines"]
    for name, _ in cloned:
        per_project[name.split("/")[0]][0] += 1

    duplicated = sum(e[0] for e in per_project.values())
    total = sum(e[1] for e in per_project.values())
    percent = 100.0 * duplicated / total if total else 0.0

    rows = ["| Project | Lines in a clone | Share |", "|---|---:|---:|"]
    for project in sorted(per_project):
        d, t = per_project[project]
        rows.append(f"| `{project}` | {d}/{t} | {100.0 * d / t:.1f}% |")
    rows.append(f"| **Total** | **{duplicated}/{total}** | **{percent:.1f}%** |")
    table = "\n".join(rows)

    pairs = sorted(clones, key=lambda c: c["lines"], reverse=True)
    listing = [f"- {c['lines']} lines: `{location(c['firstFile'])}` = `{location(c['secondFile'])}`"
               for c in pairs]
    changes = changed_report(clones, revision, config) if revision else ""

    print(f"{len(clones)} clone(s) in {len(sources)} file(s).\n")
    print(table)
    if changes:
        print(f"\n{changes}")

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(f"## Code duplication\n\n{table}\n\nCeiling: {ceiling:.1f}%\n\n")
            if changes:
                handle.write(f"{changes}\n\n")
            handle.write(f"<details><summary>{len(clones)} clone(s), largest first</summary>\n\n"
                         + "\n".join(listing) + "\n\n</details>\n")

    if percent > ceiling:
        print(f"::error::Duplication {percent:.1f}% is above the {ceiling:.1f}% ceiling.", file=sys.stderr)
        return 1

    print(f"\nDuplication {percent:.1f}% is within the {ceiling:.1f}% ceiling.")
    return 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("ceiling", nargs="?", type=float, default=CEILING)
    parser.add_argument("--diff", metavar="REVISION", help="also report duplicated code changed since REVISION")
    args = parser.parse_args()

    sys.exit(main(args.ceiling, args.diff))
