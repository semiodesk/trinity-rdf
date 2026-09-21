# 0046. Localized literals are typed containers, not ambient resource state

Date: 2026-09-17

## Status

Proposed (2.0). The design below is agreed; implementation follows. On landing, this supersedes
[0027](0027-localized-literals.md), whose status becomes *Superseded by 0046*.

## Context

RDF literals carry language tags (`"Hallo"@de`). [0027](0027-localized-literals.md) recorded how
Trinity handles them and marked itself *"Accepted — rudimentary; flagged for improvement"*, with a
Revival note asking for *"a proper localized-literal type and a consistent read/write API"*. The
same request has been sitting in the code since 2016 as `// TODO: Write a custom string class with
an associated language` (`Trinity/Resources/Resource.cs:534`). This ADR answers it.

### Language is a mode switch on the object

`Resource.Language` is a settable `string` whose setter calls `ReloadLocalizedMappings()`
(`Resource.cs:1367`). That method does not filter a view — it **moves values**. For every
non-`LanguageInvariant` `string`/`List<string>` mapping it drains the mapped value back into the
untyped `_properties` bag, clears the mapping, then refills it from the bag with the values whose
tag matches the new language. So at any instant **exactly one language is mapped and the rest sit
in the bag**.

Three consequences follow, and they are why this design cannot be kept:

- **A mapped property is a single-language window.** There is no way to ask a mapped `string` which
  languages exist. The answer is only in the untyped bag, as `Tuple<string,string>` values.
- **It is not usable concurrently.** The setter mutates `_properties` and every string mapping, so
  two threads reading one `Resource` in different locales corrupt each other's *storage*, not merely
  each other's view. A web API serving per-request locales has no safe reading at all.
- **Correctness rests on an invariant nobody stated.** Committing while a language is active does
  not wipe the other languages — but only because every language's value is in exactly one of the
  bag or the mapping, and `ListValues(bool)` enumerates both. Nothing names or tests that invariant;
  remove either half and the other languages are silently deleted.

This design works well in a desktop application, where one UI language is active at a time and one
thread owns the object. That is the environment it was written for. It does not survive being a
library API.

### One concept, four representations

| Shape | Where |
|---|---|
| `Tuple<string,string>` | untyped writes (`Resource.cs:539,553`); dotNetRDF read (`dotNetRDFQueryResult.cs:371`); Virtuoso read (`Trinity.Virtuoso/VirtuosoSparqlQueryResult.cs:120`) |
| bare `string` + out-of-band `IPropertyMapping.Language` | inside a mapping, while a language is active |
| `Tuple<string,CultureInfo>` | serializer key (`XsdTypeMapper.cs:140,351`) — and it **drops the culture** |
| `string[] { value, lang }` | `XsdTypeMapper.DeserializeXmlNode:741` — appears unreferenced |

`SparqlSerializer.SerializeValue` branches over three of them, each with the identical comment
`// string + language`. [0027](0027-localized-literals.md) named two of the four; the third and
fourth were found while writing this ADR.

### The defects this causes

Every one of these is a consequence of there being no type whose job is to know what a
language-tagged literal *is*, so each call site re-derives it and they disagree.

| # | Where | Defect |
|---|---|---|
| 1 | `Resource.cs:1099,1114` | **`ListValues(Property)` double-wraps.** `GetValueObject()` has already tagged the value, so `x as string` is `null` and `ListValues(Property)`/`GetValue(Property)` return `Tuple<null, lang>`. Invisible because the tests assert only `.Count()`. |
| 2 | `dotNetRDFQueryResult.cs:372` vs `Resource.cs:244` | **Tag-case churn.** Reads preserve the server's casing; writes lower-case it. Against [0039](0039-resource-write-semantics.md)'s ordinal delta a value read as `@en-US` and committed after `Language = "en-US"` emits a spurious `DELETE '…'@en-US` + `INSERT '…'@en-us`, and flips `HasUnsavedChanges()`. |
| 3 | `SparqlQueryProvider.cs:178,287` | **LINQ projection throws.** `Convert.ChangeType` on a `Tuple<string,string>` raises `InvalidCastException`, so `Select(p => p.Name)` fails outright if any value is tagged. The adjacent `Uri` → `UriRef` special case exists for precisely this reason. |
| 4 | `Resource.cs:976` | `HasProperty(p, v, string lang)` omits the `.ToLower()` that `AddProperty` applies, and lookup is an ordinal tuple match — so `HasProperty(p,"x","DE")` is **false** after `AddProperty(p,"x","DE")`. |
| 5 | `Resource.cs:1388,1399` | `_properties[key].Remove(v)` leaves **empty `HashSet` entries**, breaking the invariant `HasProperty(Property)` documents at `:892-895`. The bare indexer also throws `KeyNotFoundException` when two mappings share a predicate. |
| 6 | `Resource.cs:297-309` | The copy constructor drops `_language` but shares `_mappings` **by reference**, so the copy's two language views disagree. |
| 7 | `PropertyMapping.cs:510` | `&&`/`\|\|` precedence makes the type guard a tautology; the condition reduces to `LanguageInvariant \|\| IsNullOrEmpty(Language)`. Since `Language` has a **public setter**, setting it on a non-string mapping yields `Tuple(null, lang)` or an NRE in `ToLanguageList`. |
| 8 | `SparqlQueryTranslator.cs:2520` | **LINQ cannot express a language at all** — all five `LiteralTerm` sites pass `null`. Worse, it is *inconsistent with itself*: `==` emits a plain literal and never matches `"Hallo"@de`, while `Contains` emits `CONTAINS(?v,"…")`, which SPARQL argument compatibility (§17.4.3) **does** match against tagged values. The provider silently disagrees about whether tagged data exists. |
| 9 | `Resource.cs:1395` | Matching is an exact case-insensitive compare — **no BCP-47 fallback**. `de` never matches `de-DE`. |

Also: the `.ToLower()` calls are culture-sensitive rather than invariant (Turkish-I);
`XsdTypeMapper` has no `Tuple<string,string>` serializer at all, and `DeserializeLiteralNode`
ignores `node.Language` entirely; `SerializeTranslatedString` emits the invalid `'x'@` for
`CultureInfo.InvariantCulture`, whose `.Name` is `""`; and the two `ListValues` overloads implement
language independently — one reads `mapping.Language`, the other re-derives from
`Resource.Language` — so they disagree.

## Decision

**A language-tagged literal gets one CLR type, and a mapped property holds every language at once
in a container that declares its own multiplicity. `Resource.Language` is removed.**

### `LangString` — the literal term

```csharp
public sealed class LangString : IEquatable<LangString>, IComparable<LangString>
{
    public string Value { get; }      // never null
    public string Language { get; }   // never null or empty; lower-cased invariantly at construction
    public LangString(string value, string language);
    public LangString(string value, CultureInfo culture);
    public bool MatchesLanguage(string range);                   // RFC 4647 basic filtering
    public static bool operator ==(LangString l, LangString r);  // and !=
    public override string ToString();                           // Value
    public string ToNTriples();                                  // "Hallo"@de
}
```

This replaces all four representations above. `SparqlSerializer.SerializeValue`'s three branches
collapse to one — the clearest measure that the change worked.

**A class, not a struct.** An RDF term reads like a value type, but the engine is `object`-typed end
to end (`HashSet<object> _properties`, `GetValueObject()`, `SetOrAddMappedValue(object)`), so the
allocation saving is boxed away on the first hop while the correctness cost is real:
`PropertyMapping<T>.Clear()` does `_value = default(T)`, which for a struct is a `LangString` with a
null `Value` — an invalid literal handed to user code — and `AddPropertyToMapping`'s `value == null`
guard (`Resource.cs:453`) could never fire. Nullable reference types are off repo-wide, so
`LangString?` would be `Nullable<LangString>`, a distinct type every compatibility check would have
to special-case. `UriRef` is a class for the same family of reasons.

**No implicit conversion, in either direction.** This applies [0025](0025-resource-identity-uriref-blanknodes.md)'s
lesson rather than repeating it. `implicit operator string` would make `person.Label == "Hallo"`
bind to `string.operator ==` and return **true** for `"Hallo"@fr`; the reverse conversion would make
it return **false** for `"Hallo"@de`. Both are silent, in the most-typed expression in the API. With
neither, that expression is **CS0019, no applicable operator** — a compile error, which is exactly
what [0025](0025-resource-identity-uriref-blanknodes.md) wishes it could have achieved for `Uri` and
could not.

But `==`/`!=` between two `LangString`s **must** be declared: without them a class falls back to
reference equality and `a == b` is silently false for equal literals, which is the
[0025](0025-resource-identity-uriref-blanknodes.md) defect verbatim.

**The tag is lower-cased invariantly at construction.** This is what makes defect 4 unrepeatable
rather than merely fixed: neither `AddProperty` nor `HasProperty` lowers the tag any more, so they
cannot disagree about it. It also settles defect 2, because [0039](0039-resource-write-semantics.md)'s
delta compares serialized strings ordinally and now sees a stable form on both sides of a read/commit
cycle. RDF 1.1 compares language tags case-insensitively.

**A plain literal stays a plain `string`; `Language` is never null.** Admitting a null tag would
create a second representation of an untagged literal and reopen the exact problem this ADR closes.
It also preserves at the type level the semantics `ResourceTest.HasPropertyTest2` pins as *"Current
interpretation -> Value+Language != Value"*: `"Hallo"` and `new LangString("Hallo","de")` are
different types and can never compare equal.

### `LocalizedString` and `LocalizedStringCollection` — the mapped containers

Multiplicity **mirrors the scalar/collection split authors already use**. Choosing between
`LocalizedString` and `LocalizedStringCollection` is the same act as choosing between `string` and
`List<string>`, one level down: the first holds one value per language, the second many.

```csharp
public sealed class LocalizedString : IEnumerable<LangString>
{
    public string this[string language] { get; set; }      // exact match; set replaces
    public string this[CultureInfo culture] { get; set; }
    public IReadOnlyCollection<string> Languages { get; }
    public string Invariant { get; set; }                   // the untagged literal, if any
    public string Best();                                   // CurrentUICulture, then Invariant
    public string Best(params string[] languageRanges);     // RFC 4647 Lookup
    public bool TryGetBest(string range, out LangString match);
    public bool Contains(string language);
    public bool Remove(string language);
    public int Count { get; }
    public bool IsEmpty { get; }
}
```

`LocalizedStringCollection` adds `Add(language, value)`, `All(language) => IReadOnlyList<string>`,
`Remove(language, value)` and `AllInvariant`.

`Languages` is the direct answer to *"which languages are available"* — the question the old design
could not answer from the mapped surface at all.

**The indexer is exact-match on get and set.** A getter that falls back while the setter does not
makes `t[k] = t[k]` a non-no-op, which is a classic trap. Fallback lives in `Best`/`TryGetBest`,
where it is named and opt-in, and that is the single place defect 9 is fixed.

**`Best` implements RFC 4647 §3.4 Lookup**: truncate the request at the last `-`, skipping singleton
subtags, and retry — `de-DE-1901` → `de-DE` → `de`. Note the direction: `Best("de-DE")` finds `@de`,
but `Best("de")` does **not** find `@de-DE`. See *Limits*.

### The authoring model: the property type states the intent

| Declared type | Sees | Replaces |
|---|---|---|
| `string`, `List<string>` | untagged literals only | `[RdfProperty(uri, languageInvariant: true)]` |
| `LocalizedString` | every language, one value each | `[RdfProperty(uri)] string` over tagged data |
| `LocalizedStringCollection` | every language, many values each | — |
| `LangString`, `List<LangString>` | tagged literals, raw triple view | `PropertyMapping<Tuple<string,string>>` |

```csharp
[RdfProperty(rdfs.label)]
public partial LocalizedString Label { get; }       // get-only

[RdfProperty(schema.identifier)]
public partial string Identifier { get; set; }      // untagged only
```

**`languageInvariant` becomes redundant**, because the property's type now carries the information —
a `string` property is language-invariant by construction. The two-argument
`RdfPropertyAttribute` constructor is marked `[Obsolete]` (removed in 2.1) rather than deleted, so
1.x call sites get an explanation instead of CS1501, and **TRIN008** (Info) fires when the generator
sees the flag. The old flag turns out to be an exact partition of the migration:
`languageInvariant: true` stays `string`; every other string-typed mapped property becomes
`LocalizedString`.

**Containers are get-only.** `article.Label = null`, and `article.Label = other.Label` aliasing one
container across two resources, are both removed at compile time. This requires a generator fix —
`Render` emits `get`+`set` unconditionally (`MappingSourceGenerator.cs:200-205`), which is a compile
error against a get-only declaring half — plus **TRIN009** (Warning) when a container property
declares a setter. Emitting only the accessors the declaration declares is independently correct for
every mapped type. The container is seeded by `PropertyMapping<T>`'s constructor rather than by the
generator: unlike `IList<T>`, it is concrete, so no new generated constructor shape is needed.

### Routing: how a tagged literal reaches the mapping

`IsTypeCompatible` (`PropertyMapping.cs:565-572`) already carries explicit widening cases for
`Resource` and for `Uri` → `UriRef`, with a comment noting the gate must agree with what
`SetOrAddMappedValue` will accept. This adds one more in the same shape: a container mapping accepts
`LangString` **and** `string`; a `string` mapping accepts `string` only — which is what makes
`string` mean untagged-only.

That single change is what dissolves the bag/mapping shuffle. Store-materialized `LangString`s now
land *in* the mapping (today `IsValueCompatible` refuses a `Tuple<string,string>` for a
`PropertyMapping<string>`, which is why every tagged literal falls into `_properties` and why
`ReloadLocalizedMappings` had to exist at all).

`IsList` currently doubles as "holds more than one value", and a container is multi-valued without
being an `IList`. Rather than lie about `IsList` — which sends the constructor into
`GetGenericArguments()[0]` on a non-generic type — `IPropertyMapping` gains
`IEnumerable<object> EnumerateValues()`, and the enumeration sites route through it.

### `Resource.Language` is removed

Along with `IResource.Language`, `ReloadLocalizedMappings`, `TransferMappingToProperties`,
`IPropertyMapping.Language`, `PropertyMapping<T>.Language`, `LanguageInvariant` and `ToLanguageList`.
`GetValueObject()` returns `_value` unconditionally.

Defects 1, 5, 6 and 7 **disappear** rather than being patched — each exists only because of the
ambient state or the wrapping it required. And the unstated invariant that made commits safe is
replaced by a structural one: every language lives in the container, `GetValueObject()` becomes
state-free, and the [0039](0039-resource-write-semantics.md) snapshot no longer depends on when
`Language` was last assigned.

The untyped API gains `ListLanguages(Property)` and `ListLanguages()`; `AddProperty`,
`RemoveProperty` and `HasProperty` keep their signatures and construct a `LangString`.

Removing the member also removes a name collision: `Language` is a natural domain property, and
`tests/Trinity.Generator.Tests/GeneratorMappingTest.cs:75` exists solely to pin the `new`-hiding
workaround, which [0013](0013-replace-il-weaving-with-source-generator.md) notes must appear on both
halves or it is an unfixable CS8800.

### LINQ

Under this design `string` genuinely means untagged, so the translator's existing comment —
*"Stored as a plain literal; emit plain so term equality matches"* — becomes **true** rather than a
workaround. The default `==` path does not change. What is added:

- `ToTerm` gains a `LangString` case populating the dormant `LiteralTerm.Language`
  (`SparqlAst.cs:224`); `SparqlQueryWriter.WriteLiteral:333-335` already emits it.
- A chain kind for `LocalizedString.get_Item`, so `Where(a => a.Label["de"] == "Hallo")` emits
  `FILTER (?v = "Hallo"@de)` — exact term equality, which stores can index.
- Query-marker extension methods, needing no new AST node because `SparqlFunctionExpression` takes
  an arbitrary name: `HasLanguage(range)` → `langMatches(lang(?v), "…")`; `IsPlain()` →
  `lang(?v) = ""`; `LanguageTag()` → `lang(?v)`, projectable and groupable; `Lexical()` → `STR(?v)`.
- Defect 3 is fixed by adding a `LangString` case to `CoerceValue`/`ExecuteBindings`, mirroring the
  `Uri` → `UriRef` case beside it.
- **`Best(...)` inside a query throws `NotSupportedException`.** RFC 4647 Lookup is a client-side
  fallback walk with no faithful SPARQL; quietly translating it to `langMatches` would return the
  wrong rows. Refusing loudly is the posture [0041](0041-layered-read-views.md) established.
- `ORDER BY` on a localized member emits `ORDER BY STR(?v)`. SPARQL 1.1 §15.1 leaves the relative
  order of literals with *different* language tags implementation-defined, so the raw form diverges
  across backends. This is a stronger statement than [0037](0037-linq-provider-rebuild.md)'s
  "codepoint, not .NET", and it means culture-aware collation cannot be pushed down at all — a
  caller needing `de-DE` collation must materialize and sort client-side.

## Alternatives rejected

**Keep `Language`, reinterpreted as a read-time preference that filters instead of moving values.**
Tempting, because the desktop ergonomics are genuinely good. Rejected because it is still ambient
mutable state on an object shared between requests — the concurrency problem is unchanged — and
because it keeps `Language` occupying a natural domain name. The desktop convenience is recovered
without any Trinity-specific mechanism: `Label.Best()` consults `CultureInfo.CurrentUICulture`,
which every UI framework already sets and which is `AsyncLocal`-scoped on modern .NET.

**A `ReadOptions`/`LanguagePreference` model view** — `model.WithLanguages("de","en")` returning an
immutable view that materializes resources with a language preference. This was designed in full and
rejected as *unnecessary under this ADR*: it exists to answer "which language does a mapped `string`
property show", and a container that holds every language deletes the question. It also carried real
cost — per-concrete-class copy constructors for `Model`, `ModelGroup` and `LayeredModel` (a
delegating decorator would break the `model is ILayeredModel` type tests in
`SparqlSerializer.GenerateDatasetClause` and silently read the un-subtracted baseline, exactly
[0041](0041-layered-read-views.md)'s failure mode), plus new `GetResource` overloads that would have
to dodge the reflection-by-name handles in `Model.cs:107`, `ModelGroup.cs:114` and
`LayeredModel.cs:182`. Revisit only if a genuinely *filtered fetch* is ever wanted.

**A single container type with many-per-tag storage and a single-valued view.** The argument for it
is that RDF places no cardinality constraint on `(subject, predicate)`, so a one-per-language
container facing two `@de` values must either throw or drop. That is true — but it is an objection
to Trinity's scalar mapping concept as a whole, not to per-language cardinality: `string` against a
multi-valued predicate already drops, because `SetOrAddMappedValue`'s scalar branch does
`_value = (T)value` and documents itself as *"replaces the current value if it is mapped to one
value"*. Mirroring the existing split keeps one rule for authors to learn and gives them the same
escape hatch they already know. The lossiness is recorded under *Consequences* rather than designed
around.

**Filtering the fetch rather than the mapped surface.** Five stores build the resource read as a
`DESCRIBE` (`StoreBase:699` and the four backends), which has nowhere to hang a `FILTER`; a filter
on `?o` would need `!isLiteral(?o) || lang(?o) = "" || langMatches(…)` merely to avoid dropping every
IRI and number. And a partially-loaded resource is dangerous: `StoreBase.UpdateResource`'s
no-baseline branch replaces wholesale with `DELETE { <s> ?p ?o }`, which would destroy the languages
the copy never loaded. Keeping the resource complete is what keeps [0039](0039-resource-write-semantics.md)
honest.

## Consequences

- **One representation.** Four shapes collapse to `LangString`; `SparqlSerializer`'s three branches
  become one; `XsdTypeMapper`'s vestigial `Tuple<string,CultureInfo>` serializer (which dropped the
  culture) and its `string[]` parse output are deleted.
- **Nine defects addressed**, four of them by construction rather than by patch.
- **Concurrency becomes possible.** A `Resource` is still single-threaded for writes, as it always
  was, but reading it in two locales no longer involves shared mutable state.
- **`LocalizedString` against multi-valued data is lossy, deliberately.** If the store holds two
  `@de` labels for a `LocalizedString` property, one is dropped — and because
  `TrySerializeResourceDelta` computes removals from what the resource currently holds
  (`SparqlSerializer.cs:249`), the dropped value is then **deleted from the store** on the next
  `Commit()`. This is exactly what a scalar `string` already does to a multi-valued predicate.
  `LocalizedStringCollection` is the escape hatch, as `List<string>` is today.
- **Tag casing is normalized.** A store returning `de-DE` round-trips as `de-de`. Semantically
  identical under RDF 1.1, visible in a Turtle dump.
- **Both store read paths change together**, including the easily-missed second one in a different
  project (`Trinity.Virtuoso/VirtuosoSparqlQueryResult.cs:120`). That adapter also tests `StrType`
  before `StrLang`, the inverse of the RDF 1.1 ordering the core path documents at
  `dotNetRDFQueryResult.cs:367-369`; it works today only because Virtuoso leaves `StrType` null for
  tagged literals, and it is corrected while there.
- **Migration is compile-error-driven except in one case.** `r.Language = "de"` becomes CS1061;
  `(Tuple<string,string>)r.GetValue(p)` becomes an `InvalidCastException`; a hand-written
  `PropertyMapping<Tuple<string,string>>` throws at registration, surfaced through
  `MappingDiscovery`'s `AggregateException` ([0020](0020-runtime-metadata-discovery.md)). **The
  silent case**: a model that declared `string` and whose data is all `@en` reads `null` after the
  upgrade with no compile error. Nothing is *lost* — the values stay on the resource, remain visible
  via `ListValues(property)`, and the delta will not remove them — but this must lead the migration
  note. `.OfType<Tuple<string,string>>()` in consumer code is the other silent case; it returns
  empty and should be grepped for.
- **The test suite's blind spot is closed.** The existing localized tests assert only `.Count()`,
  which is why defect 1 survived: the counts were right and every value was null. Going forward, a
  localized test may not assert only cardinality.

## Limits worth stating

- **`Best("de")` does not find `@de-DE`.** RFC 4647 Lookup truncates the *request*, not the
  available tags. For `skos:prefLabel` the opposite is often wanted; an extended-match mode can be
  added later without breaking this one.
- **`LangString.ToString()` returns `Value`**, optimizing for string interpolation over diagnostic
  clarity; `ToNTriples()` and `[DebuggerDisplay]` cover the latter. `UriRef.ToString()` likewise
  gives no hint of its fragment-awareness.
- **No culture-aware collation, at any layer.** SPARQL has no collation, so ordering is codepoint
  ordering and cross-tag ordering is store-defined.
- **`LocalizedString` picks a value, not a merge.** Where several values share a tag, the container
  surfaces the first in arrival order — stable within an object, arbitrary across sessions because
  store row order is.
- **`JsonResourceConverter` is unexamined.** A sealed type with getter-only properties and no
  parameterless constructor does not round-trip through Newtonsoft by default; this needs verifying
  before implementation, and may need an explicit converter.
- **No IRI or datatype containers.** This ADR covers `rdf:langString` only. The extensible datatype
  registry [0026](0026-xsd-dotnet-datatype-mapping.md) asks for remains future work.

## Related
- [0027](0027-localized-literals.md) — superseded by this; its Revival note is this ADR's brief
- [0026](0026-xsd-dotnet-datatype-mapping.md) — the datatype path this unifies with
- [0025](0025-resource-identity-uriref-blanknodes.md) — the implicit-conversion and `==` lesson applied here
- [0017](0017-resources-open-mapped-and-dynamic.md) — why the untyped bag keeps showing every value
- [0018](0018-decorators-are-syntactic-sugar.md) — why the property type, not the attribute, carries the meaning
- [0039](0039-resource-write-semantics.md) — the delta whose stability depends on tag normalization
- [0037](0037-linq-provider-rebuild.md) — the ordering contract this narrows
- [0041](0041-layered-read-views.md) — the refuse-rather-than-mistranslate posture `Best()` follows
