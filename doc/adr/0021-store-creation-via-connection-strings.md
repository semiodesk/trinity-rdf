# 0021. Stores are created from connection strings

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
Store construction should be uniform across backends and configurable without code changes,
in an idiom familiar to .NET developers — analogous to ADO.NET connection strings.

## Decision
`StoreFactory.CreateStore(connectionString)` parses a `provider=…;key=…;…` string (regex in
`ParseConfiguration`, `Trinity/Stores/StoreFactory.cs`) and dispatches to the matching
`StoreProvider`. Convenience factories wrap common cases (`CreateMemoryStore()` ⇒
`provider=dotnetrdf`). Connection strings may also be read from
`ConfigurationManager.ConnectionStrings` ([0011](0011-configuration-model.md)). Backends beyond
the two built-ins (`dotnetrdf`, `sparqlendpoint`) must be registered with
`StoreFactory.LoadProvider<T>()` before use ([0008](0008-store-model-abstraction.md)).

## Consequences
- One idiom selects and configures any backend; switching memory ↔ Virtuoso is a string change.
- The string format is **provider-defined and loosely typed** (regex-parsed); mistakes surface
  at runtime rather than compile time.
- Couples to the manual provider-registration story (and the dead MEF attributes) in
  [0008](0008-store-model-abstraction.md).

## Related
- [0008](0008-store-model-abstraction.md), [0011](0011-configuration-model.md)
