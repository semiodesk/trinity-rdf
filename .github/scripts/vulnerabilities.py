#!/usr/bin/env python3
"""Report every package with a known advisory in the solution's restored graph, and fail on High or
Critical.

Usage: vulnerabilities.py        (after a restore: it reads the projects' assets files)

The report is `dotnet list package --vulnerable --include-transitive` over the whole solution, test
projects included, and that scope is the point. The defect this gate was added for (ADR-0054) was
invisible from the projects that ship: every store adapter targets netstandard2.0 and pinned
System.Security.Permissions 5.0.0, whose netstandard2.0 dependency group is harmless -- but a net5+
consumer resolves its net5.0 group instead, which brings System.Drawing.Common 5.0.0 (critical). Only a
project that consumes an adapter the way a user does resolves that group, and here those are the net8.0
store test projects. An adapter without such a project would escape this check.

The threshold lives here, so CI and a local .github/scripts/check.sh cannot disagree about it. Moderate
and Low are reported, never gated. An advisory that cannot be fixed yet -- no patched version upstream --
goes in ACCEPTED with the reason, where a reviewer sees it, rather than the gate being switched off.

`dotnet list package` exits 0 whatever it finds, and an unreachable source makes it print plain text
rather than JSON. Neither is ever read as "no advisories": a report that cannot be produced fails in CI
and is reported as not run locally -- offline is an absent optional input there, as a missing npx is
for the duplication scan.
"""
import argparse
import json
import os
import subprocess
import sys

from changed_lines import repo_root

SOLUTION = "Semiodesk.Trinity.sln"

# NuGet's severities, lowest first. Anything else -- a severity this list does not know -- is gated:
# failing closed on a name nobody anticipated is the safe reading.
NOT_GATED = ("Low", "Moderate")
ORDER = ("Critical", "High", "Moderate", "Low")

# Advisory id (the last segment of its URL, e.g. GHSA-xxxx-xxxx-xxxx) -> why it is accepted. Empty: no
# advisory is accepted. An entry needs a reason a reviewer can check, and is reported every run, with a
# note once nothing in the graph has it any more.
ACCEPTED = {}


class ReportError(Exception):
    def __init__(self, reason):
        super().__init__(reason)
        self.reason = reason


def first_line(text):
    return next((line.strip() for line in text.splitlines() if line.strip()), "")


def parse(returncode, stdout, stderr=""):
    """The report as a dict, or ReportError when there is no complete report to read."""
    try:
        report = json.loads(stdout)
    except ValueError:
        report = None
    if not isinstance(report, dict):
        reason = first_line(stderr) or first_line(stdout) or f"exit code {returncode}, no output"
        raise ReportError(f"dotnet list package produced no report ({reason})")
    errors = [p.get("text", "") for p in report.get("problems") or [] if p.get("level") == "error"]
    if errors:
        raise ReportError("; ".join(errors))
    if returncode != 0:
        raise ReportError(f"dotnet list package exited with {returncode}")
    if not report.get("projects"):
        raise ReportError("the report names no projects")
    return report


def findings(report):
    """(advisory, package, version) -> {"severity", "url", "projects"}, one row per advisory and version."""
    found = {}
    for project in report["projects"]:
        name = os.path.splitext(os.path.basename(project["path"].replace("\\", "/")))[0]
        for framework in project.get("frameworks") or []:
            for kind in ("topLevelPackages", "transitivePackages"):
                for package in framework.get(kind) or []:
                    for vulnerability in package.get("vulnerabilities") or []:
                        url = vulnerability.get("advisoryurl", "")
                        advisory = url.rstrip("/").rsplit("/", 1)[-1] or url
                        key = (advisory, package["id"], package["resolvedVersion"])
                        row = found.setdefault(key, {
                            "severity": vulnerability.get("severity", "Unknown"),
                            "url": url,
                            "projects": set(),
                        })
                        row["projects"].add(name)
    return found


def gated(severity):
    return severity not in NOT_GATED


def rank(severity):
    return ORDER.index(severity) if severity in ORDER else -1


def table(found, accepted):
    rows = ["| Severity | Package | Version | Advisory | Projects |", "|---|---|---|---|---|"]
    for (advisory, package, version), row in sorted(
            found.items(), key=lambda item: (rank(item[1]["severity"]), item[0][1], item[0][0])):
        severity = row["severity"]
        if advisory in accepted:
            severity += f" (accepted: {accepted[advisory]})"
        projects = ", ".join(f"`{p}`" for p in sorted(row["projects"]))
        rows.append(f"| {severity} | `{package}` | {version} | [{advisory}]({row['url']}) | {projects} |")
    return "\n".join(rows)


def verdict(report, accepted):
    """(text, failed): the report's findings as Markdown, and whether any of them is gated."""
    found = findings(report)
    warnings = [p.get("text", "") for p in report.get("problems") or [] if p.get("level") != "error"]
    blocking = sorted({key[0] for key, row in found.items() if gated(row["severity"]) and key[0] not in accepted})
    stale = sorted(set(accepted) - {key[0] for key in found})

    out = []
    if found:
        out.append(table(found, accepted))
    else:
        out.append(f"No package in the {len(report['projects'])} projects has a known advisory.")
    out += [f"\nNote: {text}" for text in warnings]
    out += [f"\nACCEPTED lists {advisory}, which nothing in the graph has any more; remove it." for advisory in stale]
    return "\n".join(out), bool(blocking)


def report_of_solution(root):
    command = ["dotnet", "list", SOLUTION, "package", "--vulnerable", "--include-transitive", "--format", "json"]
    try:
        result = subprocess.run(command, cwd=root, capture_output=True, text=True, timeout=600)
    except FileNotFoundError:
        raise ReportError("dotnet is not installed")
    except subprocess.TimeoutExpired:
        raise ReportError("dotnet list package did not finish within 600 s")
    return parse(result.returncode, result.stdout, result.stderr)


def main():
    try:
        report = report_of_solution(repo_root())
    except ReportError as error:
        if os.environ.get("GITHUB_ACTIONS") == "true":
            print(f"::error::Vulnerable-package check could not run: {error.reason}", file=sys.stderr)
            return 1
        print(f"Vulnerable-package check not run: {error.reason}. CI enforces it regardless.")
        return 0

    text, failed = verdict(report, ACCEPTED)
    print(text)

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write(f"## Vulnerable packages\n\n{text}\n\nGated: every severity except "
                         f"{' and '.join(NOT_GATED)}.\n\n")

    if failed:
        sys.stdout.flush()
        print("::error::A package with a High or Critical advisory is in the graph. Raise or pin it in "
              "Directory.Packages.props, or remove the reference that brings it in.", file=sys.stderr)
        return 1

    print("\nNo High or Critical advisory.")
    return 0


if __name__ == "__main__":
    argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter).parse_args()
    sys.exit(main())
