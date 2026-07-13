# 0020. Runtime metadata via global MappingDiscovery and OntologyDiscovery

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
To materialize typed resources, the runtime must know which .NET types map to which RDF
classes. To resolve/serialize terms and to declare SPARQL prefixes
([0024](0024-sparql-reuses-registered-prefixes.md)), it must know the registered ontologies
(prefix → namespace, plus the `Property`/`Class` instances). This metadata is gathered from
the consumer's own assemblies.

## Decision
Two **static** registries scan assemblies at runtime:
- `MappingDiscovery` (`Trinity/MappingDiscovery.cs`) — discovers mapped/`[RdfClass]` types;
  populated via `RegisterAssembly(asm)` / `RegisterCallingAssembly()`.
- `OntologyDiscovery` (`Trinity/OntologyDiscovery.cs`) — holds `Namespaces` (prefix→`Uri`),
  `Properties`, `Classes`; populated via `AddAssembly(...)`, `AddNamespace(prefix, uri)`,
  `RegisterCallingAssembly()`.

Consumers **explicitly register** their model/vocabulary assemblies at startup (both `elxgen`
and DevHub call these in their bootstrap).

## Consequences
- Simple and DI-free: one-time registration lights up mapping, term resolution, and SPARQL
  prefix injection.
- The registries are **global, static, mutable state** — not scoped per store/model,
  order-sensitive, and awkward for test isolation and multi-tenant hosts.
- Forgetting to register an assembly causes **silent misses**: types are not materialized,
  prefixes are not declared, queries fail at the store.

## Revival notes
Consider instance-scoped discovery (registered on the store/model, or via DI) to remove the
global static state while keeping a zero-config default.

## Related
- [0002](0002-attribute-based-object-mapping.md), [0005](0005-ontology-code-generation.md),
  [0024](0024-sparql-reuses-registered-prefixes.md)
