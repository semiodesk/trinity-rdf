# 0010. Target frameworks: netstandard2.0 core, net461 tools, net472 tests

Date: 2026-07-13

## Status
Accepted — partially a revival blocker

## Context
The project straddles the .NET Framework → .NET Core/.NET 5+ transition. Maintenance
stopped before the frameworks unified, leaving a mix of project styles and targets.

## Decision (as it stands today)
- **Core + store libraries** (`Trinity`, `Trinity.Virtuoso`, `Trinity.Fuseki`,
  `Trinity.GraphDB`): SDK-style, **netstandard2.0**.
- **Build tools** (`Trinity.CilGenerator`, `Trinity.OntologyGenerator`): legacy non-SDK
  projects, **net461**, `packages.config`, output EXEs shipped in the NuGet `tools/` folder.
- **Tests** (`Trinity.Tests` and the store test projects): **net472**, NUnit.

## Consequences
- Core is broadly consumable (netstandard2.0 loads on .NET Framework and modern .NET).
  Keep new core code netstandard2.0-compatible unless we deliberately multi-target.
- On a machine with only the .NET SDK, `dotnet build Trinity/Trinity.csproj` and the store
  projects **succeed**; the full solution **fails** because the net461 tool projects need
  the .NET Framework 4.6.1 targeting pack (MSB3644), and the net472 tests need the 4.7.2 pack.
- `dotnet test` cannot run the suite as-is (net472).
- The prebuilt tool EXEs still *run* under `dotnet build` (see [0003](0003-mapping-via-il-weaving.md));
  the TFM problem is about *building them from source*.

## Revival notes
See [0015](0015-modernize-target-frameworks-and-ci.md): convert tools to SDK-style (add the
Framework reference-assemblies package or retarget), retarget tests to net8/net9, and decide
whether the core multi-targets (e.g. `netstandard2.0;net8.0`).

## Related
- [0003](0003-mapping-via-il-weaving.md), [0012](0012-packaging-and-distribution.md),
  [0015](0015-modernize-target-frameworks-and-ci.md)
