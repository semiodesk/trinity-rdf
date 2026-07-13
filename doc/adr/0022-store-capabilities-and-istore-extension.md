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
  the store: dotNetRDF in-memory applies an `RdfsReasoner`; GraphDB uses `infer=true` and an
  implicit model group; other stores may ignore the flag.
- There is **no capability model.** Trinity does not list, expose, or negotiate what a given
  store supports. Callers must know their backend.

## Consequences
- Minimal, uniform contract; adding a backend is straightforward.
- Requesting a capability a store lacks (inferencing, transactions) fails or silently no-ops
  with **no discoverable signal** — e.g. Fuseki/GraphDB `BeginTransaction` return `null`
  ([0009](0009-supported-store-backends.md)).
- No way to write portable code that adapts to store features, and no place to assert them in tests.

## Revival notes
Update the `IStore`/`StoreBase` docstrings to match actual behavior. Consider a lightweight
capability descriptor (supports-inferencing / transactions / update / full-text …) so callers
and tests can adapt instead of guessing.

## Related
- [0008](0008-store-model-abstraction.md), [0009](0009-supported-store-backends.md)
