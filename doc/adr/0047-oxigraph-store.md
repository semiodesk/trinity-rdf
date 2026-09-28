# 0047. Oxigraph as a fourth store backend

Date: 2026-09-21

## Status
Accepted

## Context
The three existing backends — Virtuoso, GraphDB, Fuseki — are all heavyweight servers. Oxigraph
(Rust, RocksDB-backed) is the other end of that range: one static binary, a small container,
sub-second startup, strict SPARQL 1.1. It is the sensible choice for development, CI, and
deployments where standing up Virtuoso or GraphDB is disproportionate.

It is also the **thinnest** backend Trinity has: no reasoner, no client-visible transactions, no
authentication, and no dataset or repository concept — one server holds one dataset, so the host
names the store completely.

`CLAUDE.md` predicts what adding a backend does: *"expect it to find things — that is the point of
having more than one."* It found four, three of them in code that has shipped for years.

## Decision

### The adapter
`OxigraphStore : StoreBase` in the shape ADR-0043 left `FusekiStore` in — the twelve abstract
members, `IsReady` derived from the connector, an idempotent `Dispose`, `NoOpTransaction`, and
everything else inherited.

`OxigraphConnector : SparqlHttpProtocolConnector, IUpdateableStorage` — the same base
`FusekiConnector` uses, plus query, update and graph listing. **Not** a reused `FusekiConnector`:
that type derives all three endpoints from one URI and requires it to end in `/data`. Pointed at
Oxigraph it would resolve `/query` and `/update` correctly by coincidence of layout and resolve the
Graph Store endpoint to `/data`, where Oxigraph serves nothing. That is not a minor gap: of the
connector calls a Trinity store makes, **18 of 25 are Graph Store Protocol** — `HasGraph` (7),
`DeleteGraph` (5), `SaveGraph` (4), `LoadGraph` (2) — against one `Query` and one `Update`. The
majority of the adapter would have 404'd.

### Inferencing is refused, not ignored
`inferenceEnabled: true` throws `NotSupportedException`.

ADR-0022 permits a store to ignore the flag, and Fuseki does, having no per-query switch. Oxigraph
is a stronger case: there is no reasoner *at all*, so there is nothing to configure and nothing that
could ever honour it. ADR-0022's own Consequences say what ignoring costs — a capability the store
lacks *"silently no-ops with no discoverable signal"* — and an un-inferred answer is
indistinguishable from a correct one. `ILayeredModel` already throws on this same flag for this same
reason (ADR-0041).

This inverts the second half of two `LayeredModelMaterializationTest` cases. ADR-0042 has
materialization lift the *rewriting* refusal, because the view becomes one ordinary graph and
Trinity has nothing left to refuse; what the store then does is its own business. Oxigraph's
business is to refuse. Materialization lifts Trinity's refusal; it cannot conjure a reasoner.

## What covering a strict backend found

**1. dotNetRDF writes invalid RDF/XML.** Its writer emits a DTD whose entity values are unquoted —
`<!ENTITY ns0 http://example.org/>` — which is not well-formed XML. Oxigraph answers HTTP 400
(*"<!ENTITY values should be enclosed in double quotes"*); Fuseki, GraphDB and Virtuoso accept it,
which is why it has gone unnoticed. Reproduced with raw `curl`, independently of Trinity.

**2. …and prefixes Turtle with a byte order mark**, which is not valid at the head of a Turtle
document. Oxigraph reports it as *"not a valid subject or graph name"* at line 1, column 1.

`OxigraphConnector.SaveGraph` therefore writes Turtle from an explicit BOM-less encoder. Setting
`Encoding` on a `MimeTypeDefinition` does not work — the inherited `SaveGraph` never consults it.

**3. A catch-all `Accept` header loses ASK results.** dotNetRDF's combined RDF-or-SPARQL accept list
includes the plain-text result formats, and Oxigraph will negotiate to them: an ASK comes back as
the five bytes `false`, which is neither a SPARQL result document nor RDF, and fails to parse.
`GraphDBConnector` carries the same caveat in its own words. The Accept header is chosen by query
form instead.

**4. GraphDB could not parse TriG from a string or stream** — see ADR-0043's follow-up, closed here.
Each backend carried its own copy of the format switch and they had drifted; GraphDB's had no TriG
case, so the document fell to the RDF/XML arm. It is now `StoreBase.TryParse`, shared for the reason
`GroupByTargetGraph` is shared. The gap survived because every existing TriG test goes through
`Read(Uri, Uri, …)`, which routes by graph name and never reaches the switch.

## Consequences
- A backend that needs no server administration, which makes the store suite cheap to run locally.
- **`inferenceEnabled: true` throws on Oxigraph and is silently ignored on Fuseki.** Two backends,
  two answers to the same request, both defensible under ADR-0022 — which is the cost of having no
  capability model. A caller cannot ask; it must know its backend.
- Oxigraph canonicalizes the integer-derived XSD datatypes into `xsd:integer`, so `xsd:short` reads
  back as `Int32`. Mapped properties are unaffected — they declare a target type and Trinity converts
  into it (ADR-0040) — but the unmapped bag declares none. Five `ResourceTest` cases are
  `Assert.Inconclusive`, the same split as Virtuoso and for the same reason.
- Oxigraph refuses a blank node as a graph *name*, correctly: SPARQL's `GRAPH` takes an IRI. The
  other three accept it as an extension, which is why `ListModels` filters for well-formed IRIs at
  all. The filter stays; the scenario is simply unreachable on this backend.
- Writing Turtle rather than RDF/XML is a **workaround in our adapter for a dotNetRDF defect**. The
  defect is upstream and still affects every other backend; it is invisible there only because they
  are lenient.

## Review of the PR: the copies had drifted

Review of the PR found defects in the Graph Store read path that the adapter had copied from
`FusekiStore`. Each of them is in every copy, so each was fixed in every copy:

- **`Read(update: true)` replaced the graph** on Oxigraph, Fuseki and GraphDB. Each wrote through
  `SaveGraph`, which is a Graph Store `PUT` and so a replace; skipping the delete when updating made no
  difference. This was data loss on the two released backends. It went unnoticed because the shared
  test only checked the value it had just added. Oxigraph now `POST`s (`OxigraphConnector.AppendGraph`);
  Fuseki and GraphDB go through `UpdateGraph`, as `VirtuosoStore` always did.
- **`leaveOpen: true` closed the caller's stream** on all five stores, because disposing a plain
  `StreamReader` closes the stream beneath it.
- **Reading TriG from a file returned `null`** on the three Graph Store backends, which callers read as
  failure even though the data had been written.

**Graph names are exact IRIs.** `Uri.AbsoluteUri` lower-cases the host and re-escapes the path, so a
Graph Store write that names the graph by it addresses a different graph from the one the SPARQL path
(`OriginalString`) queries. `OxigraphConnector` addresses graphs itself and uses `OriginalString`.
dotNetRDF's own result parsers normalize IRIs the same way, which is why `ListGraphNames` reads
`STR(?g)`. Fuseki, GraphDB and Virtuoso still get this wrong inside dotNetRDF's connectors, so it is
quarantined there (`doc/known-test-failures.md`) until they get the same override.

**The query form travels with the query.** `OxigraphStore.ExecuteQuery` knows whether it is sending a
SELECT/ASK or a CONSTRUCT/DESCRIBE and passes that to the connector, which picks the Accept header and
the parser from it. Re-parsing every query to find out cost a second parse on the hot path. The
content type could not decide either: dotNetRDF maps `application/rdf+xml` to its results parser.

The broader suggestion, moving the Graph Store logic into one base class so the copies cannot drift,
is deferred to a separate change. This PR already touches every backend.

## Revival notes
- The LINQ path materializes results through reflection, so a store's exception reaches the caller
  wrapped in `TargetInvocationException`. Catching `NotSupportedException` around `AsQueryable`
  therefore does not work, for any backend that refuses anything. Unwrapping it is a change to shared
  code and was left out of this one.
- Report the RDF/XML entity-quoting and Turtle BOM defects upstream to dotNetRDF.

## Related
- [0022](0022-store-capabilities-and-istore-extension.md), [0036](0036-integration-tests-testcontainers.md),
  [0040](0040-numeric-conversion-on-read.md), [0041](0041-layered-read-views.md),
  [0042](0042-staged-writes-and-accept.md), [0043](0043-fuseki-store-revival.md),
  [0044](0044-store-suites-green-and-in-ci.md)
