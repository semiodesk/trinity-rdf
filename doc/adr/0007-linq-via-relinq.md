# 0007. LINQ-to-SPARQL via Remotion.Linq (re-linq)

Date: 2026-07-13 (decision original to 2015–2020 design)

## Status
Superseded by [0037](0037-linq-provider-rebuild.md) — the re-linq provider was retired in 2.0 and
replaced by an owned SPARQL LINQ provider. Kept for historical context.

## Context
A stated selling point is querying the graph with LINQ instead of SPARQL. Translating
LINQ expression trees to SPARQL is non-trivial; a parsing/visitor framework was needed.

## Decision
The LINQ provider is built on **Remotion.Linq (re-linq) 2.2.0**. `SparqlQueryable<T>`
extends `QueryableBase<T>`, `SparqlQueryExecutor` implements re-linq's `IQueryExecutor`,
and `Model.AsQueryable<T>` builds the pipeline via `QueryParser.CreateDefault()`. A set
of query-model visitors and generators under `Trinity/Query/Linq/` (~24 files) consume
re-linq clause and result-operator types.

## Consequences
- Rich LINQ surface (Where/Take/Skip/Count/joins/grouping) mapped to SPARQL.
- re-linq is **effectively abandoned** upstream. It still runs on netstandard, so it is
  not a build blocker, but it is a dead dependency deeply woven through the query layer.
- **Neither current external consumer uses Trinity LINQ or SPARQL at all** — both
  materialize with `GetResources<T>()` and filter in memory. So this subsystem is not on
  the critical path for the consumers driving the revival.

## Revival notes
Deprioritize. Keep as-is while stabilizing. If it must change later, the options are to
keep re-linq indefinitely (isolated behind the provider) or rewrite the provider onto a
maintained expression-visitor approach — a large effort to schedule only if a consumer
needs LINQ.

## Related
- [0006](0006-build-on-dotnetrdf.md), [0008](0008-store-model-abstraction.md)
