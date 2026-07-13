# 0035. Remove INotifyPropertyChanged support

Date: 2026-07-13

## Status
Accepted — supersedes the NPC aspects of [0003](0003-mapping-via-il-weaving.md),
[0013](0013-replace-il-weaving-with-source-generator.md), and [0032](0032-data-virtualization-paged-collections.md)

## Context
Trinity offered opt-in property-change notification for resources: a `[NotifyPropertyChanged]`
attribute woven by cilg into setters that raise `PropertyChanged`, `Resource` implementing
`INotifyPropertyChanged` (via `IResource`), and `RaisePropertyChanged`/`RegisterPropertyChanged`
runtime hooks. The `AsyncVirtualizingCollection`/`AsyncVirtualizingSparqlCollection` likewise
implemented `INotifyPropertyChanged`/`INotifyCollectionChanged`. All of it was convenience for
building WPF/MVVM UIs.

Evidence gathered during the revival:
- Neither external consumer (elxgen, DevHub) references any NPC member (0 hits); the test suite
  had 0 references — the feature was **unused and untested**.
- Current/planned targets (backend/MCP/agents; Blazor WASM) do not bind resources via
  `INotifyPropertyChanged`.
- NPC was 1 of the 3 IL-weaving tasks and the most complex (a setter that both calls `SetValue`
  and raises an event), complicating the weaver and the planned source generator (0013).
- The virtualizing collections (async and sync) were not wired into any query path or consumer.

## Decision
Remove resource-level `INotifyPropertyChanged` support and the WPF-oriented async collections:
- Delete the `[NotifyPropertyChanged]` attribute and the `ImplementNotifyPropertyChanged` weaver
  task (+ its `ILGenerator` wiring). The weaver now runs only `ImplementRdfClass` +
  `ImplementRdfProperty`.
- Remove `Resource.PropertyChanged`, `RaisePropertyChanged`, `RegisterPropertyChanged`, the
  `_notifyingProperties` field and the rollback re-raise; drop `INotifyPropertyChanged` from
  `IResource`.
- Delete `AsyncVirtualizingCollection` and `AsyncVirtualizingSparqlCollection`. The synchronous
  `VirtualizingCollection`/`VirtualizingSparqlCollection` paging primitives are kept.

## Consequences
- Smaller public surface; the weaver is simpler and the source generator (0013) implements only
  two member kinds (no NPC setter).
- **Breaking public API change**: `IResource` no longer implements `INotifyPropertyChanged` and
  `Resource` no longer exposes `PropertyChanged`. Practical impact is nil (no consumer used it),
  but it warrants a version bump at republish.
- Verified: full solution builds SDK-only and the Windows test gate stays green (260 passed,
  16 skipped) after removal.

## Related
- [0003](0003-mapping-via-il-weaving.md), [0013](0013-replace-il-weaving-with-source-generator.md),
  [0032](0032-data-virtualization-paged-collections.md)
