# 0032. Data virtualization via lazy, paged collections

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Superseded by [0035](0035-remove-inotifypropertychanged.md) — the entire virtualizing-collection
feature (async **and** synchronous) was removed as unused, unwired dead code. SPARQL result paging
via `offset`/`limit` on `ISparqlQueryResult` ([0031](0031-multimodal-query-results.md)) is unaffected.

## Context
Knowledge graphs get large. Materializing an entire result set — especially to bind a UI list
— is expensive. The README calls out "data virtualization" as a feature: results should load
on demand in pages.

## Decision
`Trinity/Collections/` provides a virtualizing collection stack:
- `IItemsProvider<T>` + `VirtualizingCollection<T>` — a generic on-demand, paged list;
- `AsyncVirtualizingCollection<T>` — asynchronous page loading (for responsive UI binding);
- `VirtualizingSparqlCollection<T>` backed by `SparqlQueryItemsProvider` — pages fetched via
  SPARQL using offset/limit (see [0031](0031-multimodal-query-results.md)).

LINQ `Take`/`Skip` map to SPARQL paging ([0007](0007-linq-via-relinq.md)).

## Consequences
- Large result sets can be browsed without loading everything; the async variant keeps UIs
  responsive (originally a WPF-style virtualization pattern).
- The stack is UI-oriented and adds moving parts; correctness depends on **stable ordering**
  across page fetches.

## Revival notes
Confirm the async/virtualization stack still fits modern UI stacks (and is still needed);
guarantee deterministic ordering for paged queries.

## Related
- [0031](0031-multimodal-query-results.md), [0007](0007-linq-via-relinq.md)
