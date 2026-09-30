#!/usr/bin/env python3
"""Summarize a jscpd report per project, list the clones, and enforce a duplication ceiling.

Usage: duplication.py <jscpd-report.json> <ceiling-percent>

The measure is the share of product lines that sit in a clone at all -- either side of it. jscpd's
own headline figure counts roughly one side of each pair (5.4% here where this is 9.6%), which
halves exactly the thing worth seeing: every duplicated line has a twin that a fix must also reach.
That is how this codebase has regressed before -- a fix landing in one store adapter's copy and
not the other's, or in two of three IModel implementations -- so both copies count.

What is scanned (product code only, comments ignored) is configured in .jscpd.json, so a local
`npx jscpd@<version> .` sees the same files as CI.
"""
import json
import os
import sys
from collections import defaultdict


def main(report_path, ceiling):
    if not os.path.isfile(report_path):
        print(f"::error::No jscpd report at {report_path}", file=sys.stderr)
        return 1

    with open(report_path, encoding="utf-8") as handle:
        report = json.load(handle)

    sources = report["statistics"]["formats"].get("csharp", {}).get("sources", {})
    clones = report["duplicates"]

    # (file, line) in any clone, on either side. A set, because clones overlap: one file's
    # per-source count in the report can exceed its length.
    cloned = set()
    for clone in clones:
        for side in (clone["firstFile"], clone["secondFile"]):
            cloned.update((side["name"], line) for line in range(side["start"], side["end"] + 1))

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
    listing = "\n".join(
        f"- {c['lines']} lines: `{c['firstFile']['name']}:{c['firstFile']['start']}-{c['firstFile']['end']}`"
        f" = `{c['secondFile']['name']}:{c['secondFile']['start']}-{c['secondFile']['end']}`"
        for c in pairs)

    print(f"{len(clones)} clone(s) in {len(sources)} file(s).\n")
    print(table)
    print(f"\n{listing}")

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(f"## Code duplication\n\n{table}\n\nCeiling: {ceiling:.1f}%\n\n"
                         f"<details><summary>{len(clones)} clone(s), largest first</summary>\n\n"
                         f"{listing}\n\n</details>\n")

    if percent > ceiling:
        print(f"::error::Duplication {percent:.1f}% is above the {ceiling:.1f}% ceiling.", file=sys.stderr)
        return 1

    print(f"\nDuplication {percent:.1f}% is within the {ceiling:.1f}% ceiling.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        sys.exit(2)

    sys.exit(main(sys.argv[1], float(sys.argv[2])))
