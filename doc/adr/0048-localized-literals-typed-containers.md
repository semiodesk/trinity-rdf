# 0048. Localized literals are typed containers, not ambient resource state

Date: 2026-09-17

## Status

Proposed (2.0). The design below is agreed; implementation follows. On landing, this supersedes
[0027](0027-localized-literals.md), whose status becomes *Superseded by 0048*.

## Context

RDF literals carry language tags (`"Hallo"@de`). [0027](0027-localized-literals.md) recorded how
Trinity handles them and marked itself *"Accepted — rudimentary; flagged for improvement"*, with a
Revival note asking for *"a proper localized-literal type and a consistent read/write API"*. The
same request has been sitting in the code since 2016 as `// TODO: Write a custom string class with
an associated language` (`Trinity/Resources/Resource.cs:552`). This ADR answers it.

### Language is a mode switch on the object

`Resource.Language` is a settable `string` whose setter calls `ReloadLocalizedMappings()`
(`Resource.cs:1381`). That method does not filter a view — it **moves values**. For every
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
| `Tuple<string,string>` | untyped writes (`Resource.cs:554,568`); dotNetRDF read (`dotNetRDFQueryResult.cs:372`); Virtuoso read (`Trinity.Virtuoso/VirtuosoSparqlQueryResult.cs:120`) |
| bare `string` + out-of-band `IPropertyMapping.Language` | inside a mapping, while a language is active |
| `Tuple<string,CultureInfo>` | serializer key (`XsdTypeMapper.cs:140,351`) — and it **drops the culture** |
| `string[] { value, lang }` | `XsdTypeMapper.DeserializeXmlNode:743` — appears unreferenced |

`SparqlSerializer.SerializeValue` branches over three of them, each with the identical comment
`// string + language`. [0027](0027-localized-literals.md) named two of the four; the third and
fourth were found while writing this ADR.

### The defects this causes

Every one of these is a consequence of there being no type whose job is to know what a
language-tagged literal *is*, so each call site re-derives it and they disagree.

| # | Where | Defect |
|---|---|---|
| 1 | `Resource.cs:1113,1128` | **`ListValues(Property)` double-wraps.** `GetValueObject()` has already tagged the value, so `x as string` is `null` and `ListValues(Property)`/`GetValue(Property)` return `Tuple<null, lang>`. Invisible because the tests assert only `.Count()`. |
| 2 | `dotNetRDFQueryResult.cs:372` vs `Resource.cs:258` | **Tag-case churn.** Reads preserve the server's casing; writes lower-case it. Against [0039](0039-resource-write-semantics.md)'s ordinal delta a value read as `@en-US` and committed after `Language = "en-US"` emits a spurious `DELETE '…'@en-US` + `INSERT '…'@en-us`, and flips `HasUnsavedChanges()`. |
| 3 | `SparqlQueryProvider.cs:180,287` | **LINQ projection throws.** `Convert.ChangeType` on a `Tuple<string,string>` raises `InvalidCastException`, so `Select(p => p.Name)` fails outright if any value is tagged. The adjacent `Uri` → `UriRef` special case exists for precisely this reason. |
| 4 | `Resource.cs:990` | `HasProperty(p, v, string lang)` omits the `.ToLower()` that `AddProperty` applies, and lookup is an ordinal tuple match — so `HasProperty(p,"x","DE")` is **false** after `AddProperty(p,"x","DE")`. |
| 5 | `Resource.cs:1402,1413` | `_properties[key].Remove(v)` leaves **empty `HashSet` entries**, breaking the invariant `HasProperty(Property)` documents at `:904-907`. The bare indexer also throws `KeyNotFoundException` when two mappings share a predicate. |
| 6 | `Resource.cs:311-323` | The copy constructor drops `_language` but shares `_mappings` **by reference**, so the copy's two language views disagree. |
| 7 | `PropertyMapping.cs:510` | `&&`/`\|\|` precedence makes the type guard a tautology; the condition reduces to `LanguageInvariant \|\| IsNullOrEmpty(Language)`. Since `Language` has a **public setter**, setting it on a non-string mapping yields `Tuple(null, lang)` or an NRE in `ToLanguageList`. |
| 8 | `SparqlQueryTranslator.cs:2521` | **LINQ cannot express a language at all** — all five `LiteralTerm` sites pass `null`. Worse, it is *inconsistent with itself*: `==` emits a plain literal and never matches `"Hallo"@de`, while `Contains` emits `CONTAINS(?v,"…")`, which SPARQL argument compatibility (§17.4.3) **does** match against tagged values. The provider silently disagrees about whether tagged data exists. |
| 9 | `Resource.cs:1409` | Matching is an exact case-insensitive compare — **no BCP-47 fallback**. `de` never matches `de-DE`. |

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
guard (`Resource.cs:467`) could never fire. Nullable reference types are off repo-wide, so
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

`LocalizedStringCollection` keeps the same surface but its indexer returns **every** value for a tag
— `this[string language] => IReadOnlyList<string>` — and it adds `Add(language, value)`,
`Set(language, params string[])`, `Remove(language, value)`, `AddInvariant`/`RemoveInvariant`, and an
`Invariant` that is a list. Both implement `ILocalizedText`, which carries everything independent of
multiplicity (`Languages`, `Best`, `TryGetBest`, `Contains`, `Remove`, `Count`, `IsEmpty`), so the
RFC 4647 rules exist once — a second copy of the matching logic is precisely how the four
representations this ADR replaces came to disagree.

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

- **The tag is attached where the variable is bound, and it is part of the binding's cache key.**
  This ADR first specified the constraint at *comparison* time, with the binding keyed by predicate
  path alone. Both halves of that were wrong, and neither failed loudly:

  - keying by path alone gave `Label["de"]` and `Label["en"]` in one predicate *the same* variable, so
    the two constraints met on it as `LANG(?v) = "de" && LANG(?v) = "en"` — unsatisfiable, so a query
    with an obvious answer returned nothing;
  - constraining at comparison time left every other consumer of the variable — `StartsWith`,
    `Contains`, `IN`, `ORDER BY`, a comparison against another member — reading it bound to every
    language of the property at once.

  `BindChain` therefore takes the tag, includes it in the key (as `BindCount` already did for its own)
  and emits the constraint when it creates the binding; consumers pass the whole `ChainInfo` through an
  overload, so dropping the tag on the way is not expressible rather than merely discouraged. The
  constraint moves into the `OPTIONAL` group when a binding is upgraded, or it would discard the rows
  the `OPTIONAL` exists to keep.

- The comparison itself is `STR(?v) = "Hallo"`, **not** `FILTER (?v = "Hallo"@de)`: measured on
  dotNetRDF 3.5.2, a language-tagged literal inside a `FILTER` comparison matches *regardless of its
  tag* — `"Bericht"@fr` matched a value tagged `@de` — while the same literal in a triple pattern
  matches correctly, and `STR`/`LANG` both evaluate correctly. It gives up the index-friendliness term
  equality would have had; binding the term in the triple pattern instead would recover it, and is the
  obvious follow-up if it ever shows up in a profile. The tag must be a **constant** — it becomes part
  of the query text — so a closure is folded to one by the partial evaluator and a per-row value is
  refused.

- The tag test is `LCASE(LANG(?v)) = "de"`, lower-cased on **both** sides. Stores do not agree on how
  they hand a tag back: Jena canonicalizes `de-de` to `de-DE`, RDF4J returns it as written. Comparing
  a lower-cased constant against a raw `LANG()` therefore matches on some backends and not others —
  and bare `de`/`en` test tags, which is what the first round of tests used, never show it.

- `ToTerm` **refuses** a `LangString` rather than emitting `"Hallo"@de`. Every one of its callers puts
  the result in a filter expression, where that form is the tag-blind one measured above; this ADR
  originally had it populate `LiteralTerm.Language` there, which reintroduced the defect it had just
  documented. A tagged constant is split into `STR`/`LANG` against the bound variable instead, and an
  `IN` list of tagged literals expands into a disjunction of those comparisons for the same reason —
  SPARQL's `IN` is a chain of `=`.

- A mapped `string` binds with `LANG(?v) = ""`, which is what finally makes *"a mapped `string` means
  untagged"* true in LINQ rather than only when materializing. Without it the claim held for `==`
  alone — a plain literal term matches only a plain literal — and failed for string functions, where
  SPARQL argument compatibility makes `STRSTARTS("Markiert"@de, "Mark")` true, and for `Select`, which
  returned tagged values unwrapped into strings. Measured before keeping, since it lands on every
  mapped-string binding: over 20k documents the delta is below the run-to-run noise floor, and the
  in-memory engine's variance on an untouched query exceeded any difference attributable to it.
- Query-marker extension methods — `HasLanguage(range)` → `langMatches(lang(?v), "…")`, `IsPlain()` →
  `lang(?v) = ""`, `LanguageTag()` → `lang(?v)`, `Lexical()` → `STR(?v)` — need no new AST node,
  because `SparqlFunctionExpression` takes an arbitrary name. **Not built.** Indexing by language
  covers the cases that motivated this ADR, and the helpers are worth adding when something actually
  needs them rather than on speculation.
- Defect 3 is fixed by the binding constraint, not by a marshalling case. Adding a `LangString` →
  `string` case to `CoerceValue` was the first attempt: it removed the `InvalidCastException` and
  replaced it with a quieter bug, since the projection then returned tagged values as though they
  were untagged — the very leak that projecting a single language is refused for. `CoerceValue` now
  **throws** on that combination instead, as a diagnostic for a constraint that failed to apply.
- **`Best(...)`/`TryGetBest(...)` inside a query throw `NotSupportedException`.** RFC 4647 Lookup is a
  client-side fallback walk with no faithful SPARQL; quietly translating it to `langMatches` would
  return the wrong rows. Refusing loudly is the posture [0041](0041-layered-read-views.md)
  established. **Projecting a single language** — `Select(d => d.Label["de"])` — is refused for the
  same reason: the bound variable carries every language of the property, so projecting it would
  silently return the wrong rows. Filter on the language in `Where` and project the resource.
  **`.Count`/`.Count()`/`.Any()` on a container** is refused on the same grounds: `LocalizedString`
  counts *languages* and `LocalizedStringCollection` counts *values*, so no single count of matching
  triples is right for both — and the one that was emitted also counted untagged literals, which
  neither container holds.
- `ORDER BY` on a localized member should emit `ORDER BY STR(?v)`. **Not built**, for the same reason
  as the helpers. SPARQL 1.1 §15.1 leaves the relative
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
to dodge the reflection-by-name handles in `Model.cs:109`, `ModelGroup.cs:116` and
`LayeredModel.cs:184`. Revisit only if a genuinely *filtered fetch* is ever wanted.

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
`DESCRIBE` (`StoreBase:702` and the four backends), which has nowhere to hang a `FILTER`; a filter
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
- **`LocalizedString` against multi-valued data is lossy, deliberately — but the value is orphaned,
  not deleted.** If the store holds two `@de` labels for a `LocalizedString` property, the mapped
  surface shows one. An earlier draft of this ADR claimed the other was then deleted on the next
  `Commit()`, reasoning that the delta computes removals from what the resource currently holds.
  That is wrong, and measured: the snapshot `CapturePersistedValues` takes is built from
  `ListValues()`, which is the resource *after* the container dropped the duplicate, so the dropped
  value is in neither side of the delta. `HasUnsavedChanges()` returns `false` and `Commit()` emits
  nothing for it.

  So the value **survives in the store, permanently invisible through that property** — no mapped
  read will show it and no commit will ever remove it. That is the safer of the two behaviours and
  the more confusing one, so it is worth stating plainly rather than leaving as a footnote.

  The same correction applies to the analogy: a scalar `string` over a multi-valued predicate
  behaves identically — both values stay in the store, the mapping shows one. Measured alongside.
  `LocalizedStringCollection` is still the escape hatch, as `List<string>` is today, and it is now
  the escape hatch from *hidden* data rather than from *destroyed* data.
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
