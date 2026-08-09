# 0017. Resources are open: mapped and dynamic (unmapped) properties coexist

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
A strongly-typed mapping ([0002](0002-attribute-based-object-mapping.md)) risks becoming a
closed world that cannot represent predicates the class did not declare. But RDF resources
are **open** — any predicate may apply to any subject. Trinity wants typed convenience
without losing the ability to carry arbitrary triples.

## Decision
A `Resource` maintains two parallel stores (`Trinity/Resource.cs`):
- `_mappings` — `Dictionary<string, IPropertyMapping>`, the typed/mapped properties backed
  by `PropertyMapping<T>`;
- `_properties` — `Dictionary<Property, HashSet<object>>`, arbitrary **unmapped** predicates.

The untyped triple API (`AddProperty(Property, …)`, `RemoveProperty`, `GetValue(Property)`)
lets any resource — including one with a mapped class — be **annotated dynamically at
runtime**. `AddPropertyToMapping` routes a value to the mapped holder when a compatible
mapping exists, otherwise into `_properties`. Crucially, **`ListValues` returns both mapped
and unmapped properties**, so enumeration and serialization see the complete resource.

## Consequences
- Best of both worlds: typed access for known predicates, open annotation for everything
  else; nothing is silently dropped on load/serialize round-trips.
- Consumers enumerating a resource's properties get a **mix** of mapped and dynamic values
  and must not assume "only my declared properties are present."
- The routing rules in `AddPropertyToMapping` (mapped vs. dynamic) are semantically load-bearing.

## Related
- [0002](0002-attribute-based-object-mapping.md), [0016](0016-resource-centric-not-triple-centric.md),
  [0018](0018-decorators-are-syntactic-sugar.md)
