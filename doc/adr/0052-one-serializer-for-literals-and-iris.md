# 0052. Literals and IRIs reach SPARQL and SQL text through one serializer each

Date: 2026-10-02

## Status
Accepted

## Context
Trinity builds SPARQL and, on Virtuoso, SQL as text, and interpolates values into it. Three kinds of
value are interpolated: string literals, IRIs, and the lexical forms of typed and language-tagged
literals. ADR-0046 settled the IRI rule — written verbatim from `OriginalString` or refused — but only
for the sites it audited. The literal rule had never been written down, and the function that wrote a
string literal was wrong.

`SparqlSerializer.SerializeString` wrote any value containing a newline in the long form `'''…'''` and
escaped no quotes in it; otherwise it wrote `'…'`, escaping `\` and `'` but not a carriage return.
Measured through a mapped `string` property committed to a model, on the in-memory store and the four
servers:

| Value set on a mapped `string`, then `Commit()` | in memory | Fuseki | Virtuoso |
|---|---|---|---|
| contains a newline and `'''` | adds a triple it never named | same | same |
| …built to close the template and add an operation | writes into another graph | same | didn't take |
| several lines, ending in `'` | accepted (lenient parser) | HTTP 400 | silently writes nothing (#50) |
| a lone carriage return | throws | throws | ok |

The value had ended its literal early, and the rest of it was read as part of the update. The function
is unchanged since 2016 (`b48b0e5`); before that the preprocessor wrapped values in raw `'''` itself.

**It reached every literal**, because `SparqlPreprocessor` tokenizes every query and update and writes
each literal back out through `SerializeString(token.Value)`. There were in fact three copies of the
escaping — this one, `SparqlQueryWriter.Escape` (the LINQ writer) and `LangString.ToNTriples` — and the
two that were correct made no difference: the preprocessor re-wrote their output through the broken one
before any store saw it. The LINQ writer's double quotes never reached a backend.

Two adjacent things were wrong for the same reason, a value written into text without being held to
the grammar:

- `SerializeTypedLiteral` placed `XsdTypeMapper.SerializeObject`'s output between quotes unescaped.
  That was harmless for the lexical forms Trinity's own writes produce — numbers, dates, base64, whose
  forms hold no quote — but `XsdTypeMapper.SerializeString` returned a string wrapped in quotes of its
  own, so a string passed to the public `SerializeTypedLiteral` came out as `'"abc"'^^<…>`: the stored
  lexical form held the quotes, and an apostrophe ended the literal.
- A language tag was appended as given. Inside Trinity every tag is validated and lower-cased by
  `LangString` first, but the public `SerializeTranslatedString` and the LINQ writer took one as given.
  A tag has no quoted form in the grammar, so it cannot be escaped — it has to be validated.

And IRIs were interpolated raw in many places ADR-0046 had not reached: `StoreBase`'s delta commit
(the path every backend's `Commit()` takes) and its insert and replace, the preprocessor's `AddGraph`
(the path every model read takes) and its graph parameters and re-emitted IRI tokens, the LINQ writer's
IRI terms, the layered-view expression writer's extension-function IRIs, and Virtuoso's own `WITH`,
`FROM` and blank-id-lookup statements. A `>` in a graph or resource IRI ended it and the rest became
query text. `SerializeUri` also wrote any identifier beginning `_:` as given — a blank node label is
written bare, so it is the one identifier with no delimiter keeping what follows out of the query.

On Virtuoso the manager built SQL by placing the graph IRI between quotes:
`DELETE FROM DB.DBA.RDF_QUAD WHERE G = DB.DBA.RDF_MAKE_IID_OF_QNAME('<iri>')`. An apostrophe is legal in
an IRI and the IRI guard cannot refuse one, so a graph named `http://a/x')OR('1'='1` made the condition
true for every quad: replacing that graph deleted the data of another graph. Measured.

Finally, a parameter bound after `LIMIT` or `OFFSET` was written with `value.ToString()` whatever its
type, so a string bound there went into the query as text.

## Decision
**One serializer owns each kind of value, and every writer calls it.**

1. **String literals** — `SparqlSerializer.SerializeString`. Always the short double-quoted form,
   `STRING_LITERAL2`, escaping `\`, `"`, LF, CR and tab. Double quotes because N-Triples requires them,
   so `LangString.ToNTriples` uses the same function. Two escapes are never written: `\uXXXX`, because
   SPARQL 1.1 §19.2 decodes it before parsing so `"` would arrive as a bare quote; and `\'`, which
   needs no escape inside double quotes and which dotNetRDF's tokenizer refuses there. The long form is
   gone. `SparqlQueryWriter.WriteLiteral` and `LangString.ToNTriples` call this; `Escape` and the
   `ToNTriples` loop were deleted.

2. **Typed literals** — `SerializeTypedLiteral` is `SerializeString(lexical) + "^^" + SerializeIriRef(dt)`,
   and `XsdTypeMapper.SerializeString` returns the plain lexical form, like every other serializer
   there. `SerializeDateTime` routes through it.

3. **Language tags** — validated, not escaped, by `LangString.NormalizeLanguage` (the one rule every
   tag passes), which also lower-cases. A public caller's tag is refused if it is not a tag.

4. **IRIs** — `SerializeIriRef` wherever the grammar requires an `IRIREF` (`GRAPH`, `WITH`, `FROM`,
   `INTO`, datatypes, dataset clauses, LINQ terms); `SerializeUri` only where a blank node label is
   legal. It is public now, because the store adapters are separate assemblies. `RequireWritableIri`
   gained a string overload, for IRI text the preprocessor writes back after dotNetRDF has decoded its
   `\u` escapes. `SerializeUri` now holds a `_:` label to the characters of `BLANK_NODE_LABEL`, none of
   which can end it.

5. **SQL** — parameterized, as the TTLP calls already were. A validated IRI can still contain `'`, so
   SQL that names one cannot be made safe by the IRI guard alone.

6. **Bound parameters are typed.** A `LIMIT`/`OFFSET` parameter takes a non-negative integer and
   refuses anything else, including a numeric string. A `FROM` parameter takes a graph identifier.

**A refusal, not a rewrite.** A value or identifier that cannot be written is refused, naming itself,
as ADR-0046 decided for IRIs: a rewrite would change what the caller asked for, and silence is worse
than a loud refusal at the call.

## Consequences
- The output form of every literal changed from `'…'` to `"…"`, which is visible in any query's
  `ToString()`. Four tests pinned the old quoting and were updated; `XsdTypeMapper.SerializeObject` of
  a string now returns the plain value; `SerializeTranslatedString` lower-cases or refuses a tag; `Bind`
  refuses a non-integer `LIMIT`/`OFFSET`; `SerializeIriRef` is public; a LINQ comparison against a blank
  resource throws (it already refused one, so only the message changed).
- Covering a strict backend was necessary. The Virtuoso SQL defect is invisible to a lenient store, and
  the carriage-return fidelity question below showed only on a backend asked for XML results.
- **Oxigraph returned a stored carriage return as a line feed** from every `SELECT`. It became reachable
  only once a value could hold a lone CR (this change), and it was Oxigraph being asked for SPARQL XML
  results first — an XML parser normalizes a raw CR to LF (XML 1.0 §2.11), while the stored value was
  exact. Its connector now asks for JSON results first.
- The duplication share fell: two of the three escape copies are gone.

### What the guards actually are
- **For literals, a spec-derived oracle, not dotNetRDF.** dotNetRDF is the tokenizer whose output the
  preprocessor re-writes and the engine behind the in-memory store, so asking it whether the escaping is
  right only shows the two agree. `SparqlLiteralOracle` decides from the grammar whether text is exactly
  one literal and decodes it. A hostile corpus and a seeded fuzz are checked with it; a store round-trip
  on all five backends breaks the circularity, because Jena, rdf4j, Oxigraph and Virtuoso each parse the
  text themselves and the value returns through JSON, XML or ADO rather than the SPARQL tokenizer.
- **The preprocessor's second (and third) pass is a fixed point.** A literal this serializer writes,
  decoded and re-serialized, reproduces itself — proven across the templates a literal is placed in, for
  `SparqlQuery` and `SparqlUpdate`. This is also the evidence that the tokenizer hands back decoded
  values, which the whole second pass relies on.
- **Two reflection sweeps, so a new call site is covered without anyone remembering it.** One drives
  every public `SparqlSerializer` method that turns a value into text over the corpus and checks each
  with the oracle; a method added later fails it until it is given a check. The other drives every
  `IModel` accessor taking a `Uri` against a model whose graph IRI cannot be written, and requires each
  to refuse before issuing SPARQL. A hand-maintained list of the sites cannot notice the one it omits,
  which is how three copies of the escaping came to exist with only the broken one reaching a store.

### Not in this change
- `SerializeFetchUris` writes `PREFIX <name>` and drops the namespace IRI (a correctness defect, not one
  that lets text through); it needs a prefix table to fix.
- The remaining stale `Compile Remove` entries #60 lists.
- The graph-name spelling by `AbsoluteUri` (#53), which this change guards but does not re-spell.

## Related
- ADR-0046 (the IRI rule this extends — verbatim or refused, `SerializeUri` vs `SerializeIriRef`,
  bind-versus-interpolate), ADR-0048 (the language-tag validation reused), ADR-0047 (the Oxigraph
  backend and its strictness), ADR-0039 (the per-value delta `Commit()` whose templates reuse the
  serialized fragments)
- #75 (the Virtuoso IRI interpolation), #50 (the swallowed Virtuoso refusal), #60 (the dead file),
  #53 (the `AbsoluteUri` spelling)
- `Trinity/Query/SparqlSerializer.cs`, `Trinity/Query/SparqlPreprocessor.cs`,
  `Trinity/Query/Sparql/SparqlQueryWriter.cs`, `Trinity/Query/SparqlExpressionWriter.cs`,
  `Trinity/Types/XsdTypeMapper.cs`, `Trinity/Types/LangString.cs`,
  `Trinity.Virtuoso/VirtuosoStore.cs`, `Trinity.Virtuoso/VirtuosoManager.cs`
- `Trinity.Tests/Query/SparqlLiteralOracle.cs` and `HostileLiterals.cs` (the oracle and corpus),
  `SparqlLiteralSerializationTest.cs` and `SparqlIriSerializationTest.cs` (the unit guards),
  the shared store fixtures `ResourceMappingTest`, `ResourceWriteSemanticsTest`, `SparqlUpdateTest`,
  `SparqlQueryTest` and `LayeredModelStagingTest`, and `BulkResourceQueryShapeTest` (the IModel sweep)
