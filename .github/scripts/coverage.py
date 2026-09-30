#!/usr/bin/env python3
"""Merge Cobertura reports, print a per-assembly summary, and enforce a line-coverage floor.

Usage: coverage.py <directory-of-cobertura-xml> [<floor-percent>] [--diff <revision>]

The three in-memory suites each emit their own report and they overlap -- Trinity core is exercised
by all of them -- so the reports are merged by taking, for every (source file, line), the highest hit
count seen in any of them. Summing the totals instead would count shared lines several times and
report a number that is not coverage of anything.

Test assemblies are excluded. A test assembly is by construction almost fully covered -- it is the
thing being run -- so including them adds ~10 points of noise that moves with how much test code was
written rather than with how much of the product is exercised. Here that was the difference between
a reported 88.9% and an actual 80.9%.

The floor is deliberately a little under the current number (80.9% when it was set) rather than at
it: a ratchet set to the exact current value turns any honest refactor that deletes well-covered code
into a red build. It is a floor against drift, not a target. It lives here, not in its callers, so CI
and a local check (.github/scripts/check.sh) cannot disagree about it.

--diff reports the change rather than the codebase: which changed product lines no test covers. The
total barely moves for any one commit, so it cannot say that; the changed lines can. Files no
in-memory suite loads -- the store adapters, which only the Docker suites exercise and those collect
no coverage -- are listed as not measured rather than silently dropped or counted as uncovered. This
part is reported, never gated.
"""
import argparse
import glob
import os
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

from changed_lines import added_lines, annotate, hunks, is_product_source, ranges, repo_root

FLOOR = 78.0

# GitHub shows at most 10 warning annotations per step; the job summary lists everything.
MAX_ANNOTATIONS = 10


def changed_report(hits, revision):
    root = repo_root()

    # (repo-relative file, line) -> hits, product assemblies only.
    product = defaultdict(int)
    for (assembly, filename, line), hit in hits.items():
        if assembly.endswith(".Tests"):
            continue
        rel = os.path.relpath(filename, root).replace(os.sep, "/")
        product[(rel, line)] = max(product[(rel, line)], hit)

    measured = {path for path, _ in product}
    covered = missed_total = 0
    uncovered = []
    unmeasured = []

    for path, lines in sorted(added_lines(hunks(revision)).items()):
        if not is_product_source(path):
            continue
        if path not in measured:
            unmeasured.append((path, len(lines)))
            continue

        coverable = [n for n in lines if (path, n) in product]
        missed = [n for n in coverable if product[(path, n)] == 0]
        covered += len(coverable) - len(missed)
        missed_total += len(missed)
        # A line with no coverage entry (blank, brace, comment) may sit inside a run.
        for first, last in ranges(missed, joinable=lambda n, p=path: (p, n) not in product):
            uncovered.append((path, first, last))

    out = [f"### Changed lines (against {revision})", ""]

    if not covered and not missed_total and not unmeasured:
        out.append("No coverable product lines changed.")
        return "\n".join(out)

    out.append(f"{covered + missed_total} coverable product line(s) changed: {covered} covered, "
               f"{missed_total} not covered by any in-memory test.")
    if uncovered:
        out.append("")
        out.extend(f"- `{path}:{first}-{last}`" if first != last else f"- `{path}:{first}`"
                   for path, first, last in uncovered)
    if unmeasured:
        out += ["", "Not measured -- no in-memory suite loads these files, and the Docker store suites "
                "that run them collect no coverage:", ""]
        out.extend(f"- `{path}` ({count} changed line(s))" for path, count in unmeasured)

    for path, first, last in uncovered[:MAX_ANNOTATIONS]:
        annotate("warning", path, first, last, "Not covered",
                 "No in-memory test executes this changed code.")

    return "\n".join(out)


def main(directory, floor, revision):
    reports = sorted(glob.glob(os.path.join(directory, "**", "*.cobertura.xml"), recursive=True))

    if not reports:
        print(f"::error::No Cobertura reports found under {directory}", file=sys.stderr)
        return 1

    # (assembly, file, line) -> hits, maximised across reports.
    hits = defaultdict(int)

    for report in reports:
        root = ET.parse(report).getroot()

        for package in root.iter("package"):
            assembly = package.get("name") or "(unknown)"

            for cls in package.iter("class"):
                filename = cls.get("filename") or ""

                for line in cls.iter("line"):
                    key = (assembly, filename, int(line.get("number")))
                    hits[key] = max(hits[key], int(line.get("hits", "0")))

    per_assembly = defaultdict(lambda: [0, 0])

    for (assembly, _, _), hit in hits.items():
        if assembly.endswith(".Tests"):
            continue

        entry = per_assembly[assembly]
        entry[1] += 1
        if hit > 0:
            entry[0] += 1

    covered = sum(e[0] for e in per_assembly.values())
    total = sum(e[1] for e in per_assembly.values())
    percent = 100.0 * covered / total if total else 0.0

    rows = ["| Assembly | Lines | Coverage |", "|---|---:|---:|"]
    for assembly in sorted(per_assembly):
        c, t = per_assembly[assembly]
        rows.append(f"| `{assembly}` | {c}/{t} | {100.0 * c / t:.1f}% |")
    rows.append(f"| **Total** | **{covered}/{total}** | **{percent:.1f}%** |")

    table = "\n".join(rows)
    changes = changed_report(hits, revision) if revision else ""

    print(f"Merged {len(reports)} report(s).\n")
    print(table)
    if changes:
        print(f"\n{changes}")

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(f"## Code coverage\n\n{table}\n\nFloor: {floor:.1f}%\n")
            if changes:
                handle.write(f"\n{changes}\n")

    if percent < floor:
        print(f"::error::Line coverage {percent:.1f}% is below the {floor:.1f}% floor.", file=sys.stderr)
        return 1

    print(f"\nLine coverage {percent:.1f}% meets the {floor:.1f}% floor.")
    return 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("directory")
    parser.add_argument("floor", nargs="?", type=float, default=FLOOR)
    parser.add_argument("--diff", metavar="REVISION", help="also report the lines changed since REVISION")
    args = parser.parse_args()

    sys.exit(main(args.directory, args.floor, args.diff))
