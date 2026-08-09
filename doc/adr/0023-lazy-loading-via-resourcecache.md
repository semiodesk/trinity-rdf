# 0023. Lazy loading of linked resources via ResourceCache (not currently disablable)

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted — with a known limitation flagged for the revival

## Context
Resources reference other resources. Eagerly materializing the whole object graph on every
load would be expensive and could pull in large subgraphs. Trinity lazily loads linked
resources so that a load stays cheap.

## Decision
Each `Resource` owns a `ResourceCache` (`Trinity/Resource.cs`, `Trinity/ResourceCache.cs`)
that defers resolution of linked resources: a mapped object-valued property stores the target
URI, and the actual `Resource` is materialized on first access
(`ResourceCache.CacheValue(...)` when set from the model; resolved lazily via the cache in
`GetValue`/`LoadCachedValues`). The cache is bound to the resource's `Model`.

## Consequences
- Cheap loads; linked resources are fetched on demand.
- **Lazy loading is always on — there is currently no switch to disable it** or to force
  eager loading. This can cause surprise N+1-style fetches, and it makes a resource's ability
  to resolve links depend on its `Model` still being available/open.
- The cache is per-resource state; combined with the `Resource` finalizer/`Dispose`, it adds
  GC and lifetime considerations.

## Revival notes
Expose a way to configure eager vs. lazy loading (per model or per query) and to opt out;
revisit the per-resource cache and finalizer lifetime model.

## Related
- [0002](0002-attribute-based-object-mapping.md), [0019](0019-models-are-named-graphs-modelgroups.md)
