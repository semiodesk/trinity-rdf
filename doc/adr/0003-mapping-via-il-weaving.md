# 0003. Implement mapping via compile-time IL weaving (cilg / Mono.Cecil)

Date: 2026-07-13 (decision original to 2015–2020 design)

## Status
Accepted — under active review for the revival (see [0013](0013-replace-il-weaving-with-source-generator.md))

## Context
Given the attribute-based model ([0002](0002-attribute-based-object-mapping.md)),
the accessor bodies that connect a property to RDF have to come from somewhere.
Two classic options: runtime reflection/`Reflection.Emit`, or compile-time code
generation. The authors chose compile-time byte-code manipulation for performance
and to keep authoring to plain auto-properties, as stated in the README
("Using byte-code manipulation, we implement the code required for the mapping
during program compilation").

## Decision
A dedicated tool, `cilg` (`Trinity.CilGenerator`, assembly `cilg.exe`), rewrites the
*already-compiled* consumer assembly using **Mono.Cecil 0.11.2**. For each mapped type
it: adds a `PropertyMapping<T> <name>k__MappingField`, removes the compiler backing
field, injects the mapping-field initializer into every constructor, rewrites each
auto-property getter/setter to `GetValue`/`SetValue`, and synthesizes the `GetTypes()`
override from `[RdfClass]`. It also weaves `INotifyPropertyChanged` for
`[NotifyPropertyChanged]`.

`cilg` runs as an MSBuild task wired through the package's
`build/Semiodesk.Trinity.targets` (`CilGeneratorTarget`, `AfterTargets="Build"`).
See `Trinity.CilGenerator/ILGenerator.cs`, `Trinity.CilGenerator/Tasks/*`,
`Trinity/Targets/Semiodesk.Trinity.targets`.

## Consequences
- Authors write clean POCOs; the mapping is fast (no per-access reflection).
- A **post-build IL rewrite** is hostile to modern build expectations: it fights
  deterministic builds, SourceLink, and portable PDBs (see
  [0004](0004-force-full-pdb-symbols.md)), and is hard to reason about.
- The tool is a **.NET Framework net461 EXE** (see [0010](0010-target-frameworks.md)).
  Verified 2026-07: the *prebuilt* `cilg.exe` from the published NuGet **does load and
  run** as an MSBuild task under the .NET 10 SDK on Windows, and correctly weaves a
  net9 assembly (confirmed against `elxgen`). So the mechanism is not "broken" — but
  it is Windows-centric and fragile (see revival notes).
- Building `cilg` *from source* currently fails on a machine with only the .NET SDK
  (no net461 targeting pack); this is separate from running the shipped binary.

## Revival notes
The weaver works today on Windows but blocks cross-platform / Blazor WASM / reproducible
builds, mainly via the full-PDB requirement. DevHub disables it entirely and hand-writes
the woven members; elxgen relies on it. The proposed direction is to replace weaving with
a Roslyn source generator ([0013](0013-replace-il-weaving-with-source-generator.md)),
keeping the runtime `Resource`/`PropertyMapping` engine unchanged for wire compatibility.

## Related
- [0002](0002-attribute-based-object-mapping.md), [0004](0004-force-full-pdb-symbols.md),
  [0012](0012-packaging-and-distribution.md), [0013](0013-replace-il-weaving-with-source-generator.md)
