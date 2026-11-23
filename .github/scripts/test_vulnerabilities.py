"""Tests for vulnerabilities.py, run by `python3 -m unittest discover -s .github/scripts`.

RC5 is trimmed from the real `dotnet list package --vulnerable --include-transitive --format json`
report of v2.0.0-rc.5, the release this gate was added after (ADR-0054). The critical advisory appears
only under a net8.0 test project, never under the netstandard2.0 adapter that causes it -- that is why
the gate scans the whole solution. UNREACHABLE is what the same command printed, with exit code 1, when
its source could not be reached: plain text, no JSON.
"""
import io
import json
import os
import unittest
from contextlib import redirect_stdout, redirect_stderr
from unittest import mock

import vulnerabilities
from vulnerabilities import ReportError, findings, parse, verdict

SDC = "https://github.com/advisories/GHSA-rxg9-xrhp-64gj"
REGEX = "https://github.com/advisories/GHSA-cmhx-cq75-c4mj"
SSH1 = "https://github.com/advisories/GHSA-q939-rpr3-3284"
SSH2 = "https://github.com/advisories/GHSA-mggc-4xg6-vcxf"
ANGLE = "https://github.com/advisories/GHSA-pgww-w46g-26qg"


def package(name, version, *advisories):
    return {"id": name, "resolvedVersion": version,
            "vulnerabilities": [{"severity": severity, "advisoryurl": url} for severity, url in advisories]}


def project(path, framework=None, transitive=(), top_level=()):
    entry = {"path": path}
    if framework:
        entry["frameworks"] = [{"framework": framework}]
        if transitive:
            entry["frameworks"][0]["transitivePackages"] = list(transitive)
        if top_level:
            entry["frameworks"][0]["topLevelPackages"] = list(top_level)
    return entry


def report(*projects, problems=None):
    data = {"version": 1, "parameters": "--vulnerable --include-transitive", "projects": list(projects)}
    if problems is not None:
        data["problems"] = problems
    return data


RC5 = report(
    project("/r/Trinity/Trinity.csproj"),
    project("/r/Trinity.Virtuoso/Trinity.Virtuoso.csproj", "netstandard2.0",
            [package("System.Text.RegularExpressions", "4.3.0", ("High", REGEX))]),
    project("/r/tests/Trinity.Tests.Virtuoso/Trinity.Tests.Virtuoso.csproj", "net8.0", [
        package("SSH.NET", "2023.0.0", ("High", SSH1), ("High", SSH2)),
        package("System.Drawing.Common", "5.0.0", ("Critical", SDC)),
        package("System.Text.RegularExpressions", "4.3.0", ("High", REGEX)),
    ]),
    project("/r/Trinity.Vocabulary/Trinity.Vocabulary.csproj", "netstandard2.0",
            [package("AngleSharp", "1.4.0", ("Moderate", ANGLE))]),
)

UNREACHABLE = ("error: Unable to load the service index for source https://127.0.0.1:9/index.json.\n"
               "error:   Connection refused (127.0.0.1:9)\n")


class Gates(unittest.TestCase):
    def test_rc5_fails_on_its_critical_advisory(self):
        text, failed = verdict(RC5, {})
        self.assertTrue(failed)
        self.assertIn("`System.Drawing.Common` | 5.0.0 | [GHSA-rxg9-xrhp-64gj]", text)
        self.assertLess(text.index("Critical"), text.index("High"), "most severe first")

    def test_moderate_is_reported_not_gated(self):
        only = report(project("/r/V/Trinity.Vocabulary.csproj", "netstandard2.0",
                              [package("AngleSharp", "1.4.0", ("Moderate", ANGLE))]))
        text, failed = verdict(only, {})
        self.assertFalse(failed)
        self.assertIn("GHSA-pgww-w46g-26qg", text)

    def test_a_clean_report_passes(self):
        clean = report(project("/r/Trinity/Trinity.csproj"), project("/r/T/Trinity.Tests.csproj"), problems=[])
        text, failed = verdict(clean, {})
        self.assertFalse(failed)
        self.assertIn("No package in the 2 projects has a known advisory.", text)

    def test_top_level_packages_count_too(self):
        direct = report(project("/r/A/A.csproj", "net8.0", top_level=[package("P", "1.0", ("High", SSH1))]))
        self.assertTrue(verdict(direct, {})[1])

    def test_an_unknown_severity_fails_closed(self):
        odd = report(project("/r/A/A.csproj", "net8.0", [package("P", "1.0", ("Severe", SSH1))]))
        self.assertTrue(verdict(odd, {})[1])


class Rows(unittest.TestCase):
    def test_one_row_per_advisory_naming_every_project(self):
        rows = findings(RC5)
        regex = rows[("GHSA-cmhx-cq75-c4mj", "System.Text.RegularExpressions", "4.3.0")]
        self.assertEqual(regex["projects"], {"Trinity.Virtuoso", "Trinity.Tests.Virtuoso"})
        self.assertEqual(len(rows), 5)  # two SSH.NET advisories are two rows

    def test_windows_paths(self):
        windows = report(project("C:\\r\\Trinity.Virtuoso\\Trinity.Virtuoso.csproj", "netstandard2.0",
                                 [package("P", "1.0", ("High", SSH1))]))
        self.assertEqual(next(iter(findings(windows).values()))["projects"], {"Trinity.Virtuoso"})


class Accepted(unittest.TestCase):
    def test_an_accepted_advisory_is_listed_with_its_reason_and_not_gated(self):
        reasons = {key[0]: "no fix upstream" for key in findings(RC5)}
        text, failed = verdict(RC5, reasons)
        self.assertFalse(failed)
        self.assertIn("Critical (accepted: no fix upstream)", text)

    def test_one_unaccepted_advisory_still_fails(self):
        reasons = {key[0]: "x" for key in findings(RC5) if key[0] != "GHSA-rxg9-xrhp-64gj"}
        self.assertTrue(verdict(RC5, reasons)[1])

    def test_an_entry_nothing_has_any_more_is_reported(self):
        text, failed = verdict(report(project("/r/A/A.csproj")), {"GHSA-gone-gone-gone": "x"})
        self.assertFalse(failed)
        self.assertIn("ACCEPTED lists GHSA-gone-gone-gone, which nothing in the graph has any more", text)


class NeverReadsAFailureAsClean(unittest.TestCase):
    def test_plain_text_from_an_unreachable_source(self):
        with self.assertRaises(ReportError) as raised:
            parse(1, UNREACHABLE)
        self.assertIn("Unable to load the service index", raised.exception.reason)

    def test_an_error_problem(self):
        refused = report(project("/r/A/A.csproj"), problems=[{"level": "error", "text": "NuGet requires HTTPS sources."}])
        with self.assertRaises(ReportError) as raised:
            parse(1, json.dumps(refused))
        self.assertIn("HTTPS", raised.exception.reason)

    def test_a_failing_exit_code_with_a_report(self):
        with self.assertRaises(ReportError):
            parse(1, json.dumps(report(project("/r/A/A.csproj"))))

    def test_a_report_naming_no_projects(self):
        with self.assertRaises(ReportError):
            parse(0, json.dumps(report()))

    def test_a_warning_problem_is_shown_not_fatal(self):
        warned = report(project("/r/A/A.csproj"), problems=[{"level": "warning", "text": "something odd"}])
        text, failed = verdict(parse(0, json.dumps(warned)), {})
        self.assertFalse(failed)
        self.assertIn("Note: something odd", text)

    def test_a_report_that_cannot_be_produced_fails_in_ci_and_is_not_run_locally(self):
        def unavailable(root):
            raise ReportError("offline")

        with mock.patch.object(vulnerabilities, "report_of_solution", unavailable), \
                mock.patch.object(vulnerabilities, "repo_root", lambda: "/r"):
            for environment, expected in (({"GITHUB_ACTIONS": "true"}, 1), ({}, 0)):
                with self.subTest(environment=environment), mock.patch.dict(os.environ, environment, clear=True), \
                        redirect_stdout(io.StringIO()) as out, redirect_stderr(io.StringIO()):
                    self.assertEqual(vulnerabilities.main(), expected)
                    if not expected:
                        self.assertIn("not run: offline", out.getvalue())


if __name__ == "__main__":
    unittest.main()
