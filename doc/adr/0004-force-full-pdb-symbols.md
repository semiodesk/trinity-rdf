# 0004. Force full PDB symbols so the weaver can rewrite assemblies

Date: 2026-07-13 (decision original to the .NET Core era of the project)

## Status
Accepted — a primary revival blocker

## Context
The IL weaver ([0003](0003-mapping-via-il-weaving.md)) reads and rewrites an
assembly's symbols via Mono.Cecil. Mono.Cecil's PDB reader crashed on the
**portable PDBs** that .NET Core / modern SDK projects emit by default (the
`Semiodesk.Trinity.targets` comment references the class of failure seen with the
NUnit VS adapter).

## Decision
`build/Semiodesk.Trinity.targets` force-sets `<DebugType>Full</DebugType>` for every
target framework except `netcoreapp1.0`, so that the weaver reads a Windows-style
full PDB it can handle.

## Consequences
- Weaving succeeds on Windows with full PDBs.
- **Full PDBs are Windows-only.** This makes the choice incompatible with:
  - Blazor WebAssembly / cross-platform builds (need portable PDBs),
  - Linux/macOS CI,
  - deterministic builds and SourceLink.
- This single line is the concrete reason the two external consumers diverge:
  DevHub (Blazor WASM, cross-platform) forces `DebugType=portable` and **disables the
  weaver**, hand-writing the mapping; elxgen (Windows) lets the default stand and the
  weaving works.

## Revival notes
Resolving this is the crux of the modern-.NET story. Options: upgrade Mono.Cecil and
support portable PDBs and stop forcing Full; or eliminate post-build symbol rewriting
altogether by moving to a source generator ([0013](0013-replace-il-weaving-with-source-generator.md)),
which never touches symbols.

## Related
- [0003](0003-mapping-via-il-weaving.md), [0013](0013-replace-il-weaving-with-source-generator.md)
