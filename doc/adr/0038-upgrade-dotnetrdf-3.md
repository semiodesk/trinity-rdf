# 0038. Upgrade to dotNetRDF 3.x (Core / Client / Inferencing split)

Date: 2026-08-03

## Status
Accepted (2.0) — supersedes the version pin in [0006](0006-build-on-dotnetrdf.md).

## Context
dotNetRDF was pinned to **2.7.0** for the whole revival. [0006](0006-build-on-dotnetrdf.md) deferred
the 3.x jump as "its own scoped effort once the build and codegen are stable, since it touches every
store adapter". Shipping 2.0 on a 2019-era engine would have meant a second breaking release later, so
the upgrade belongs in the 2.0 window. [0037](0037-linq-provider-rebuild.md) was done first
deliberately, to get the LINQ layer out of the blast radius.

## Decision
Upgrade to **3.5.2**, taking the package split:

| Package | Referenced by | Why |
|---|---|---|
| `dotNetRdf.Core` | `Trinity` | RDF/SPARQL engine, in-memory store, parsers/writers |
| `dotNetRdf.Client` | `Trinity` + all three store projects | HTTP storage connectors; `Trinity` needs it for the built-in SPARQL-endpoint store, `Trinity.Virtuoso` because `IStorageProvider` lives there |
| `dotNetRdf.Inferencing` | `Trinity` | `RdfsReasoner` moved out of Core |

All three target **netstandard2.0**, so Trinity's TFM is unchanged. `Newtonsoft.Json` moves to 13.0.4
(Core's floor).

Code changes:
- **Graph naming.** `IGraph.Name` (an `IRefNode`) replaces `BaseUri` as a graph's identity; it is
  immutable, so graphs are now created with their name (`new Graph(graphUri)`) instead of being named
  afterwards. `ListModels` reads `Name` and yields only URI-named graphs; store removal uses
  `Remove(IRefNode)`. `LoadSchema` discovers the ontology IRI *after* parsing, so it re-homes the
  triples into a graph created with that name.
- **Inferencing.** `TripleStore` → `InferencingTripleStore` (Core's store no longer accepts an engine).
- **Language-tagged literals.** Under RDF 1.1, which 3.x follows, a lang-tagged literal also carries
  the `rdf:langString` datatype, so `ParseCellValue` must test the language *before* the datatype or it
  misreads such a literal as a plain string.
- **Virtuoso.** The vendored `VirtuosoManager` needed only: the two new abstract members
  (`ListGraphNames()`, `UpdateGraph(IRefNode, …)`) delegating to the existing overloads, `SaveGraph`
  keying off `Name`, `LoadGraph` no longer renaming the caller's graph, and a `ToBindings(Set)` helper
  because `SparqlResult` no longer takes an algebra `Set`.
- **GraphDB.** `Tools.HttpDebugRequest/Response` are gone (HttpClient logging replaces them) and
  `Options.UseBomForUtf8` → `new UTF8Encoding(false)`, its former default. The rest of the Sesame
  connector's protected surface survived, so no rewrite was needed.

## Consequences
- **Far smaller than planned.** 3.x kept the `Uri`/`string` storage overloads and `SparqlRemoteEndpoint`
  as `[Obsolete]` rather than removing them, so the feared ~1550-line Virtuoso re-port and the
  endpoint-store rewrite did not happen. The whole solution compiled after ~10 fixes.
- **The dangerous part was silent, not a compile error.** `graph.BaseUri = uri` still compiles but no
  longer names the graph, so a bare version bump built cleanly and then failed 203 of 392 tests
  ("The Graph you tried to add already exists in the Graph Collection" — every graph was unnamed).
- **Naming at construction is not sufficient for the HTTP connectors.** A parsed `@base` overwrites
  `Graph.BaseUri`, and dotNetRDF's Sesame/Fuseki connectors still derive the graph they *write* to from
  `BaseUri` rather than `Name`. That silently wrote GraphDB reads into a graph named after the document
  base; the read paths therefore re-assign `BaseUri` after parsing. Worth remembering as an upstream
  inconsistency, and worth re-testing on future 3.x releases.
- **The LINQ layer needed no migration at all** — 104/104 corpus cases and the 94-case parity suite
  passed untouched, because the provider emits SPARQL strings rather than using dotNetRDF's Query
  Builder ([0037](0037-linq-provider-rebuild.md)). That was the payoff of sequencing it first.
- `SparqlRemoteEndpoint` and `UriLoader` are obsolete-but-working; migrating them to
  `SparqlQueryClient`/`Loader` is deferred because both are async-first and `IStore` is synchronous.
- Store pass rates are unchanged from 2.7: in-memory **384 passed / 8 skipped**, Virtuoso **90/97**,
  GraphDB **97/101**, Fuseki **4/86**. Fuseki's pre-existing connector bug ([0009](0009-supported-store-backends.md))
  is *not* fixed by 3.x.

## Follow-ups
- Migrate off the obsolete `SparqlRemoteEndpoint`/`UriLoader` when an async store surface is on the table.
- Re-check the quarantined `DateTime` round-trip bug (ADR-0026) now that literal handling has moved.
- Revisit whether the in-memory inferencing tests (ADR-0022) behave differently under
  `InferencingTripleStore`.

## Related
- [0006](0006-build-on-dotnetrdf.md) (version pin superseded), [0037](0037-linq-provider-rebuild.md),
  [0009](0009-supported-store-backends.md), [0036](0036-integration-tests-testcontainers.md).
