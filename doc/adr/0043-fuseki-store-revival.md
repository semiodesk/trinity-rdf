# 0043. Reviving the Fuseki store: the 4/86 was a missing dataset, not a connector bug

Date: 2026-08-21

## Status
Accepted (2.0).

## Context

`Trinity.Fuseki` was recorded as **4/86 passing, blocked upstream**, in four places:
[0036](0036-integration-tests-testcontainers.md) ("dotNetRDF's `FusekiConnector` POSTs to `/ds/query`
where this server rejects POST (GET works), and graph deletes 405"), [0009](0009-supported-store-backends.md),
[0041](0041-layered-read-views.md), `README.md` ("affected by an upstream connector issue … should be
considered experimental") and `CLAUDE.md`. `FusekiStore` even carried a `TODO` agreeing with it: *"the
URL must end with /sparql instead of /query"*.

Because a backend blocked upstream is not worth writing tests against, Fuseki was excluded from the
ADR-0039, 0041 and 0042 test work. It became the only backend with no coverage for write semantics,
numeric round-trips, layered read views or the resource suite — and the only one still running a
hand-written, non-generic copy of the store suite. The exclusion was self-reinforcing: the more
coverage the other backends gained, the less anyone looked at this one.

**The diagnosis was wrong.** Probing a live container directly:

```
$ curl -u admin:test http://localhost:PORT/\$/datasets
{ "datasets" : [ ] }
```

`FusekiContainer` created the dataset with `FUSEKI_DATASET_1=ds`. That variable belongs to
`secoresearch/fuseki`; `stain/jena-fuseki` ignores it, without complaint. **No dataset was ever
created**, so every path under `/ds/*` returned 404 — GET and POST alike, query, update and Graph
Store Protocol. The connector was never at fault, and neither was the query URL: `/<dataset>/query`
is exactly what Fuseki serves and what the connector derives from the `/<dataset>/data` URL it is
given.

Creating the dataset over the admin protocol makes all of it work, the 405 on graph delete included:

| request | before | after |
|---|---|---|
| `POST /ds/query` | 404 | 200 |
| `GET /ds/query` | 404 | 200 |
| `POST /ds/update` | 404 | 200 |
| `PUT /ds/data?graph=…` | 404 | 201 |
| `DELETE /ds/data?graph=…` | 405 | 204 |

**Why it survived.** The readiness probe was `GET /$/ping`, which answers 200 on a Fuseki server with
no datasets at all. It confirmed that a container was running, which was never in doubt, and could not
observe the one thing that had gone wrong. The failure then presented as 82 tests failing with HTTP
404 deep in the run — a shape that looks like a broken client, which is what it was read as.

## Decision

Revive the backend properly, in four steps.

1. **Provision the dataset, and prove it answers.** `FusekiContainer` POSTs
   `dbType=mem&dbName=ds` to `/$/datasets`, then issues a trivial `ASK` against `/ds/query` before
   any fixture runs. A readiness probe must exercise the thing under test; anything less cannot fail
   when the thing under test is missing. Breaking the provisioning on purpose now fails
   `OneTimeSetUp` with the cause named, which was verified.

2. **Put Fuseki on the shared generic fixtures.** A `FusekiTestSetup : IStoreTestSetup` (three
   members) plus twelve one-liner fixtures, matching GraphDB file for file. This retires ~3,500 lines
   of hand-written copy. Nothing was lost: the two tests unique to the copy are subsumed by the
   shared bases, the two that were unconditionally `Assert.Inconclusive` asserted nothing, and the
   `;rule=urn:semiodesk/test/ruleset` suffix four fixtures appended to the connection string was a
   no-op the provider never parsed.

3. **Fix the adapter's own defects** (below), now that a suite can see them.

4. **Correct the record** in 0009, 0036, 0041, 0022, 0028, `README.md` and `CLAUDE.md`.

### Fuseki's capabilities, stated plainly

- **No per-query inferencing.** A Jena reasoner is a property of the *dataset*, so it applies to every
  query or to none; there is no `infer=true` equivalent to GraphDB's. `FusekiStore.ExecuteQuery`
  therefore ignores `IsInferenceEnabled`, which [0022](0022-store-capabilities-and-istore-extension.md)
  permits, and this is now documented on the method instead of being silent. The four shared
  inferencing tests are `Assert.Inconclusive`, following the `Trinity.Tests/Store/DotNetRDF/`
  precedent. Giving the dataset a reasoner would make them pass but would break the honest cases:
  inference could no longer be turned *off*, so `inferenceEnabled: false` would quietly lie.
- **No transactions.** `BeginTransaction` returns a `NoOpTransaction` — never null
  ([0039](0039-resource-write-semantics.md)).
- **Layered read views work with no store-specific code**, as designed:
  `CreateLayeredModel` is a store-agnostic extension whose only gate is
  `!(store is SparqlEndpointStore)`. Note that `Accept()`'s atomicity rests on single-request
  atomicity, which [0042](0042-staged-writes-and-accept.md) measured on in-memory, GraphDB and
  Virtuoso but **not** on Jena.
- **Requires Fuseki 5.1.0 or newer.** Jena 4.0.0 answers HTTP 500 *"Not a valid UUID string:
  urn:uuid:…"* to any query mentioning a `urn:uuid:` or `uuid:` IRI — verified against a live server,
  and not specific to `VALUES`: a plain triple pattern and a `FILTER` equality fail identically,
  while an `INSERT` of the same IRI succeeds. Jena hands the whole IRI to a UUID parser instead of
  the UUID part, so data written under such an identifier is permanently unreadable. That is not an
  edge case here: `Model.CreateResource()` mints `urn:uuid:` identifiers by default. Fixed upstream;
  the test image is pinned to `stain/jena-fuseki:5.1.0`.

### The defect covering Fuseki found in the core

The overlay's dataset clause was emitted **twice**. Every layered read builds its query text with the
three `FROM NAMED` graphs already in it and then sets `query.Model`, whose setter adds those same
graphs through the preprocessor. The preprocessor skips a graph it has already recorded — but it never
recorded a `FROM NAMED` one, because the tokeniser splits `FROM NAMED <g>` into three tokens
(FROM, NAMED, URI) and the check looked only for `Token.FROM` (115) and `Token.FROMNAMED` (117).
`FROMNAMED` never appears in that position. Plain `FROM` matched and worked correctly, which is why
only the layered path — the sole emitter of `FROM NAMED` — was affected.

Virtuoso and GraphDB tolerate the repeated clause, so their layered suites passed throughout
ADR-0041 and 0042 and the bug stayed invisible. Jena rejects it with *"URI already in named graph
set"*, HTTP 400. **105 of Fuseki's 106 initial failures were this one defect.**

Two regression tests, both verified to fail when the defect is reintroduced. The pre-existing
`DatasetClauseNamesAllThreeGraphsAndNeverMergesThem` asserted the clause in isolation and passed the
whole time; the new one asserts the query *after* the model is assigned. That gap — unit-testing a
fragment rather than the path — is the reusable lesson.

### Adapter defects fixed

Wrong answers:
- `ContainsModel(Uri)` called `HasGraph`, **discarded the result**, and returned `false`
  unconditionally. It had no test on any backend but the in-memory one, which is how it survived.
- `CreateModelGroup(params IModel[])` never populated its list, so it always returned an **empty
  group** (the bug [0009](0009-supported-store-backends.md) and
  [0019](0019-models-are-named-graphs-modelgroups.md) recorded). It was declared `new` rather than
  `override`, which is why it never bit: a caller holding an `IStore` dispatched to `StoreBase`'s
  correct implementation. Deleting the override is the entire fix.
- `IsReady` was a hardcoded `true` and stayed `true` after `Dispose`.
- `TryParse` had no `Trig` case, so TriG read through `Read(string)`/`Read(Stream)` was parsed as
  **RDF/XML** — a silent failure. `Read(Uri, Uri, …)` special-cased TriG, so the class contradicted
  itself.
- `Read(Uri, Uri, …)` accepted only scheme `http`, returning `null` with no diagnostic for `https`.
- A password-only configuration silently dropped the credentials.
- `FusekiStoreProvider` defaulted `dataset` to the literal `"dataset"` and never validated it, so a
  caller who omitted `dataset=` got a store that connected, reported ready, and 404d on everything —
  **the same failure mode as the container bug above.** Now defaults to `ds`, and an empty dataset is
  refused at construction rather than at first query.

`StoreBase.ContainsModel(IModel)` gained the null guard GraphDB had already written into its own
override — deleting Fuseki's override otherwise inherited a `NullReferenceException`. Caught by a new
test rather than by reading.

Dead code removed: `FusekiSparqlQueryResult` (internal, no overrides, zero references),
`Properties/AssemblyInfo.cs` (no license header, and an `[assembly: Guid]` byte-identical to
`Trinity.Virtuoso`'s — two assemblies claiming one typelib GUID), the `_parser` field and the
commented-out line that was its only consumer, and four overrides that only repeated `StoreBase`.
`ListModels` moved off the obsolete `ListGraphs()` to `ListGraphNames()`
([0038](0038-upgrade-dotnetrdf-3.md)).

**`UpdateResource` is deliberately left as it is.** It differs from `StoreBase` only in emitting
`INSERT DATA { GRAPH <g> { … } }` for a new resource where the base allocates a blank-node id via
`SELECT BNODE()` and emits `WITH … INSERT … WHERE {}`. The three blank-id tests the generic suite
newly runs against Fuseki pass as it stands, so there is no measured reason to change the SPARQL it
emits.

## Consequences

- **Fuseki: 4/86 → 248 passed, 0 failed, 1 skipped** (the `CanRemoveBlankNodeValuedLink` quarantine
  every backend skips). It is the *best*-passing backend, ahead of GraphDB (241/246) and Virtuoso
  (234/239), and it is covered by the layered, staging, differential, query-corpus, write-semantics,
  numeric-round-trip and resource suites for the first time.
- GraphDB (241/246) and Virtuoso (234/239) are byte-identical to their pre-change failure sets, which
  matters because this change edits `StoreBase` and deletes per-store overrides in favour of it.
- The in-memory suite gains the two regression tests: 604 passed, 3 failed, 7 skipped. The three
  failures are the pre-existing `UriRef` cases that fail only on a rolled-forward .NET 10 runtime
  (`doc/known-test-failures.md`).
- **A Fuseki 5.x server is now required.** This is a real constraint on consumers, not just on tests.
- The store-integration suites stay out of the default CI job, unchanged from
  [0036](0036-integration-tests-testcontainers.md). Nothing stops Fuseki regressing to 4/86 silently
  except the readiness probe added here — which is precisely why it was added.

### Follow-ups, deliberately not done here

These are repo-wide, present identically in `GraphDBStore` and `dotNetRDFStore`, and do not belong in
a Fuseki commit:

- **`Read(Stream, …, bool leaveOpen)` ignores `leaveOpen`**: `using (TextReader reader = new
  StreamReader(stream))` disposes the stream regardless, making the `if (!leaveOpen)` that follows
  dead. Three copies.
- **An unreachable branch** in `Read(Uri, Uri, …)`: the method dereferences `url.AbsoluteUri`, which
  throws for a relative `Uri`, before testing `url.IsAbsoluteUri`.
- **The TriG loop** in the same method deletes the *target* graph once per iteration while saving each
  parsed graph under its own name, so `graphUri` is ignored for TriG.
- **`Write(…)` silently writes nothing** when the graph does not exist — a caller cannot tell that
  from an empty graph.
- **`TryParse` is a third verbatim copy** of the same format switch; the missing TriG case was fixed
  in one copy only.
- `ContainsModelTest` should be promoted from a per-store fixture to a shared one. Virtuoso and
  GraphDB implement `ContainsModel` correctly today, but nothing shared holds them to it.

## Related
- [0036](0036-integration-tests-testcontainers.md) — the containerized suites, and the corrected 4/86
  claim
- [0009](0009-supported-store-backends.md) — the backend inventory and the `CreateModelGroup` bug
- [0041](0041-layered-read-views.md), [0042](0042-staged-writes-and-accept.md) — the layered work
  Fuseki was excluded from, and the defect that exclusion hid
- [0022](0022-store-capabilities-and-istore-extension.md) — inferencing as a capability a store may
  ignore
- [0028](0028-store-level-transactions.md), [0039](0039-resource-write-semantics.md) — transactions
  and the `NoOpTransaction`
- [0038](0038-upgrade-dotnetrdf-3.md) — the `ListGraphs` → `ListGraphNames` rename
