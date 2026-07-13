# 0013. Replace IL weaving with a Roslyn source generator (partial properties)

Date: 2026-07-13

## Status
**Accepted** — implemented as `Trinity.Generator` and shipped in **Trinity 2.0**, where it
replaces the cilg weaver entirely (see Outcome). `INotifyPropertyChanged` was dropped
([0035](0035-remove-inotifypropertychanged.md)), so the generator emits only the
`PropertyMapping<T>` field, `GetValue`/`SetValue` accessors, and `GetTypes()`.

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

## Outcome (implemented in Trinity 2.0)
`Trinity.Generator` is an `IIncrementalGenerator` that emits, for each `partial` `[RdfProperty]`
property, a `protected PropertyMapping<T>` field + implementing getter/setter, and a `GetTypes()`
override for each `partial` `[RdfClass]` class. It ships in the `Semiodesk.Trinity` package under
`analyzers/dotnet/cs`, so referencing the package applies it automatically — no build-time tools,
no `.targets`, no IL weaving.

Validated by migrating the **entire** `Trinity.Tests` suite to `partial` mapped classes: it now
passes with **no weaver** (260 passed / 16 skipped, identical to the old weave run) across scalars,
collections (default instances), language-invariant strings, resource references, multiple
`[RdfClass]`, and inheritance (including `GetTypes`-only subclasses and interface implementers).

**Trinity 2.0 is a breaking release:** mapped resource classes and their `[RdfProperty]` properties
must be declared `partial` (min C# 13 / .NET 9 SDK). The cilg weaver, its `.targets`, and the
`tools/` payload were removed. Consumers on the auto-property style migrate by adding the `partial`
keyword. A future diagnostic can flag `[RdfProperty]` on a non-`partial` member.

## Related
- [0002](0002-attribute-based-object-mapping.md), [0003](0003-mapping-via-il-weaving.md),
  [0004](0004-force-full-pdb-symbols.md), [0012](0012-packaging-and-distribution.md)
