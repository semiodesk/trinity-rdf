# 0027. Language-tagged (localized) literals

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted — rudimentary; flagged for improvement. The replacement is designed in
[0046](0046-localized-literals-typed-containers.md); this becomes *Superseded by 0046* when that
lands.

## Context
RDF supports language-tagged string literals (`"Hallo"@de`). Applications need to store and
retrieve per-language values, and the mapping must carry the language tag alongside the string.

## Decision
A mapped `string` property is either language-invariant or language-tagged — declared via
`[RdfProperty(uri, languageInvariant)]`, with `PropertyMapping` tracking `Language` /
`LanguageInvariant`. Localized values are represented as a **string + culture** pair
(`Tuple<string, CultureInfo>`), serialized to the bare string with the language emitted as
`xml:lang`. On parse, an `xml:lang` literal is surfaced as a `string[] { value, lang }`
(`XsdTypeMapper.DeserializeXmlNode`). The untyped API carries a culture for localized values.

## Consequences
- Basic i18n works: values can be written and read per language.
- The representation is **inconsistent** — `Tuple<string, CultureInfo>` on one path,
  `string[] { value, lang }` on another — and there is no first-class "localized string" type
  or convenient per-language accessor. This is acknowledged as **rudimentary**.

## Revival notes
Introduce a proper localized-literal type and a consistent read/write API; unify with
`XsdTypeMapper` ([0026](0026-xsd-dotnet-datatype-mapping.md)).

Answered by [0046](0046-localized-literals-typed-containers.md). It also found that the
representation is worse than recorded above — there are **four** shapes, not two (the live read path
produces a `Tuple<string,string>` that this ADR does not name, and the `Tuple<string,CultureInfo>`
serializer silently drops the culture) — and that the ambient `Resource.Language` switch is the root
of nine defects, not merely an ergonomic wart.

## Related
- [0026](0026-xsd-dotnet-datatype-mapping.md), [0017](0017-resources-open-mapped-and-dynamic.md)
- [0046](0046-localized-literals-typed-containers.md) — the replacement design
