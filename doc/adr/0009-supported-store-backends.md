# 0009. Supported store backends; Stardog removed

Date: 2026-07-13

## Status
Accepted

## Context
Trinity targets a range of deployments: quick in-memory prototyping, remote SPARQL
endpoints, and production triple stores. Each backend is a separate provider so its
dependencies do not burden the core.

## Decision
Shipped/maintained backends:
- **in-memory (dotNetRDF / Leviathan)** — `provider=dotnetrdf`; the reference store, fully
  functional, used by tests and the ontology generator.
- **SPARQL endpoint** — `provider=sparqlendpoint`; query remote endpoints.
- **Virtuoso** (`Trinity.Virtuoso`) — functional and cross-platform; uses the OpenLink ADO.NET
  provider vendored as a **self-recompiled `OpenLink.Data.Virtuoso.dll` targeting netstandard2.0**
  (via HintPath), plus a vendored copy of dotNetRDF's dropped `VirtuosoManager`. Builds and loads
  on Linux/macOS — no longer a modernization blocker.
- **GraphDB** (`Trinity.GraphDB`) — functional, most recently maintained; custom connector
  over dotNetRDF's Sesame connector with `infer=true` reasoning support; no transactions.
- **Fuseki** (`Trinity.Fuseki`) — functional via dotNetRDF's Fuseki connector; no
  transactions. Revived and now green on the shared store suite
  ([0043](0043-fuseki-store-revival.md)), which also fixed the
  `CreateModelGroup(params IModel[])` bug (it built an empty group).

Stardog support was **removed** (`git`: "Removed Stardog support"). Note: test projects
under `tests/Trinity.Tests.Stardog` and `provider=stardog` references still exist even
though there is **no Stardog provider project** — stale.

## Consequences
- Backend choice is a per-provider dependency decision; core stays lean.
- Virtuoso is on the critical path (both external consumers use it); with the provider
  recompiled to netstandard2.0 it is cross-platform and no longer the modernization risk it
  once appeared to be.
- The dangling Stardog references are misleading and should be cleaned up.

## Revival notes
The OpenLink provider is a self-recompiled netstandard2.0 assembly (cross-platform); document
how it is rebuilt and keep that source/recipe available. The Fuseki/GraphDB transaction stubs now
return a `NoOpTransaction` rather than `null` ([0039](0039-resource-write-semantics.md)), and the
Fuseki `CreateModelGroup` bug is fixed ([0043](0043-fuseki-store-revival.md)).
Remove or resurrect Stardog explicitly.

## Related
- [0008](0008-store-model-abstraction.md), [0006](0006-build-on-dotnetrdf.md)
