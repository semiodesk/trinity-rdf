# 0019. Models are named graphs; ModelGroups query several at once

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
RDF stores hold multiple named graphs. Applications need to scope reads and writes to a
specific graph, and also to query across several graphs together — e.g. data + schema, or
data + an inferred/implicit graph.

## Decision
- `IModel`/`Model` represents a **single named graph** over an `IStore`, and is the unit for
  CRUD and queries.
- `IModelGroup`/`ModelGroup` (`ModelGroup : IModelGroup`, and `IModelGroup : IModel`)
  aggregates several models and is **itself an `IModel`**, so a query issued against a group
  transparently spans all member graphs. Groups are created via the store
  (`CreateModelGroup(...)`).

See `Trinity/Model/Model.cs`, `Trinity/Model/IModelGroup.cs`, `Trinity/Model/ModelGroup.cs`.

## Consequences
- Clean scoping: write into one model, read across many by grouping — without changing call
  sites, because a `ModelGroup` *is* an `IModel`.
- Correct group behavior depends on the store honoring multi-graph queries.
- Note the known Fuseki `CreateModelGroup(params IModel[])` bug (builds an empty group),
  tracked in [0009](0009-supported-store-backends.md).

## Notes added later
- A `ModelGroup` can only **union**: its dataset clause is one `FROM` per member, and SPARQL has no
  inverse of that. Subtracting a graph needs the guard inside the graph pattern, which is a different
  abstraction — see [0041](0041-layered-read-views.md), which adds `ILayeredModel` as a sibling rather
  than extending this one.
- `IModelGroup.DefaultModel` was **removed**. It was declared on the interface, auto-implemented on
  `ModelGroup`, and never read or assigned anywhere in the repository — an intent that was never wired
  up. A caller could set it and nothing would happen, which is worse than the property not existing.
  Removing it is a breaking change to a public interface, taken deliberately in 2.0.
  `IModelGroup` now adds nothing to `IModel` ∪ `ISet<IModel>`: it exists to name the combination — a set
  of models that is itself a model — rather than to add behaviour to it.

## Related
- [0008](0008-store-model-abstraction.md), [0009](0009-supported-store-backends.md),
  [0024](0024-sparql-reuses-registered-prefixes.md), [0041](0041-layered-read-views.md)
