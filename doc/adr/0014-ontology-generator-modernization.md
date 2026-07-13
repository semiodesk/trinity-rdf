# 0014. Reimplement ontology vocabulary generation as a compile-time generator/tool

Date: 2026-07-13

## Status
**Proposed** — no decision made yet.

## Context
`OntologyGenerator` ([0005](0005-ontology-code-generation.md)) is a net461 EXE MSBuild task
with legacy `ConfigurationManager`/`app.config` coupling ([0011](0011-configuration-model.md))
and buggy config detection. Both external consumers bypass it and hand-write vocabularies —
evidence the mechanism's ergonomics failed, not that vocab generation is unwanted.

## Decision (proposed)
Reimplement vocabulary generation for modern .NET. Two shapes are viable:
1. **Roslyn source generator** consuming `.ttl` files as `AdditionalFiles`, with prefix/
   namespace supplied via MSBuild item metadata (`CompilerVisibleItemMetadata`), replacing
   `ontologies.config`. Best DX (live, no files on disk, cross-platform). The tradeoff is the
   RDF parser runs inside the compiler/IDE — use a lightweight Turtle parser, or dotNetRDF
   (netstandard2.0) bundled into the analyzer and accept the heavier load.
2. **netstandard2.0 MSBuild task / `dotnet tool`** that emits `.g.cs` pre-compile — keeps the
   heavy parser out of the IDE, retains full multi-format fidelity, at the cost of the live DX.

Either way: drop the `app.config` legacy path; forbid build-time network fetches
(resolve `<websource>` to local files at author time); sort terms for deterministic output.
Reuse the existing `Templates.cs` emission (typed `Ontology` class + string-constant class)
largely unchanged.

## Consequences
- Cross-platform vocab generation with no net461 EXE.
- Option 1 packages naturally alongside the mapping generator
  ([0013](0013-replace-il-weaving-with-source-generator.md)) in one analyzer package, and can
  expose the `prefix:term → URI` map for compile-time validation of `[RdfProperty(Vocab.X)]`.
- Lower priority than the mapping generator, since consumers currently hand-write vocab and
  are unblocked.

## Related
- [0005](0005-ontology-code-generation.md), [0011](0011-configuration-model.md),
  [0013](0013-replace-il-weaving-with-source-generator.md)
