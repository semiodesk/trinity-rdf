# 0034. RDF (de)serialization formats and JSON-LD resource converter

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
Resources and graphs must move in and out of the store and across the wire in standard RDF
syntaxes, and integrate with JSON tooling used by application layers.

## Decision
`IStore`/`IModel` `Read`/`Write` operate over an `RdfSerializationFormat` enum
(`Trinity/RdfSerializationFormat.cs`) — Turtle, TriG, N-Triples, RDF/XML, JSON-LD, etc. — using
dotNetRDF writers/parsers (e.g. `CompressingTurtleWriter`, `TriGWriter`). A
`JsonResourceConverter` (`Trinity/Serialization/`) provides Newtonsoft.Json (de)serialization of
resources. `Resource.ListValues(forSerialization: true)` produces the serialization view
(mapped **and** unmapped properties — [0017](0017-resources-open-mapped-and-dynamic.md)).

## Consequences
- Interop with the common RDF syntaxes and with JSON pipelines (consumers such as `elxgen`
  round-trip through `RdfSerializationFormat.Turtle` and `JsonLd`).
- JSON handling is on **Newtonsoft.Json** ([0006](0006-build-on-dotnetrdf.md)); a possible later
  move to `System.Text.Json`.

## Related
- [0006](0006-build-on-dotnetrdf.md), [0017](0017-resources-open-mapped-and-dynamic.md)
