# 0025. Resource identity: fragment-aware URIs (UriRef), URNs, and blank nodes

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
RDF identifies resources by IRI, and **fragment identifiers are semantically significant**:
`ex:doc#a` and `ex:doc#b` are different resources. But .NET's `System.Uri.Equals`
**ignores the fragment**, so two distinct RDF resources would compare equal — a correctness
hazard for a resource-centric mapper ([0016](0016-resource-centric-not-triple-centric.md)).
RDF also has **blank nodes** (locally-scoped anonymous identifiers) needing their own
identity, and .NET's `Uri` lower-cases the host, which corrupts case-sensitive IRIs for some
stores (notably Virtuoso).

## Decision
- `UriRef : Uri` (`Trinity/UriRef.cs`) overrides `Equals`/`GetHashCode` to **factor in the
  fragment**, so fragment-distinct IRIs are distinct resources. It carries an `IsBlankId`
  flag; blank-node refs compare by `OriginalString`. `UriRef.GetGuid()` mints
  `urn:uuid:{GUID}` identifiers for new resources.
- Blank nodes are modelled via `BlankId` (`Trinity/BlankId.cs`) and the `IsBlankId` path;
  URNs via `Trinity/Urn.cs`.
- Serialization uses `Uri.OriginalString` (not `AbsoluteUri`) to preserve host case for stores
  like Virtuoso (see the comment in `XsdTypeMapper.SerializeIResource`).

## Consequences
- Correct RDF identity semantics for fragment IRIs and blank nodes that the framework `Uri`
  cannot provide.
- All resource identity must flow through `UriRef`; mixing raw `Uri` and `UriRef` risks
  incorrect equality.

## Amendment (2026-08-26): an `Equals` override is not sufficient

The original decision assumed that overriding `Equals`/`GetHashCode` was enough to make identity
fragment-aware. It is not, and .NET 10 made that visible.

**.NET 10 added `IEquatable<Uri>` to `System.Uri`.** `EqualityComparer<T>.Default` prefers the
strongly-typed interface over the `object` overload, so on that runtime every `HashSet<Uri>`,
`Dictionary<Uri,…>`, `Contains`, `Distinct` and `GroupBy` routed *around* `UriRef.Equals` and compared
fragment-blind. Nothing had to be recompiled: `Trinity` targets netstandard2.0, so a consumer merely
running on .NET 10 inherited the defect. `UriRef` now implements `IEquatable<Uri>`.

Overriding `Equals` also never covered `==`, which binds **statically**. Two `UriRef` values compared
with `==` resolved to `Uri.operator ==`. `UriRef` now declares `operator ==`/`!=`.

**One hazard survives and cannot be removed.** The operators only apply when *both operands are
declared* `UriRef`. Assign a `UriRef` to a variable of type `Uri` and `==` binds to `Uri.operator ==`
again; a static operator cannot be overridden. This is why "use `UriRef`" is a **rule**, not a
preference, and why it is now enforced in two places:

- **`TRIN007`** (warning) — the source generator reports a mapped property declared `System.Uri`, or a
  mapped collection of them.
- **`PropertyMapping<T>`** throws for `System.Uri`, in Release builds too, because mappings can also be
  written by hand ([0018](0018-decorators-are-syntactic-sugar.md)) where the generator
  never sees them.

Three exact `typeof(Uri)` identity checks had to be relaxed first, or the advice could not be followed:
`PropertyMapping`'s DEBUG type whitelist rejected `UriRef`, `XsdTypeMapper.SerializeObject` threw "no
serialiser available" for a `UriRef` value, and `Resource.AddPropertyToMapping` routed `UriRef` mappings
down the `ResourceCache` branch instead of the direct-set one. A subclass fails an exact type test; all
three now resolve to the registered base type.

`Resource.Equals` compared with `==` and so had the same defect, silently affecting `Class` (which
declares no equality of its own) and therefore type resolution for every query result. It now compares
`OriginalString`, as `Property.Equals` already did — which also makes it agree with `Resource.GetHashCode`,
which has always hashed `OriginalString`.

`GetHashCode` combined the two hashes with bitwise **`&`** rather than a mix, collapsing distinct
identifiers into shared buckets; it now mixes. `Equals` and `GetHashCode` also disagreed about which
operand's `IsBlankId` decided the branch, so the contract held only through short-circuit evaluation.

## Revival notes
The `&`-instead-of-mix hash bug this ADR originally flagged is fixed in `UriRef`

## Related
- [0002](0002-attribute-based-object-mapping.md), [0016](0016-resource-centric-not-triple-centric.md)
