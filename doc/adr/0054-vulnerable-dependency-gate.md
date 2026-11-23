# 0054. Vulnerable dependencies fail CI, judged from where a consumer stands

Date: 2026-11-23

## Status
Accepted

## Context
2.0.0-rc.5 shipped four store adapters that handed every net5+ consumer **System.Drawing.Common 5.0.0**
(GHSA-rxg9-xrhp-64gj, remote code execution, critical). Nothing in the repository noticed.

The route is easy to miss. Each adapter targets netstandard2.0 and referenced System.Security.Permissions
5.0.0. That package's netstandard2.0 dependency group is harmless, and it is the only group the adapter's
own restore ever resolves. A consumer on net5 or later resolves the **net5.0 group** instead, which
brings System.Windows.Extensions 5.0.0 and with it System.Drawing.Common 5.0.0. Asked about the adapter
projects, `dotnet list package --vulnerable` reported nothing critical. Asked about the net8.0 store test
projects, which reference the adapters as a user does, it reported it immediately.

An audit of the whole solution found three more:

| Advisory | Package | Severity | Reached | Through |
|---|---|---|---|---|
| GHSA-cmhx-cq75-c4mj | System.Text.RegularExpressions 4.3.0 | High | all four adapter packages | System.Data.Common 4.3.0 |
| GHSA-pgww-w46g-26qg | AngleSharp 1.4.0 | Moderate | the `trinity-vocab` tool, which bundles it | dotNetRdf.Core, in Trinity.Vocabulary, which the 1.7.1 pin did not reach |
| GHSA-q939-rpr3-3284, GHSA-mggc-4xg6-vcxf | SSH.NET 2023.0.0 | High | test projects and benchmarks, never shipped | Testcontainers 3.10.0 |

The .NET 10 SDK's NuGet audit had been printing NU1904 and NU1903 warnings on every restore. Nobody
read them: they sat among the build's other warnings, and nothing failed.

## Decision

### The dependencies

- **GraphDB, Fuseki and Oxigraph drop System.Security.Permissions and System.Data.Common.** Neither
  package is used by their source; the references were copied from Virtuoso.
- **Virtuoso keeps System.Security.Permissions, at 10.0.x.** Its vendored
  `OpenLink.Data.Virtuoso.dll` binds to that assembly — its metadata lists AssemblyRefs `netstandard
  2.0.0.0` and `System.Security.Permissions 4.0.2.0`, for `CodeAccessPermission`, `PermissionState` and
  `IUnrestrictedPermission`, none of which netstandard2.0 provides. From 8.0 on, System.Windows.Extensions
  has no dependencies, so System.Drawing.Common leaves the graph altogether rather than moving to a
  patched version. 10.0 is the supported LTS line; .NET 8 left support on 2026-11-10.
- **No adapter references System.Data.Common.** netstandard2.0 provides the ADO.NET types Trinity.Virtuoso
  uses, and the vendored DLL does not reference the assembly. The package was the only route
  System.Text.RegularExpressions 4.3.0 had into the graph.
- **Trinity.Vocabulary references AngleSharp itself.** Any project that reaches dotNetRdf.Core other
  than through Trinity must, or it resolves the 1.4.0 dotNetRdf.Core asks for.
- **Testcontainers goes to 4.17.0**, the first to depend on the fixed SSH.NET 2026.0.0. Its parameterless
  `ContainerBuilder()` is obsolete, so the fixtures pass the image to the constructor, and `stores.py`
  — which reads the pinned images from the fixtures — matches that spelling, guarded by `test_stores.py`.

### The gate

`.github/scripts/vulnerabilities.py` runs `dotnet list package --vulnerable --include-transitive --format
json` over **the whole solution** and fails on any **High or Critical** advisory. Moderate and Low are
reported in the same table and never fail.

- **The whole solution, test projects included.** This is not about hygiene in the tests. The net8.0
  store test projects are the only projects that resolve the adapters' dependencies as a consumer does,
  so they are where an adapter's consumer-facing graph becomes visible. Scanning only what ships would
  have reported rc.5 clean.
- **A script, not `WarningsAsErrors` on NU1903/NU1904.** A restore error would break every local build
  the moment an advisory is published, including the build needed to fix it. The script fails one CI job
  and a commit's check, and names the package, version, advisory and every project the package reaches.
- **A failure to produce the report is never read as clean.** `dotnet list package` exits 0 whatever it
  finds, and an unreachable source makes it print plain text rather than JSON. Either way the script
  fails in CI and reports itself *not run* locally — offline is an absent optional input there, as a
  missing `npx` is for the duplication scan (ADR-0049). An unknown severity name is gated.
- **An accepted-advisory list, empty now.** An advisory with no fix upstream goes in `ACCEPTED`, keyed
  by advisory id, with a reason a reviewer can check. It is listed every run with that reason, and the
  script says so when nothing in the graph has it any more. The alternative — switching the gate off
  until upstream ships — is how a second advisory slips through behind the first.
- **Its own CI job**, `vulnerabilities`, not a step of `build`: an advisory published overnight reddens
  that check alone and hides no test result. `check.sh` runs the same script after the build, while
  the store suites run.

## Consequences
- **CI can turn red with no code change**, when an advisory is published against something already in
  the graph. That is the point: the alternative was a warning nobody read. The fix is a
  bump or a pin in `Directory.Packages.props`, removing the reference that brings the package in, or a
  reasoned `ACCEPTED` entry.
- **An adapter must have a net8+ project that references it**, or its consumer-facing graph is never
  resolved and the gate cannot see it. All four have one, their store test project.
- Virtuoso consumers now get System.Security.Permissions 10.0.x. One that pins it lower gets NU1605
  (package downgrade) and must raise the pin. The other three adapters no longer bring it at all.
- The gate takes about 8 seconds after a restore and adds no package or tool.

## Related
- [0009](0009-supported-store-backends.md) — the vendored, recompiled OpenLink provider.
- [0036](0036-integration-tests-testcontainers.md), [0044](0044-store-suites-green-and-in-ci.md) — the
  store suites whose projects make the consumer graph visible.
- [0049](0049-quality-gates-and-pre-commit-check.md) — the gates this joins, and the not-run rule for
  absent optional inputs.
