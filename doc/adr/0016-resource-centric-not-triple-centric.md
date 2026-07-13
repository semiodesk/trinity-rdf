# 0016. Resource-centric persistence, not triple-centric

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
RDF is natively a set of triples, and dotNetRDF exposes triple-level manipulation
directly. Trinity deliberately raises the unit of work to the **resource** — a subject
IRI together with all of its properties — to match object-oriented / domain-driven
programming and to make the object-mapping model ([0002](0002-attribute-based-object-mapping.md))
coherent.

## Decision
The public API operates at **resource granularity**. Applications load, mutate, and
persist whole `Resource` objects (`GetResource<T>`, `CreateResource<T>`, `AddResource`/
`UpdateResource`, `DeleteResource`). A resource carries its own property values
(`_properties` + `_mappings` in `Trinity/Resource.cs`), and the store reconciles them on
persist (e.g. `UpdateResource` rewrites that resource's triples). There is **no supported
API to add or remove a single triple in isolation** — triple changes are expressed by
modifying a resource and persisting it.

## Consequences
- Update semantics operate on a coherent unit (a resource), which fits the mapping model
  and `Commit`/update flows.
- Callers cannot express a pure "add one triple" / "delete one triple" operation through
  Trinity; the granularity is always the resource. Genuine triple-level work goes around
  Trinity via dotNetRDF directly (as `elxgen` does for some graph operations).
- Persisting is effectively read–modify–write at the resource level; triple-level merge or
  fine-grained concurrency is not a first-class concept.

## Related
- [0002](0002-attribute-based-object-mapping.md), [0017](0017-resources-open-mapped-and-dynamic.md),
  [0008](0008-store-model-abstraction.md)
