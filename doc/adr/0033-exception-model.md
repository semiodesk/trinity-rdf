# 0033. Explicit exception model

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
Failures in mapping, querying, store selection, and resource state should produce clear,
catchable signals rather than generic exceptions.

## Decision
`Trinity/Exceptions/` defines domain-specific exceptions thrown across the query/store/resource
APIs:
- `InvalidQueryException`, `QueryTypeNotSupportedException`
- `ResourceNotFoundException`, `ResourceLockedException`
- `StoreProviderMissingException`
- `ResourceBlankException`, `InvalidBlankNodeIdentifierResultException`

## Consequences
- Callers can catch specific conditions (missing provider, unsupported query form, blank-node
  misuse, locked/absent resource).
- The set is **ad hoc**: there is no common base type, so blanket handling of "a Trinity error"
  is awkward, and the set is not documented as a whole.

## Revival notes
Introduce a common `TrinityException` base type; document which APIs throw which exceptions.

## Related
- [0022](0022-store-capabilities-and-istore-extension.md), [0008](0008-store-model-abstraction.md)
