#!/usr/bin/env python3
"""Summarise Cobertura coverage across several test runs.

Coverage of this codebase is only meaningful as a *union*: no single suite
exercises everything, because each store suite drives the same shared fixtures
through a different backend and the backends override different members. A
per-run report therefore understates every shared class. This merges runs by
(file, line) before reporting.

Usage:
    dotnet test <project> -c Release --collect:"XPlat Code Coverage" \
        --results-directory .coverage/<name>
    python3 scripts/coverage-summary.py .coverage            # per assembly
    python3 scripts/coverage-summary.py .coverage --classes  # per class
    python3 scripts/coverage-summary.py .coverage --uncovered StoreBase
"""

import collections
import glob
import os
import sys
import xml.etree.ElementTree as ET


def load(root_dir):
    """Returns (assembly, class, method, line) -> [hits, file], summed over runs.

    The filename is deliberately not part of the key. Runs report the same file
    both relatively and absolutely depending on how they were invoked, so keying
    on it counts every line twice and halves the reported coverage.
    """
    reports = glob.glob(os.path.join(root_dir, "**", "coverage.cobertura.xml"), recursive=True)

    if not reports:
        sys.exit(f"No coverage.cobertura.xml under {root_dir}. Did the run use --collect?")

    hits = {}

    for report in reports:
        for package in ET.parse(report).getroot().iter("package"):
            assembly = package.get("name", "")

            # Test assemblies cover themselves; that is not the question being asked.
            if "Tests" in assembly:
                continue

            for cls in package.iter("class"):
                class_name = cls.get("name", "")
                file_name = cls.get("filename", "")

                for method in cls.iter("method"):
                    method_name = method.get("name", "")

                    for line in method.iter("line"):
                        key = (assembly, class_name, method_name, int(line.get("number")))
                        entry = hits.setdefault(key, [0, file_name])
                        entry[0] += int(line.get("hits", "0"))

    print(f"merged {len(reports)} run(s)", file=sys.stderr)

    return hits


def rollup(hits, level):
    covered = collections.Counter()
    total = collections.Counter()

    for (assembly, class_name, method_name, _), (n, _file) in hits.items():
        key = {"assembly": assembly,
               "class": f"{assembly}|{class_name}",
               "method": f"{assembly}|{class_name}|{method_name}"}[level]
        total[key] += 1
        if n:
            covered[key] += 1

    return covered, total


def main():
    root_dir = sys.argv[1] if len(sys.argv) > 1 else ".coverage"
    hits = load(root_dir)

    if "--uncovered" in sys.argv:
        wanted = sys.argv[sys.argv.index("--uncovered") + 1]
        print(f"Lines never hit in any run, in classes matching {wanted!r}:\n")

        for (_, class_name, method_name, line), (n, file_name) in sorted(
                hits.items(), key=lambda kv: (kv[0][1], kv[0][3])):
            if n or wanted not in class_name:
                continue
            print(f"  {class_name.split('.')[-1]}.{method_name}  "
                  f"{os.path.basename(file_name)}:{line}")

        return

    level = "class" if "--classes" in sys.argv else "assembly"
    covered, total = rollup(hits, level)

    print(f"\nUnion line coverage by {level}\n")

    for key in sorted(total, key=lambda k: (-total[k])):
        c, t = covered[key], total[key]
        label = key.split("|")[-1] if level != "assembly" else key
        print(f"  {label[:62]:<64}{c:>6}/{t:<7}{100 * c // t:>4}%")


if __name__ == "__main__":
    main()
