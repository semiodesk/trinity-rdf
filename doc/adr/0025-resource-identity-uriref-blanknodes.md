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

## Revival notes
`UriRef.GetHashCode` combines hashes with bitwise **`&`** (`base.GetHashCode() &
Fragment.GetHashCode()`) instead of `^` / `HashCode.Combine` — poor, asymmetric distribution;
worth fixing (the NET35 `Utility/Tuple` has the same `&` bug).

## Related
- [0002](0002-attribute-based-object-mapping.md), [0016](0016-resource-centric-not-triple-centric.md)
