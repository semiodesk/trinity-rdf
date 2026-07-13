# 0012. One NuGet package bundling libraries + build tools via `.targets`

Date: 2026-07-13 (decision original to 2015–2020 design)

## Status
Accepted

## Context
The mapping model only works if the build-time code generation ([0003](0003-mapping-via-il-weaving.md),
[0005](0005-ontology-code-generation.md)) runs automatically in the consumer's build.
Consumers should just `Install-Package Semiodesk.Trinity` and get both the runtime library
and the build behavior.

## Decision
`Trinity.csproj` packs a single `Semiodesk.Trinity` NuGet package containing:
- `lib/netstandard2.0/Semiodesk.Trinity.dll` (the runtime library);
- `build/Semiodesk.Trinity.targets`, auto-imported by NuGet, which wires the
  `CilGeneratorTarget` (post-build weave) and `GenerateOntologyTarget` (pre-compile);
- `tools/` containing `cilg.exe`, `OntologyGenerator.exe`, and their dependencies
  (Mono.Cecil, dotNetRDF, and ~120 `System.*` facade DLLs).

Store providers ship as separate packages (`Semiodesk.Trinity.Virtuoso`, etc.).
See the `CustomContentTarget` in `Trinity/Trinity.csproj` and
`Trinity/Targets/Semiodesk.Trinity.targets`.

## Consequences
- Zero-config for consumers: referencing the package makes mapping "just work" (on Windows).
- The package carries a **.NET Framework EXE toolchain plus a large facade-DLL payload**;
  the `.targets` uses `UsingTask AssemblyFile="…cilg.exe"` and forces full PDBs
  ([0004](0004-force-full-pdb-symbols.md)).
- The repo version is `1.0.3.50`, but the published package both external consumers depend on
  is **1.0.3.77** (an AppVeyor build; bundled `cilg.exe` v1.0.0.8, Feb 2023). The branch/commit
  that produced 1.0.3.77 should be identified before any new release, to avoid regressing
  what consumers already ship.

## Revival notes
Replace the EXE-in-`tools/` mechanism with an analyzer/source-generator package
([0013](0013-replace-il-weaving-with-source-generator.md),
[0014](0014-ontology-generator-modernization.md)), or at minimum ship managed netstandard2.0
task DLLs loaded via `UsingTask`. Reconcile versioning and republish deliberately.

## Related
- [0003](0003-mapping-via-il-weaving.md), [0004](0004-force-full-pdb-symbols.md),
  [0005](0005-ontology-code-generation.md), [0010](0010-target-frameworks.md)
