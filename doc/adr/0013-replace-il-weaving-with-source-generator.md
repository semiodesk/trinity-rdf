# 0013. Replace IL weaving with a Roslyn source generator (partial properties)

Date: 2026-07-13

## Status
**Proposed** — no decision made yet; captures the leading revival option for discussion.

## Context
The compile-time IL weaver ([0003](0003-mapping-via-il-weaving.md)) is the biggest
modern-.NET liability: it rewrites built assemblies post-build, requires full PDBs
([0004](0004-force-full-pdb-symbols.md)), is Windows-centric, and ships as a .NET
Framework EXE toolchain. DevHub already sidesteps it by hand-writing exactly the members
the weaver injects (`PropertyMapping<T>` fields + `GetValue`/`SetValue` + `GetTypes()`),
which is a working spec of the desired output.

A Roslyn source generator is *additive only* — it cannot rewrite a normal auto-property.
But **C# 13 (`.NET 9+`) `partial` properties** let a generator supply the accessor body:
the author writes a `partial` property with no body; the generator emits the implementing
half. Both external consumers are already on C# 13 (elxgen net9; DevHub `LangVersion 13.0`).

## Decision (proposed)
Add an incremental source generator that, for types carrying `[RdfClass]`/`[RdfProperty]`,
emits into a partial class: the `PropertyMapping<T>` backing field (with a field initializer,
replacing constructor injection), the implementing `partial` getter/setter calling
`GetValue`/`SetValue`, and the `GetTypes()` override; plus `INotifyPropertyChanged` support.
The runtime `Resource`/`PropertyMapping` engine is unchanged, so generated and woven
assemblies are behaviorally equivalent. Emit diagnostics for `[RdfProperty]` on non-`partial`
members (turning today's silent weaving miss into a compile error).

Authoring change: mapped types become `partial`, mapped properties gain `partial`
(a two-keyword edit versus DevHub's full hand-written members).

## Consequences
- Deterministic, cross-platform, WASM-safe; no post-build rewrite, no PDB hack, no EXE toolchain.
- Better DX: compile-time diagnostics, IDE-live generation.
- Requires C# 13 / .NET 9 SDK toolchain to build consumers; a hard floor on tooling.
- Small breaking change to the authoring model (adding `partial`); the *runtime* contract
  is preserved. cilg can run in parallel during migration and be validated against the generator.

## Validation plan
Take an `elxgen` `Resource` class, add `partial`, generate, and diff the generated members
against what `cilg` currently produces in `Elxos.Model.dll` to confirm parity before switching.

## Related
- [0002](0002-attribute-based-object-mapping.md), [0003](0003-mapping-via-il-weaving.md),
  [0004](0004-force-full-pdb-symbols.md), [0012](0012-packaging-and-distribution.md)
