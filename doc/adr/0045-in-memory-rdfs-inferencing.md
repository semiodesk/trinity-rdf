# 0045. Finish in-memory inferencing: entail into a side graph, union it in per query

Date: 2026-08-24

## Status
Accepted (2.0).

## Context

`inferenceEnabled: true` on the in-memory store **silently returned non-inferred results**. Not an
exception, not a warning — the same answer the flag-off query would give. That is precisely the failure
mode [0041](0041-layered-read-views.md) and [0042](0042-staged-writes-and-accept.md) went to some
lengths to forbid elsewhere, and the in-memory store is the one most people meet first.

The feature was started and abandoned, which is why it read as supported: `dotNetRDFStore` held an
`RdfsReasoner`, built an `InferencingTripleStore`, and called `AddInferenceEngine`. None of it did
anything. [0022](0022-store-capabilities-and-istore-extension.md) stated as fact that "dotNetRDF
in-memory applies an `RdfsReasoner`", and that sentence is why the gap was repeatedly mistaken for
working support — including by the author of this ADR, mid-review, against a repository whose own
`doc/known-test-failures.md` said plainly that the store *does not implement inferencing*.

**Half-wired parts are not support.** Three gaps, all real:

1. **The flag was never read.** `ExecuteQuery(ISparqlQuery, ITransaction)` went straight to
   `query.ToString()` → parse → `ProcessQuery`. `IsInferenceEnabled` was never consulted.
2. **No reasoner existed** for the way callers actually build the store. One was created only when the
   connection string carried `schema=`; every test, and the documented [0011](0011-configuration-model.md)
   flow, seeds vocabularies with `store.Read` / `LoadGraphs` into named graphs instead.
3. **The write path could not have fed it.** dotNetRDF materializes on `ITripleStore.Add`; Trinity
   writes through `LeviathanUpdateProcessor`, which bypasses the inference engine entirely.

## Decision

**Materialize RDFS entailments into a separate graph per model, and add that graph to a query's dataset
only when the flag is set.**

```
inferenceEnabled: false  ->  FROM <g>                    (byte-identical to previous behaviour)
inferenceEnabled: true   ->  FROM <g> FROM <inferred(g)>
```

`RdfsEntailment` (`Trinity/Stores/dotNetRDF/RdfsEntailment.cs`) computes them **per query, into a
throwaway dataset** that merely references the store's existing graphs, and discards it when the query
returns. Nothing is ever added to the store.

**Why not `AddInferenceEngine`, the obvious answer.** It writes entailments back into the graph they
came from, so they become visible to *every* query — and a store that does that cannot answer
`inferenceEnabled: false` correctly. It is the same all-or-nothing property that stops Fuseki honouring
the flag at all ([0043](0043-fuseki-store-revival.md)). The side graph is the whole reason the flag can
be per-query. `IInferenceEngine.Apply(input, output)` — *"outputs the inferred information to the Output
Graph"* — is the API that makes it possible.

**Why not rewrite queries to walk `rdfs:subClassOf*`.** That would honour the flag too, and
`OverlayQueryRewriter` is a proven in-repo precedent for parse-tree rewriting. But it covers only the
entailments someone hand-writes, and the three that matter — class hierarchy, property hierarchy,
domain/range — are already implemented and maintained inside dotNetRDF. Reimplementing RDFS in SPARQL is
the larger and less correct job.

**`StaticRdfsReasoner`, initialised from the store.** The schema is taken from the graphs actually
present rather than only from `schema=` files, so vocabularies seeded the documented way are seen.
`Static` rather than `RdfsReasoner` so the schema is fixed when the reasoner is built and applying data
cannot silently redefine the ontology.

**No public API changed.** The dataset is widened on the *parsed* query via
`SparqlQuery.AddDefaultGraph(IRefNode)`, so nothing is re-serialized and no Trinity type grew a way to
add a `FROM`. A query naming no graph is left alone — adding one would narrow it from the whole store to
a single graph, the opposite of what enabling inference should do.

The graphs to widen come from the parsed query's **resolved** `DefaultGraphNames`, not from Trinity's own
record of the `FROM` operands. That record is raw token text, which is *relative* when the query carries
a `BASE` declaration — so reading it turned a query that worked with the flag off into a
`UriFormatException` the moment inference was switched on.

### Queries that address a graph by name are refused

Entailments are added to the query's **default** graph. A pattern inside `GRAPH <g>` reads `g` itself,
which holds only asserted triples, so such a query would come back non-inferred while reporting success.
`inferenceEnabled: true` with `GRAPH` or `FROM NAMED` therefore throws `NotSupportedException` — the same
choice a layered view makes for a query it cannot rewrite faithfully ([0041](0041-layered-read-views.md)).
Silently answering it would be the exact defect this ADR exists to remove, wearing the costume of a
feature that works.

### Entailments are recomputed per query, not cached

The first implementation cached entailment graphs *inside* the store and invalidated coarsely on every
write. Review found that this bought speed at the cost of three defects, all of them the silent kind:

- the cached graphs were visible to any query enumerating `GRAPH ?g`, so internal bookkeeping and its
  entailed triples leaked into queries that had switched inference **off** — `ListModels` and
  `ContainsModel` filtered them, raw SPARQL did not;
- a failure midway through materialization **latched permanently**, because the model was marked
  materialized before the work succeeded, so every later inferred query for it silently ran uninferred;
- a *read* mutated shared state, so a concurrent invalidation could remove graphs from under an
  in-flight query.

Computing per query removes all three by construction, and removes the invalidation problem with them.
The cost is recomputation, which for an in-memory store used in tests and development is the cheaper
trade. If it ever shows up in a profile, the fix is a cache keyed on a store version — but it has to be
a cache that cannot be observed, which is what the first attempt got wrong.

### Polymorphism is gated behind the flag

This closes the open decision in [0037](0037-linq-provider-rebuild.md). `AsQueryable<Agent>()` returns
exactly-typed resources; `AsQueryable<Agent>(inferenceEnabled: true)` also returns subclasses, because
the entailed `rdf:type` triples are in scope. `SparqlLinqExemplarTest.FiltersToExactType`, which
documents the exact-type behaviour, passes **unchanged** — the proof that flag-off semantics did not
move. `CanSelectResourcesWithOperatorTypeOf` stays quarantined: it wants subclasses with the flag *off*,
which directly contradicts `FiltersToExactType`. That is a semantics choice for the project, not
something this change should decide by side effect.

## Consequences

- **In-memory**, as `passed / failed / skipped = total` so a drifted row is visible without re-running
  anything (the convention [0044](0044-store-suites-green-and-in-ci.md) adopted after its own counts
  went stale twice):

  | runtime | before | after |
  |---|---|---|
  | .NET 10 | 608 / 3 / 7 = 618 | **623 / 3 / 3 = 629** |
  | .NET 9 | 611 / 0 / 7 = 618 | **626 / 0 / 3 = 629** |

  The row depends on the runtime, not on this change: the assembly targets net8.0, and rolled forward
  onto .NET 10 the three `UriRef` equality tests fail while onto .NET 9 they pass. CI installs the
  net8.0 runtime and sees neither (`doc/known-test-failures.md`).

  The four skipped that remain are `CanSelectResourcesWithOperatorTypeOf` (twice, under the model and
  model-group fixtures), `CanRemoveBlankNodeValuedLink`, and the two live-endpoint DBPedia tests.

- **The store suites are untouched**, which is the point of the side graph: Fuseki 253 / 0 / 1,
  GraphDB 251 / 0 / 1, Virtuoso 241 / 0 / 1, all identical to [0044](0044-store-suites-green-and-in-ci.md).
- **Seven tests come out of quarantine or inconclusive**: the four shared inferencing tests
  (`TestInferencing`, `GetTypedResourcesWithInferencingTest`, `MappingTypeWithInferencingTest`,
  `MappingTypeCollectionWithInferencingTest`) — their `Assert.Inconclusive` overrides are deleted, not
  reworded — plus `CanExecuteCollectionWithInferencingEnabled` and
  `CanExecuteScalarWithInferencingEnabled`, which pass under the model *and* model-group fixtures.
- **`inferenceEnabled` now means the same thing on three of four backends.** Only Fuseki ignores it, for
  a stated architectural reason.
- `DotNetRDFInferenceTest` covers what the shared tests cannot: that entailments stay *out* of
  non-inferred queries, that they are recomputed after a write and withdrawn after a delete, that the
  side graphs are not models, and that a query naming no graph is unaffected. The two staleness tests
  were verified to fail when invalidation is removed.
- **`schema=` behaves differently.** It still loads those files, but no longer materializes entailments
  into the store on load. That was in-place materialization — the behaviour this ADR rejects — and it
  only ever fired on `Add`, so it never saw anything Trinity wrote. Nothing in the repository relied on
  it beyond `DotNetRDFConfigTest`, which asserts construction succeeds.

### Limits worth stating

- **RDFS only.** No OWL. `SkosReasoner` and `OwlReasonerWrapper` exist and could be offered later.
- **Whole-graph materialization** on first inferred query. Fine for an in-memory store; not a strategy
  that would survive being generalised to a large backend.
- **Entailments are computed per model graph.** A query spanning a model group materializes each member
  separately, so an entailment that would only follow from two graphs *together* is not derived.
- **`GRAPH` and `FROM NAMED` are refused**, not supported, when the flag is set — see above.
- **Recomputed on every inferred query.** No caching, deliberately.

## Related
- [0022](0022-store-capabilities-and-istore-extension.md) — the per-query flag, and the claim this corrects
- [0037](0037-linq-provider-rebuild.md) — the polymorphic base-type decision this closes
- [0043](0043-fuseki-store-revival.md) — why a dataset-level reasoner cannot honour a per-query flag
- [0044](0044-store-suites-green-and-in-ci.md) — Virtuoso's and GraphDB's inference provisioning
