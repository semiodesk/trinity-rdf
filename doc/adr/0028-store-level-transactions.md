# 0028. Store-level transactions (ADO-style ITransaction)

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted — inconsistently supported across backends

## Context
Some backends support transactional writes; applications need begin/commit/rollback with
isolation levels for multi-statement updates.

## Decision
`IStore.BeginTransaction(...)` returns an `ITransaction` (`Trinity/Transaction/ITransaction.cs`)
modelled on ADO.NET: `IDisposable`, a `System.Data.IsolationLevel`, `Commit()`/`Rollback()`,
and an `OnFinishedTransaction` event. Store write methods (`UpdateResource`, `DeleteResource`,
`ExecuteNonQuery`, …) accept an optional `ITransaction`.

## Consequences
- A familiar transaction model where the backend supports it (e.g. Virtuoso).
- **Support is uneven and undiscoverable**: Fuseki and GraphDB `BeginTransaction` returned
  `null` (no transactions), consistent with the absence of a capability model
  ([0022](0022-store-capabilities-and-istore-extension.md)). Callers must know their store.

## Revival notes
Document per-store support; surface transaction capability via a capability descriptor (0022);
ensure a null transaction degrades predictably rather than NRE-ing at call sites.

## Related
- [0022](0022-store-capabilities-and-istore-extension.md), [0029](0029-resource-commit-rollback-change-tracking.md),
  [0009](0009-supported-store-backends.md)
