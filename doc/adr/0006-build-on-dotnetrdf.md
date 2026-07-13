# 0006. Build on dotNetRDF as the RDF/SPARQL engine

Date: 2026-07-13 (decision original to 2015–2020 design)

## Status
Accepted

## Context
Trinity is an object-mapping and application layer, not a triple engine. It needs a
mature library for parsing/serializing RDF, an in-memory triple store, SPARQL query
and update execution, and connectors to remote stores.

## Decision
Trinity builds on **dotNetRDF**, pinned to **2.7.0**. dotNetRDF provides the in-memory
`TripleStore` with the Leviathan query/update processors, RDF (de)serialization used by
`IStore.Read`/`Write`, and connector base types reused by the store providers (e.g.
GraphDB extends dotNetRDF's Sesame connector). Newtonsoft.Json 13 is used for resource
(de)serialization.

## Consequences
- Large amounts of hard RDF work are delegated to a proven library.
- The version is pinned to the **v2 line**. dotNetRDF 3.x is a substantial breaking
  change (namespaces, async query/update APIs, connector removals, `Options` statics).
  The store adapters all sit on v2 APIs.
- dotNetRDF 2.7 has a netstandard2.0 build, so it is not itself a modern-.NET blocker;
  upgrading to 3.x is deferrable.

## Revival notes
Keep 2.7.0 for now. Treat a 3.x upgrade as its own scoped effort once the build and
codegen are stable, since it touches every store adapter. Newtonsoft.Json → System.Text.Json
is a possible later cleanup, not urgent.

## Related
- [0008](0008-store-model-abstraction.md), [0009](0009-supported-store-backends.md)
