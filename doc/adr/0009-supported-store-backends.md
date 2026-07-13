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
- **Virtuoso** (`Trinity.Virtuoso`) — functional; bound to the proprietary OpenLink
  ADO.NET provider (vendored `OpenLink.Data.Virtuoso.dll` via HintPath) plus a vendored
  copy of dotNetRDF's dropped `VirtuosoManager`. Highest modernization risk.
- **GraphDB** (`Trinity.GraphDB`) — functional, most recently maintained; custom connector
  over dotNetRDF's Sesame connector with `infer=true` reasoning support; no transactions.
- **Fuseki** (`Trinity.Fuseki`) — functional via dotNetRDF's Fuseki connector; no
  transactions; has a known `CreateModelGroup(params IModel[])` bug (builds an empty group).

Stardog support was **removed** (`git`: "Removed Stardog support"). Note: test projects
under `tests/Trinity.Tests.Stardog` and `provider=stardog` references still exist even
though there is **no Stardog provider project** — stale.

## Consequences
- Backend choice is a per-provider dependency decision; core stays lean.
- Virtuoso is both on the critical path (both external consumers use it) and the riskiest
  to modernize.
- The dangling Stardog references are misleading and should be cleaned up.

## Revival notes
Verify the OpenLink Virtuoso provider on modern runtimes; consider a SPARQL-protocol-over-HTTP
fallback for Virtuoso. Fix the Fuseki/GraphDB transaction stubs and the Fuseki model-group bug.
Remove or resurrect Stardog explicitly.

## Related
- [0008](0008-store-model-abstraction.md), [0006](0006-build-on-dotnetrdf.md)
