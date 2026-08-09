# 0031. Multi-modal SPARQL query results (resources, bindings, ASK, count)

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
SPARQL has several query forms (SELECT, ASK, CONSTRUCT, DESCRIBE) returning different shapes.
A single result abstraction should serve all of them, supporting both object materialization
and raw variable bindings, with paging for large results.

## Decision
`ISparqlQueryResult` (`Trinity/Query/ISparqlQueryResult.cs`) exposes:
- `GetResources()` / `GetResources<T>()` (with optional `offset`/`limit`) — materialize mapped
  resource objects from DESCRIBE/CONSTRUCT/interpretable SELECT forms;
- `GetBindings()` → `IEnumerable<BindingSet>`, where a `BindingSet` is a
  `Dictionary<string, object>` = one solution row — raw SELECT variable bindings;
- `GetAnwser()` → `bool` for ASK forms;
- `Count()`.

## Consequences
- One result type covers both object-mapped reads and low-level tabular access — valuable when
  a query does not map cleanly onto resources ([0016](0016-resource-centric-not-triple-centric.md)).
- Paging on `GetResources` (offset/limit) underpins data virtualization
  ([0032](0032-data-virtualization-paged-collections.md)).
- The dual "resources vs bindings" surface means callers must pick the accessor matching their
  query form; using the wrong one yields empty results. (Note the historical typo `GetAnwser`.)

## Related
- [0032](0032-data-virtualization-paged-collections.md), [0016](0016-resource-centric-not-triple-centric.md),
  [0024](0024-sparql-reuses-registered-prefixes.md)
