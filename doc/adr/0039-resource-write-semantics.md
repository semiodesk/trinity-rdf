# 0039. Commits write a per-value delta, not the whole resource

Date: 2026-08-10

## Status
Accepted (2.0) — supersedes the whole-resource write behaviour described in
[0029](0029-resource-commit-rollback-change-tracking.md).

## Context
`StoreBase.UpdateResource` wrote an existing resource as

```sparql
WITH <graph> DELETE { <uri> ?p ?o. } INSERT { …full serialization… } WHERE { OPTIONAL { <uri> ?p ?o. } }
```

so the read-modify-write cycle a caller performs to add one link rewrote the entire resource. Anything
the in-memory copy did not know about — values written by another caller after it was loaded, properties
that failed to materialize — was deleted without an exception.

The Electrix OS work stream measured this while fixing ADO 129003 (`doc/trinity-write-semantics.md`):
six `GetResource(parent) → add child → UpdateResource(parent)` cycles against one parent left **1 of 6**
links, identically on the in-memory store and Virtuoso. They worked around it by dropping to raw SPARQL.

This is not a documentation gap. No caller could avoid the loss, because the API offered no way to write
less than a whole resource.

## Decision
A resource snapshots its values whenever `IsSynchronized` becomes true — already the exact moment it is
declared to match the store, on materialization and after a commit. `Commit()` diffs the current values
against that snapshot and writes only what changed:

```sparql
WITH <graph> DELETE { <s> <p> <old>. } INSERT { <s> <p> <new>. } WHERE {}
```

An empty `WHERE` yields one solution, so the ground templates apply exactly once. A resource with no
changes issues no update at all.

Three things about the shape are deliberate:

**Per-value, not per-predicate.** Per-predicate granularity would not have fixed the measured case: six
writers each adding a child all touch the same predicate and would still overwrite one another. Only a
per-value delta lets concurrent additions coexist.

**Snapshot-and-diff, not setter tracking.** `PropertyMapping` seeds a list property with a plain
`List<T>`, so `parent.Children.Add(x)` never passes through a setter. A diff sees it regardless of how
the mutation happened, including changes to the unmapped property bag. This is what EF Core does for
proxy-less entities.

**Removals are computed against the resource's complete value list**, never the filtered one, so
`ignoreUnmappedProperties` can only suppress a write, never cause a delete. That flag was previously a
data-loss trap: the INSERT omitted unmapped triples while the DELETE still removed all of them.

A resource that was never synchronized has no baseline, so `TryBuildDeltaUpdate` returns false and the
caller falls back to whole-resource replacement. New resources are unaffected — they still INSERT.

The write path was duplicated four times (`StoreBase` plus the Virtuoso, GraphDB and Fuseki overrides)
with byte-identical bodies. All four now route through `StoreBase.TryBuildDeltaUpdate`, so the semantics
cannot drift apart; fixing only `StoreBase` initially left Virtuoso silently falling back. The bulk
`UpdateResources` path takes the same delta.

Two smaller decisions ride along:
- `Resource.IsUnresolved` flags an instance fabricated by `ResourceCache` for a link whose target the
  store did not return, so a dangling reference is distinguishable from a genuinely empty resource.
- `NoOpTransaction` replaces `null` from `BeginTransaction` on the non-transactional stores, so callers
  need not null-check and transaction code is testable in the in-memory fixture. It reports
  `IsolationLevel.Unspecified` — nothing is isolated and `Rollback()` cannot undo anything.

## Consequences
- Concurrent writers touching different values of a resource no longer erase each other. Two writers
  changing *the same* value still conflict, which is unavoidable without optimistic concurrency.
- `Commit()` means "apply my changes", not "make the store match this object". For a fully loaded
  resource these coincide; for a partially loaded one the new meaning is the expected one.
- A genuine per-resource dirty check exists for the first time (`Resource.HasUnsavedChanges()`). It is
  not aggregate: a resource can be clean while something it links to is dirty, because `Commit()` still
  does not cascade (0029).
- Blank nodes are illegal in SPARQL `DELETE` templates, and the delta names triples directly where the
  old code deleted through variables. The regression test for this is quarantined because blank-node
  values already fail on the *read* path (`doc/known-test-failures.md`), so the write-side hazard is
  currently uncovered.
- Verified against real stores, not just the in-memory one, because the emitted SPARQL changed:
  in-memory **397 passed / 0 failed / 7 skipped**, Virtuoso **99 passed / 7 pre-existing failures**,
  GraphDB **106 passed / 4 pre-existing failures**. The pre-existing failures are unchanged in identity,
  not merely in count — the four inferencing cases on both stores, plus three integer-datatype cases on
  Virtuoso. Fuseki has no fixture: different test structure, and the backend is 4/86 on an upstream
  connector bug.

## Related
- [0029](0029-resource-commit-rollback-change-tracking.md), [0016](0016-resource-centric-not-triple-centric.md),
  [0028](0028-store-level-transactions.md), [0036](0036-integration-tests-testcontainers.md)
- `doc/trinity-write-semantics.md` — the measurements this responds to
