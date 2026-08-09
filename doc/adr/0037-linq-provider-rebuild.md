# 0037. Rebuild LINQ-to-SPARQL on an owned provider (retire re-linq)

Date: 2026-07-15

## Status
Accepted (2.0) — supersedes [0007](0007-linq-via-relinq.md).

## Context
The LINQ-to-SPARQL feature was built on **Remotion.Linq (re-linq)**, abandoned upstream and shipped
as a *public transitive dependency* of `Semiodesk.Trinity`. Worse, the translation half was written
against **dotNetRDF's Query Builder** API — the single most reorganized surface in the pending
dotNetRDF 2.7 → 3.x upgrade ([0006](0006-build-on-dotnetrdf.md)). Neither external consumer uses
Trinity LINQ. So before the 3.x upgrade we had to decide the LINQ layer's fate.

We chose to **keep LINQ but rebuild the provider from scratch**, not vendor re-linq. With
AI-assisted implementation the build-vs-vendor effort is comparable, so the deciders were
maintenance surface and future headroom: a focused provider is ~40–55 classes to own versus
re-linq's ~180–200 general-purpose files, and only an owned query AST leaves room for the operators
we don't yet support and for SPARQL\* (quoted triples). EF Core is the precedent — it used re-linq
through 2.2 and replaced it with its own visitor pipeline + query AST in 3.0.

## Decision
A self-contained provider under `Trinity/Query/Sparql/`, independent of dotNetRDF's Query Builder:

- **Owned SPARQL AST** (`SparqlAst.cs`, `SparqlExpressions.cs`) + serializer (`SparqlQueryWriter.cs`)
  that emits full-IRI SPARQL text.
- **EF-Core-shaped pipeline**: `PartialEvaluator` (funcletize closures/locals), `SparqlQueryTranslator`
  (fold each LINQ operator into the AST — no intermediate query model), `SparqlQueryProvider` /
  `TrinityQueryable<T>`. Execution reuses the existing path: build a `SparqlQuery` string and call
  `Model.GetResources<T>` (resources) or `Model.ExecuteQuery(...).GetBindings()/GetAnwser()` (values,
  counts, ASK). Execution kinds: `ResourceList` / `Ask` / `Count` / `Bindings`.
- **Semantics matched to the old provider**: unbound mapped members behave as `default(T)` via
  `OPTIONAL { … } FILTER(?v = c || !BOUND(?v))` (+ `COALESCE` in value projections); interface-declared
  members (which carry no `[RdfProperty]`) resolve to the concrete type's member; collection `Count`
  uses a correlated `COUNT … GROUP BY`; `Last` inverts the ordering.
- `IModel.AsQueryable<T>()` now routes to this provider. `Trinity/Query/Linq/` (re-linq) and the
  `Remotion.Linq` package are **removed**.

## Consequences
- **The 3.x upgrade will not touch the LINQ layer** — it emits SPARQL strings, not Query-Builder
  calls. This was the main reason to do the rebuild first.
- `Remotion.Linq` is no longer a transitive dependency of the shipped package.
- **Parity**: the 51 `LinqTestBase` bodies now run on the new provider (via `IModel.AsQueryable`) and
  pass exactly as on re-linq — 88/88 active across the two model fixtures; the 14 `[Ignore]`d cases are
  unchanged. Serializer + exemplar suites cover the AST/pipeline directly; full in-memory suite
  274 passed / 14 skipped. The operator breadth was implemented with the `claude-fable-5` model
  against that parity corpus; the vertical-slice exemplar + AST design were built first as the pattern.
- Unsupported/awkward operators throw `NotSupportedException` rather than emit wrong SPARQL (bounded
  scope), consistent with the SPARQL semantic-mismatch areas flagged in the plan.

## Unbound-member and collation semantics (decided)
A `LinqCoverageTest` oracle corpus (~96 cases; each query runs through the provider *and* through
LINQ-to-Objects over the same data) drove two semantic questions to a decision:

- **A mapped member with no triple behaves as `default(T)`** — consistently, not just in projections.
  Equality already did this (`OPTIONAL … FILTER(?v = c || !BOUND(?v))`) and so did value projections
  (`COALESCE(?v, default)`); **inequalities and `ORDER BY` now do too**, comparing/sorting on
  `COALESCE(?v, default)` with an optional binding. Before this, `Where(p => p.Age < 45)` silently
  excluded resources without `foaf:age`, and worse, `OrderBy(p => p.Balance)` *dropped* them from the
  result — inconsistent with `Select(p => p.Balance)`, which already yielded `0` for them.
  Strings keep their existing no-default treatment (parity with the old provider).
- **String ordering follows SPARQL, not .NET.** `ORDER BY` on plain literals is Unicode-codepoint
  ordered (so `"Zoe"` precedes `"alice"`), whereas .NET's default comparer is culture-sensitive.
  Matching .NET would mean sorting client-side and giving up `LIMIT`/`OFFSET` pushdown, so the SPARQL
  behaviour stands and the corpus asserts it with `StringComparer.Ordinal` on the oracle side.

With both settled the corpus is green (96/96) and part of the default suite, so it gates CI — and it
is the regression net for the dotNetRDF 3.x upgrade.

## Follow-ups
- The 4 quarantined LINQ-provider correctness gaps (from 0007, in `doc/known-test-failures.md`) are
  still `[Ignore]`d and have not been re-verified against the new provider — revisit.
- **Open decision — polymorphic base-type queries.** `GetTypes()` emits only a class's own
  `[RdfClass]`, so a `Person` is not typed `foaf:Agent` and `AsQueryable<Agent>()` returns only
  resources explicitly typed `foaf:Agent`. LINQ semantics say a `Person` *is* an `Agent`, so a
  base-type query arguably should expand to a UNION over the registered subclasses
  (`MappingDiscovery` knows them) — or be left to store-side `rdfs:subClassOf` inference. This is
  the last blocker on `CanSelectResourcesWithOperatorTypeOf`, and it would change the behaviour a
  current test (`SparqlLinqExemplarTest.FiltersToExactType`) documents.
- Still deliberately unsupported (throws rather than emitting wrong SPARQL): `GroupBy` that
  materializes the group's *elements*, and `Take().Skip()` (which needs a nested sub-SELECT).
- Consider a SPARQL\* (quoted-triple) surface now that an owned AST makes it tractable.

## Related
- [0007](0007-linq-via-relinq.md) (superseded), [0006](0006-build-on-dotnetrdf.md) (dotNetRDF; the
  3.x upgrade is the next milestone), [0013](0013-replace-il-weaving-with-source-generator.md).
