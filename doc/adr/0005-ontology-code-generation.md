# 0005. Generate C# ontology vocabularies from RDF/OWL at build time

Date: 2026-07-13 (decision original to 2015–2020 design)

## Status
Accepted — under review for the revival (see [0014](0014-ontology-generator-modernization.md))

## Context
Working with ontologies means referring to many term URIs. Hand-writing URI constants
is tedious and error-prone, and loses IDE autocompletion/IntelliSense and documentation
hints. We wanted ontology terms available as strongly-typed C# with XML docs, kept in
sync with the source ontologies as part of the build.

## Decision
`OntologyGenerator` (`Trinity.OntologyGenerator`, assembly `OntologyGenerator.exe`)
reads RDF/OWL files (Turtle/N3/NTriples/TriG/RDF-XML) via dotNetRDF into an in-memory
store, enumerates terms, and emits, per ontology, two C# shapes:
- a typed class `: Ontology` exposing `Namespace`, `Prefix`, and one
  `Class`/`Property`/`Resource` static per term (kind chosen from `rdf:type`);
- a string-constant class of `const string` term URIs.

It runs as an MSBuild task (`GenerateOntologyTarget`, before `CoreCompile`) wired via
`build/Semiodesk.Trinity.targets`, emitting `Ontologies/Ontologies.g.cs`. Term names
are sanitized (C# keyword escaping, illegal-char replacement, collision suffixes).
See `Trinity.OntologyGenerator/OntologyGenerator.cs`, `Templates.cs`, `Task/GenerateOntologyTask.cs`.

## Consequences
- When configured, vocabularies stay in sync with the ontology files and get IntelliSense.
- Like the weaver, it is a **net461 EXE MSBuild task** with legacy config coupling
  ([0011](0011-configuration-model.md)); the prebuilt binary loads under `dotnet build`
  but building it from source needs the net461 pack.
- In practice both external consumers **bypass it** and hand-write their vocabulary
  classes, because it was easier than configuring the generator — a signal that the
  configuration/authoring ergonomics need rework.

## Revival notes
Vocabulary generation is a natural fit for a Roslyn source generator consuming `.ttl`
files as `AdditionalFiles`, or a modern netstandard2.0 task / `dotnet tool`. See
[0014](0014-ontology-generator-modernization.md).

## Related
- [0011](0011-configuration-model.md), [0006](0006-build-on-dotnetrdf.md),
  [0014](0014-ontology-generator-modernization.md)
