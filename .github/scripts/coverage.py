#!/usr/bin/env python3
"""Merge Cobertura reports, print a per-assembly summary, and enforce a line-coverage floor.

Usage: coverage.py <directory-of-cobertura-xml> <floor-percent>

The three in-memory suites each emit their own report and they overlap -- Trinity core is exercised
by all of them -- so the reports are merged by taking, for every (source file, line), the highest hit
count seen in any of them. Summing the totals instead would count shared lines several times and
report a number that is not coverage of anything.

Test assemblies are excluded. A test assembly is by construction almost fully covered -- it is the
thing being run -- so including them adds ~10 points of noise that moves with how much test code was
written rather than with how much of the product is exercised. Here that was the difference between
a reported 88.9% and an actual 80.9%.
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict


def main(directory, floor):
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
    print(f"Merged {len(reports)} report(s).\n")
    print(table)

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(f"## Code coverage\n\n{table}\n\nFloor: {floor:.1f}%\n")

    if percent < floor:
        print(f"::error::Line coverage {percent:.1f}% is below the {floor:.1f}% floor.", file=sys.stderr)
        return 1

    print(f"\nLine coverage {percent:.1f}% meets the {floor:.1f}% floor.")
    return 0


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        sys.exit(2)

    sys.exit(main(sys.argv[1], float(sys.argv[2])))
