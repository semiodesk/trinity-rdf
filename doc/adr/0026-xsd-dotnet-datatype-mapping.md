# 0026. XSD ↔ .NET datatype mapping with culture-invariant literals

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted

## Context
RDF literals carry XSD datatypes; .NET has its own primitive types. The mapping between them
must be lossless and **culture-independent** — a `decimal`, `double`, or `dateTime` must
serialize identically regardless of the machine's locale.

## Decision
`XsdTypeMapper` (`Trinity/XsdTypeMapper.cs`) holds bidirectional dictionaries (`NativeToXsd`,
`XsdToNative`) for a fixed set of primitives — integer family, `decimal`/`double`/`float`,
`bool`, `DateTime`→`xsd:dateTime`, `TimeSpan`→`xsd:duration`, `byte[]`→`xsd:base64Binary`,
`Uri`→`xsd:anyURI` — plus per-type serialize/deserialize delegates built on
`System.Xml.XmlConvert`, which uses the **invariant** XSD lexical forms. `PropertyMapping<T>`
restricts `T` to these supported types (plus `IResource`/`Uri`/tuple/lists).

## Consequences
- Stable, locale-independent round-tripping of typed literals.
- The supported type set is **fixed and hardcoded**; unknown datatypes fall back to `string`.
  There is no extension point for custom datatypes.
- Minor inconsistencies to tidy (`boolean_` alias; `xsd:integer`→`Int32` vs
  `xsd:nonNegativeInteger`→`UInt64`).

## Revival notes
Consider an extensible datatype registry, add missing common XSD types, and unify with the
localized-literal path ([0027](0027-localized-literals.md)).

## Related
- [0027](0027-localized-literals.md), [0002](0002-attribute-based-object-mapping.md)
