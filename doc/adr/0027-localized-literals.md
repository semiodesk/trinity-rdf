# 0027. Language-tagged (localized) literals

Date: 2026-07-13 (grounding decision, original to the design)

## Status
Accepted — rudimentary; flagged for improvement

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

## Related
- [0026](0026-xsd-dotnet-datatype-mapping.md), [0017](0017-resources-open-mapped-and-dynamic.md)
