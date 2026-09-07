# 0022. Store-defined capabilities and inferencing; extend via IStore; no capability negotiation

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
Backends differ widely — transactions, inferencing/reasoning, SPARQL feature support.
Trinity chose a single `IStore` contract and pushed those differences down into each
implementation, rather than modeling capabilities explicitly.

## Decision
- **Custom stores implement `IStore`** (in practice by extending `StoreBase`). That interface
  is *the* extension point for new backends. **Caveat:** several `IStore`/`StoreBase`
  docstrings are stale — they were not updated as the code evolved and must not be treated as
  the spec; the code is authoritative until they are fixed.
- **Inferencing is the store's responsibility.** The API surfaces only a per-query
  `inferenceEnabled` flag (`IModel.ExecuteQuery/GetResources/AsQueryable(..., bool
  inferenceEnabled = false)`, `Trinity/Model/IModel.cs`). Whether it does anything depends on
  the store: GraphDB uses `infer=true` and an implicit model group; Virtuoso selects a rule set;
  the in-memory store materializes RDFS entailments into a side graph
  ([0045](0045-in-memory-rdfs-inferencing.md)); Fuseki ignores the flag, having no per-query
  switch ([0043](0043-fuseki-store-revival.md)).

  **This sentence used to claim the in-memory store "applies an `RdfsReasoner`". It did not** —
  the wiring existed but the feature was unfinished and the flag was never read, so the store
  silently answered as though inference were off. That claim is why the gap was repeatedly
  mistaken for working support; 0045 makes it true.
- There is **no capability model.** Trinity does not list, expose, or negotiate what a given
  store supports. Callers must know their backend.

## Consequences
- Minimal, uniform contract; adding a backend is straightforward.
- Requesting a capability a store lacks (inferencing, transactions) fails or silently no-ops
  with **no discoverable signal** — e.g. Fuseki/GraphDB `BeginTransaction` returned `null`
  ([0009](0009-supported-store-backends.md)).
- No way to write portable code that adapts to store features, and no place to assert them in tests.

## Revival notes
Update the `IStore`/`StoreBase` docstrings to match actual behavior. Consider a lightweight
capability descriptor (supports-inferencing / transactions / update / full-text …) so callers
and tests can adapt instead of guessing.

## Related
- [0008](0008-store-model-abstraction.md), [0009](0009-supported-store-backends.md)
