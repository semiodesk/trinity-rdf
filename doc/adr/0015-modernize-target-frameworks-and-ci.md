# 0015. Modernize target frameworks, build, and CI

Date: 2026-07-13

## Status
**Proposed** — no decision made yet.

## Context
The current TFM mix ([0010](0010-target-frameworks.md)) means the full solution does not
build with the .NET SDK alone (net461 tools, net472 tests need Framework packs), tests can't
run under `dotnet test`, and the CI/build orchestration (`build.cake`, `appveyor.yml`,
VS2019 image, `nuget restore` + full MSBuild) is stale and full-framework-bound.

## Decision (proposed)
- Convert `Trinity.CilGenerator` / `Trinity.OntologyGenerator` to SDK-style projects; either
  add `Microsoft.NETFramework.ReferenceAssemblies` so they build without a locally installed
  pack, or supersede them entirely with the generator packages
  ([0013](0013-replace-il-weaving-with-source-generator.md),
  [0014](0014-ontology-generator-modernization.md)).
- Retarget test projects from net472 to a modern TFM (net8.0/net9.0) so `dotnet test` runs
  the suite cross-platform.
- Decide whether the core multi-targets (`netstandard2.0;net8.0`) or stays netstandard2.0-only.
- Replace `build.cake` + AppVeyor with a `dotnet`-based CI (GitHub Actions): restore, build,
  test, pack — no full MSBuild dependency.
- Reconcile package versioning (repo `1.0.3.50` vs published `1.0.3.77`, see
  [0012](0012-packaging-and-distribution.md)) before publishing.

## Consequences
- A green, cross-platform build and test loop on the .NET SDK alone — the precondition for
  iterating quickly on the revival.
- Some churn in project files and CI; sequence it so the core/stores stay buildable throughout.

## Sequencing
Retarget tests first (fastest path to a runnable safety net), then tackle the tools via the
generator ADRs, then CI, then the versioning reconciliation and a deliberate republish.

## Related
- [0010](0010-target-frameworks.md), [0012](0012-packaging-and-distribution.md),
  [0013](0013-replace-il-weaving-with-source-generator.md), [0014](0014-ontology-generator-modernization.md)
