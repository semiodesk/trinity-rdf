# 0029. Resource change tracking and object-level Commit/Rollback (no cascade)

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted — the whole-resource write behaviour described here is superseded by
[0039](0039-resource-write-semantics.md), which makes a commit write only the values that changed. The
rest (no cascade, the coarse `IsNew`/`IsSynchronized`/`IsReadOnly` flags, `Rollback()` re-fetching) still
holds; 0039 adds a per-resource `HasUnsavedChanges()` but no aggregate state.

## Context
Beyond store transactions ([0028](0028-store-level-transactions.md)), the resource objects
themselves need a way to persist or discard their in-memory changes, and to know whether they
are new or in sync with the store.

## Decision
`Resource` implements `ITransactional`:
- **`Commit()`** persists **this** resource to its model (`_model.UpdateResource(this)`), when
  it is attached to a model and not read-only (`Trinity/Resource.cs`).
- **`Rollback()`** **reloads** the resource from the model (re-fetch by URI via
  `Model.GetResource`), discarding in-memory changes and clearing the `ResourceCache`.
- Lightweight change-tracking flags **`IsNew`** (not yet persisted) and **`IsSynchronized`**
  (matches store) are maintained by `Model` create/get/update paths; **`IsReadOnly`** blocks
  commits.

## Consequences
- Simple per-resource save/reload semantics.
- **Commit does not cascade**: modifying a linked/related resource is *not* persisted by
  committing the parent — each resource must be committed itself. This is a common gotcha,
  compounded by lazy-loaded object graphs ([0023](0023-lazy-loading-via-resourcecache.md)).
- Tracking is coarse: `UpdateResource` rewrites the whole resource (no per-property dirty
  tracking), and `Rollback` incurs a full re-fetch.

## Revival notes
Document the no-cascade behaviour prominently; consider an opt-in cascade and finer-grained
dirty tracking.

## Related
- [0023](0023-lazy-loading-via-resourcecache.md), [0028](0028-store-level-transactions.md),
  [0016](0016-resource-centric-not-triple-centric.md)
