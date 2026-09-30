#!/usr/bin/env python3
"""Merge Cobertura reports, print a per-assembly summary, and enforce a line-coverage floor.

Usage: coverage.py <directory-of-cobertura-xml> [<floor-percent>]
                   [--stores <directory>] [--diff <revision>] [--no-gate]

The three in-memory suites each emit their own report and they overlap -- Trinity core is exercised
by all of them -- so the reports are merged by taking, for every (source file, line), the highest hit
count seen in any of them. Summing the totals instead would count shared lines several times and
report a number that is not coverage of anything.

Only product source files of this repository count. The rule is the file's path, not its assembly:
a test assembly is by construction almost fully covered -- including them reported 88.9% where the
product was at 80.9% -- and the store suites also instrument Testcontainers, whose NuGet package ships
symbols (2930 lines, which alone pulled a merged figure to 69.8%). A filename must resolve to a file
git knows about (tracked, or untracked and not ignored: a pre-commit check runs before `git add`).
Rewriting a `/_/` build path to repo-relative is not enough on its own, because it turns
`/_/src/Testcontainers/X.cs` into a plausible-looking `src/Testcontainers/X.cs`.

The floor is deliberately a little under the current number (80.9% when it was set) rather than at
it: a ratchet set to the exact current value turns any honest refactor that deletes well-covered code
into a red build. It is a floor against drift, not a target. It lives here, not in its callers, so CI
and a local check (.github/scripts/check.sh) cannot disagree about it. It gates the in-memory suites
only.

--stores adds the store suites' reports, one subdirectory per suite (`<Store>/` locally,
`coverage-store-<Store>/` as CI downloads them), each with an `outcome` file. They are reported and
never gated: a floor on them now would mostly measure Trinity.Virtuoso/VirtuosoManager.cs, a vendored
connector whose unused API is most of Virtuoso's uncovered lines. There is deliberately no merged
grand total -- it would sit below the gated figure purely because the adapters enter the denominator.
An adapter is measured by its own suite only: the Fuseki and GraphDB test projects reference other
adapters, whose never-executed lines would otherwise read as uncovered whenever their own suite did
not run. A suite that failed is left out entirely, since a partial report makes covered code look
uncovered.

--diff reports the change rather than the codebase: which changed product lines no test covers. The
total barely moves for any one commit, so it cannot say that; the changed lines can. A changed file
nothing measured is listed with the reason, rather than dropped or counted as uncovered. This part is
reported, never gated.

--no-gate reports without enforcing the floor, for a CI job that re-reads reports another job gated.
"""
import argparse
import glob
import os
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

from changed_lines import added_lines, annotate, git, hunks, is_product_source, ranges, repo_root
from stores import STORES

FLOOR = 78.0

# GitHub shows at most 10 warning annotations per step; the job summary lists everything.
MAX_ANNOTATIONS = 10

STORE_PREFIX = "coverage-store-"


def repo_files():
    listed = git("ls-files", "--cached", "--others", "--exclude-standard", "-z", text=True)
    return {path for path in listed.split("\0") if path}


def product_path(filename, root, files):
    """The repo-relative path of a Cobertura filename, or None if it is not product source here."""
    if filename.startswith("/_/"):
        rel = filename[3:]
    else:
        try:
            rel = os.path.relpath(filename, root).replace(os.sep, "/")
        except ValueError:  # another drive, on Windows
            return None
    return rel if rel in files and is_product_source(rel) else None


def load(directory, root, files):
    """(assembly, file, line) -> hits over every report under `directory`, and the report count."""
    reports = sorted(glob.glob(os.path.join(directory, "**", "*.cobertura.xml"), recursive=True))
    hits = defaultdict(int)

    for report in reports:
        for package in ET.parse(report).getroot().iter("package"):
            assembly = package.get("name") or "(unknown)"

            for cls in package.iter("class"):
                path = product_path(cls.get("filename") or "", root, files)
                if path is None:
                    continue

                for line in cls.iter("line"):
                    key = (assembly, path, int(line.get("number")))
                    hits[key] = max(hits[key], int(line.get("hits", "0")))

    return hits, len(reports)


def adapter_dir(store):
    return f"Trinity.{store}/"


def adapter_of(path):
    """The store whose adapter `path` belongs to, or None for shared code."""
    return next((s for s in STORES if path.startswith(adapter_dir(s))), None)


def load_stores(directory, root, files):
    """store -> (outcome, hits restricted to that store's own adapter plus shared code)."""
    stores = {}
    for entry in sorted(os.listdir(directory)) if os.path.isdir(directory) else ():
        path = os.path.join(directory, entry)
        if not os.path.isdir(path):
            continue
        store = entry[len(STORE_PREFIX):] if entry.startswith(STORE_PREFIX) else entry

        hits, count = load(path, root, files)
        marker = os.path.join(path, "outcome")
        if os.path.isfile(marker):
            with open(marker, encoding="utf-8") as handle:
                outcome = handle.read().strip() or "success"
        else:
            outcome = "success" if count else "not run"

        # Another adapter's lines in this report were loaded, not exercised; its own suite measures it.
        hits = {k: v for k, v in hits.items() if adapter_of(k[1]) in (None, store)}
        stores[store] = (outcome, hits)
    return stores


def failed(outcome):
    """A phrase for a suite outcome that is neither success nor "not run": CI's step outcome says
    `failure`, check.sh may say `timed out after 300 s`."""
    return "failed" if outcome == "failure" else outcome


def per_assembly(hits):
    totals = defaultdict(lambda: [0, 0])
    for (assembly, _, _), hit in hits.items():
        totals[assembly][1] += 1
        if hit > 0:
            totals[assembly][0] += 1
    return totals


def row(label, covered, total):
    return f"| {label} | {covered}/{total} | {100.0 * covered / total if total else 0.0:.1f}% |"


def by_line(*hit_maps):
    """(file, line) -> hits, maximised across the given maps."""
    merged = defaultdict(int)
    for hits in hit_maps:
        for (_, path, line), hit in hits.items():
            merged[(path, line)] = max(merged[(path, line)], hit)
    return merged


def store_section(memory, stores):
    ran = {s: h for s, (outcome, h) in stores.items() if outcome == "success"}
    out = ["### Store suites (reported, not gated)", ""]

    if ran:
        out += ["| Suite | Its adapter's lines | Coverage |", "|---|---:|---:|"]
        for store, hits in ran.items():
            own = [hit for (_, path, _), hit in hits.items() if path.startswith(adapter_dir(store))]
            out.append(row(store, sum(1 for h in own if h > 0), len(own)))

        # Shared code the store suites also exercise, as a row per assembly rather than a grand total.
        shared = dict(memory)
        for hits in ran.values():
            for key, hit in hits.items():
                if adapter_of(key[1]) is None:
                    shared[key] = max(shared.get(key, 0), hit)
        with_stores = per_assembly(shared)
        for assembly, (c, t) in sorted(per_assembly(memory).items()):
            mc, mt = with_stores[assembly]
            if (mc, mt) != (c, t):
                out.append(row(f"`{assembly}` including store suites", mc, mt))

    notes = []
    for store, (outcome, _) in stores.items():
        if outcome == "success":
            continue
        if outcome.startswith("not run"):
            notes.append(f"- {store}: {outcome}.")
        else:
            notes.append(f"- {store}: suite {failed(outcome)}; its coverage is left out, since a partial "
                         "report makes covered code look uncovered.")
    if notes and ran:
        out.append("")
    return "\n".join(out + notes)


def changed_report(memory, stores, revision):
    ran = {s: h for s, (outcome, h) in stores.items() if outcome == "success"}
    lines = by_line(memory, *ran.values())
    measured = {path for path, _ in lines}

    covered = missed_total = 0
    uncovered = []
    unmeasured = []

    for path, changed in sorted(added_lines(hunks(revision)).items()):
        if not is_product_source(path):
            continue
        if path not in measured:
            unmeasured.append((path, len(changed), why_unmeasured(path, stores)))
            continue

        coverable = [n for n in changed if (path, n) in lines]
        missed = [n for n in coverable if lines[(path, n)] == 0]
        covered += len(coverable) - len(missed)
        missed_total += len(missed)
        # A line with no coverage entry (blank, brace, comment) may sit inside a run.
        for first, last in ranges(missed, joinable=lambda n, p=path: (p, n) not in lines):
            uncovered.append((path, first, last))

    suites = "the in-memory suites" + (f" and {', '.join(ran)}" if ran else "")
    out = [f"### Changed lines (against {revision})", ""]

    if not covered and not missed_total and not unmeasured:
        out.append("No coverable product lines changed.")
        return "\n".join(out)

    out.append(f"{covered + missed_total} coverable product line(s) changed: {covered} covered, "
               f"{missed_total} not covered by any test that ran ({suites}).")
    if uncovered:
        out.append("")
        out.extend(f"- `{path}:{first}-{last}`" if first != last else f"- `{path}:{first}`"
                   for path, first, last in uncovered)
    if unmeasured:
        out += ["", "Not measured:", ""]
        out.extend(f"- `{path}` ({count} changed line(s)): {reason}" for path, count, reason in unmeasured)

    for path, first, last in uncovered[:MAX_ANNOTATIONS]:
        annotate("warning", path, first, last, "Not covered", f"No test executes this changed code ({suites}).")

    return "\n".join(out)


def why_unmeasured(path, stores):
    store = adapter_of(path)
    if store is None:
        return "no suite that ran loads this file"
    if store not in stores:
        return f"only the {store} store suite runs this, and it did not run"
    outcome = stores[store][0]
    if outcome == "success":
        return f"the {store} suite ran but never loaded this file"
    if outcome.startswith("not run"):
        return f"the {store} suite did not run ({outcome[len('not run'):].lstrip(': ') or 'no reason given'})"
    return f"the {store} suite {failed(outcome)}, so its coverage is left out"


def main(directory, floor, stores_dir, revision, gate):
    root = repo_root()
    files = repo_files()

    memory, count = load(directory, root, files)
    if not count:
        print(f"::error::No Cobertura reports found under {directory}", file=sys.stderr)
        return 1
    if not memory:
        # Reports exist but name no product file of this checkout: their paths point elsewhere. Say
        # so, rather than let "0.0% is below the floor" suggest the tests stopped covering anything.
        print(f"::error::The {count} report(s) under {directory} name no product source file of the "
              f"checkout at {root}; their paths do not resolve here.", file=sys.stderr)
        return 1

    totals = per_assembly(memory)
    covered = sum(e[0] for e in totals.values())
    total = sum(e[1] for e in totals.values())
    percent = 100.0 * covered / total if total else 0.0

    rows = ["| Assembly | Lines | Coverage |", "|---|---:|---:|"]
    rows += [row(f"`{a}`", c, t) for a, (c, t) in sorted(totals.items())]
    rows.append(f"| **Total** | **{covered}/{total}** | **{percent:.1f}%** |")
    table = "\n".join(rows)

    stores = load_stores(stores_dir, root, files) if stores_dir else {}
    status = 0
    for store, (outcome, hits) in stores.items():
        # A successful suite that measured none of its own adapter means the report's paths did not
        # resolve to this checkout -- say so, rather than publish every adapter line as uncovered.
        if outcome == "success" and not any(h > 0 for (_, p, _), h in hits.items() if p.startswith(adapter_dir(store))):
            print(f"::error::The {store} store report covers none of {adapter_dir(store)}; "
                  "its paths do not resolve to this checkout.", file=sys.stderr)
            status = 1

    sections = [store_section(memory, stores)] if stores else []
    if revision:
        sections.append(changed_report(memory, stores, revision))

    print(f"Merged {count} in-memory report(s).\n")
    print(table)
    for section in sections:
        print(f"\n{section}")

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        heading = "Code coverage including store suites" if stores_dir else "Code coverage"
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(f"## {heading}\n\n{table}\n\nFloor: {floor:.1f}% (in-memory suites"
                         f"{'; not enforced here' if not gate else ''})\n")
            for section in sections:
                handle.write(f"\n{section}\n")

    if not gate:
        print(f"\nLine coverage {percent:.1f}% (floor {floor:.1f}%, not enforced here).")
        return status

    if percent < floor:
        print(f"::error::Line coverage {percent:.1f}% is below the {floor:.1f}% floor.", file=sys.stderr)
        return 1

    print(f"\nLine coverage {percent:.1f}% meets the {floor:.1f}% floor.")
    return status


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("directory")
    parser.add_argument("floor", nargs="?", type=float, default=FLOOR)
    parser.add_argument("--stores", metavar="DIRECTORY", help="store suites' reports, one subdirectory per suite")
    parser.add_argument("--diff", metavar="REVISION", help="also report the lines changed since REVISION")
    parser.add_argument("--no-gate", dest="gate", action="store_false", help="report without enforcing the floor")
    args = parser.parse_args()

    sys.exit(main(args.directory, args.floor, args.stores, args.diff, args.gate))
