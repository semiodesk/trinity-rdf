# 0046. Bulk subject binding uses VALUES, never an equality chain

Date: 2026-09-22

## Status
Accepted (2.0).

## Context

`IModel.GetResources(IEnumerable<Uri>, Type, ITransaction)` is the bulk lazy-load query. It is the
sole production caller of the mapped-property dereference path (`ResourceCache.LoadCachedValues`),
which means it sits under **every** read of a mapped collection or resource reference — and under
every write of one too, because `Add`/`Remove`/`Link` read the collection before mutating it.

Three implementations of that one overload existed. Two of them, `Model` and `ModelGroup`, built the
subject constraint as a chain of equalities:

```sparql
SELECT ?s ?p ?o WHERE { ?s ?p ?o. FILTER(?s = <a>||?s = <b>||…) }
```

A consumer on `2.0.0-rc.3` reached a mapped `List<T>` of 158 elements and every read and write of it
failed against their Virtuoso with:

```
SP031: The nesting depth of subexpressions exceed limits of SPARQL compiler
```

Virtuoso parses `a || b || c || …` as nested binary pairs, and its SPARQL compiler caps how deep it
will walk that tree. The third implementation, `LayeredModel` (ADR-0041), already used `VALUES` and
was unaffected — the divergence is what let the defect survive.

### Measured, through the real store path

Against `openlink/virtuoso-opensource-7`, seeded with 2000 subjects, issued through
`VirtuosoStore.ExecuteQuery` (the ADO path a consumer actually hits), not the HTTP endpoint:

| subjects | equality chain | `VALUES` |
|---|---|---|
| 157 | OK, 33 ms | OK, 13 ms |
| 158 | OK, 34 ms | OK, 10 ms |
| 300 | OK, 132 ms | OK, 28 ms |
| 1000 | OK, 957 ms | OK, 83 ms |
| 2000 | **SP031** | OK, 170 ms |

Bisected thresholds, identical on **7.2.12 and 7.2.14**:

| shape | last size that compiles | failure past it |
|---|---|---|
| equality chain | **1024** | `SP031: SPARQL: Internal error: sparp_gp_trav_int(): stack overflow` |
| `VALUES` | **4094** | `SP030: Too many arguments for standard built-in function` |

Two things this measurement settled that the bug report could not:

- **The threshold is a property of the server build, not a universal constant.** The reported 157/158
  did not reproduce here; on both these builds the chain compiles 1024 subjects. It is also **not**
  a tunable: setting `ThreadStackSize = 60000` moved it not at all, despite the error naming a stack
  overflow. So there is no "safe" chunk size for the chain that can be justified — only removing the
  shape is defensible.
- **The `VALUES` ceiling is an argument count, not a query-text length.** 4094 subjects is 172 KB of
  query text and compiles; 4095 does not, and the error names built-in function arguments. Chunking
  therefore has to count subjects, and no amount of shortening the IRIs would buy headroom.

Independently, ADR-0041 had already measured the planner cost of the `FILTER` form at **15x**
(in-memory) and **38x** (GraphDB) against `VALUES`, because the filter gets pushed into a `UNION`
containing an anti-join. The same shape was therefore both slower everywhere and unusable past a
threshold on Virtuoso.

## Decision

**Subjects are bound with `VALUES`, emitted before the pattern they constrain, by one shared helper.**

```sparql
SELECT ?s ?p ?o WHERE { VALUES ?s { <a> <b> … } ?s ?p ?o. }
```

`LayeredModelSparql.BindSubjects` was promoted to `SparqlSerializer.GenerateSubjectBinding` and the
original deleted — **no forwarder**, because a forwarder is how a fourth copy starts. `Model`,
`ModelGroup` and `LayeredModel` now share it.

**Why not just raise the chunk size of the chain.** Because the chain's limit is a compile-time
constant of the server build that this code cannot see, and a consumer already observed it at 157
where these builds sit at 1024. Any chunk size picked here would be a guess about someone else's
server. `VALUES` moves the constraint from the expression tree, where the limit is, into the data
block, where it is 4x larger and better characterised.

### The projection shape is load-bearing, not cosmetic

`ISparqlQuery.ProvidesStatements()` is a token-level heuristic (`SparqlQueryPreprocessor`), not a
parse: it latches only when a pattern terminator is reached while exactly three global-scope
variables are in scope **in the projection's order**. If it returns false, resource materialization
refuses the query outright with `ArgumentException: The given query cannot be resolved into
statements.` — a failure that does not look like a query problem at all.

The emitted shape survives it, for reasons worth writing down:

- `VALUES ?s` reuses `?s`, and `GlobalScopeVariables` is deduplicated, so the count stays 3.
- The `VALUES` block's own `{`/`}` clear the in-scope accumulator, so its leading `?s` cannot
  pollute the comparison against the projection.
- The **trailing `.`** after `?s ?p ?o` is required. At the closing `}` of the `WHERE`,
  `_parseVariables &= _nestingLevel > 0` turns the comparison off *before* it runs, so a pattern
  with no terminator never latches. `SparqlSerializerTest.DroppingTheTrailingDotWouldBreakMaterialization`
  pins this in both directions.

### Batching at 1000 subjects per query

`VALUES` is not unbounded, so `GenerateSubjectBindings` splits the subjects into blocks of 1000 and
the caller issues one query per block. 1000 keeps a 4x margin under the measured 4094 — margin that
matters precisely because the ceiling belongs to the server build rather than to this code — and it
is where the advantage has already saturated (83 ms at 1000, against the chain's 957 ms).

Partitioning **by subject** is what makes this safe without merge logic: every triple of a given
resource stays inside one batch, so the results concatenate.

### Blank nodes are skipped, not serialized

There is no SPARQL query shape that addresses a blank node by label. A label is not a legal
`DataBlockValue` (`iri | RDFLiteral | NumericLiteral | BooleanLiteral | 'UNDEF'`), it is not legal in
a `FILTER` expression either, and where it *is* legal it means a fresh existential variable rather
than a reference. `LayeredModel` already said so in code, rejecting such a subject outright.

The old code interpolated it as `<_:0>` — an invalid *relative IRI reference* — which took the whole
query down, including every addressable subject in it. That was the recorded cause of the
long-quarantined `ResourceWriteSemanticsTest.CanRemoveBlankNodeValuedLink`
(`RdfParseException: "Cannot resolve a Relative URI Reference since there is no in-scope Base URI"`).

`GenerateSubjectBindings` skips them. `ResourceCache.LoadCachedValues` then materializes whatever did
not come back as an unresolved resource, so a blank-node-valued link still appears in its mapped
collection, with its identity, without its properties.

**This fixed the read half of that quarantined test and not the write half**, exactly as
`doc/known-test-failures.md` predicted it would. The read is now covered by
`ResourceWriteSemanticsTest.CanReadBlankNodeValuedLink`; the removal still fails, one layer further
in, with `SparqlUpdateException: "Cannot create a DELETE command where any of the Triple Patterns
are not constructable triple patterns (Blank Node Variables are not permitted)"`. That is a
write-semantics defect (ADR-0039) and stays quarantined under its own, now accurate, reason.

### Three adjacent defects in the same two methods

Fixed here because they live in the code being replaced:

| Where | Defect |
|---|---|
| `Model.cs` | Empty or null `uris` skipped the `FILTER` entirely, leaving `SELECT ?s ?p ?o WHERE { ?s ?p ?o. }` — **the whole model**, materialized as resources |
| `ModelGroup.cs` | No guard at all: empty `uris` emitted `FILTER ( )` (a syntax error), null threw `NullReferenceException` out of `string.Join` |
| both | `$"?s = <{s}>"` interpolates `Uri.ToString()`, which **unescapes percent-encoding**. `SparqlSerializer.SerializeUri` uses `OriginalString` deliberately. See below for what this actually cost |
| `Model.cs` | `uris` was enumerated twice (`.Count()`, then the projection) |
| `Model.cs` | A **dangling `if`**: `if (type.IsAssignableFrom(r.GetType()))` guarded only `r.IsNew = false;`, while `IsSynchronized` and `SetModel` ran unconditionally |
| `Model.cs` | No null guard, where `ModelGroup` and `LayeredModel.Materialize` both skip the nulls a missing `rdf:type` produces |

The dangling `if` was **removed rather than braced**, on evidence rather than taste: git shows it was
born unbraced in `64de7f2` (2020) when the method called the *untyped* `result.GetResources()`, and
was made vestigial two days later in `5b162fa` when that became `result.GetResources(type)`.
`MappingDiscovery.GetMatchingTypes` filters candidates with `type.IsAssignableFrom(...)` and the
fallback is `Activator.CreateInstance(type, uri)`, so the condition is now unconditionally true.

### What `Uri.ToString()` actually cost, measured

`Uri.ToString()` returns the *display* form, which unescapes percent-encoding; `OriginalString` and
`AbsoluteUri` do not. Swept across encodings against the in-memory store, comparing the old
interpolation with the new serialization:

| encoding | `ToString()` yields | old query | new query |
|---|---|---|---|
| `%20` | `a b` | **`RdfParseException`** | correct |
| `%3E` | `a>b` | **`RdfParseException`** | correct |
| `%22` `%3C` `%7B` `%7D` `%7C` `%5E` | `a"b`, `a<b`, … | correct | correct |
| `%23` `%2F` `%3F` `%5C` | left escaped | correct | correct |
| `%C3%A9` | `aéb` | correct | correct |

So the cost is **narrower and sharper** than "a resource is silently not found": it bites only where
the unescaped character is one SPARQL forbids inside an `IRIREF`, and then it is a **parse error that
takes the whole query down** — every other subject in the same batch with it. A space is the case a
consumer would plausibly hit. dotNetRDF re-normalizes the rest, so those worked by luck, and
`.NET` leaves reserved delimiters like `%23` escaped, so a `%23` could never have been misread as a
fragment separator.

Because the cost is a parse error rather than a silent miss, the codebase was audited for the same
shape — any `Uri` reaching SPARQL text without going through `SerializeUri`. **Three more sites had
it**, each now fixed and each guarded by a test that was verified to fail when the fix is reverted:

| Site | What it emits | Reach |
|---|---|---|
| `Model.GetResources<T>(bool, ITransaction)` | `?s a <{type.Uri}>` from `[RdfClass]` | every type-constrained read |
| `SparqlPreprocessor.AddPrefix` | `{prefix}: <{uri}>` from a registered namespace | **every query that declares a prefix** (0024) |
| `SparqlSerializer.SerializeTypedLiteral` | `'{v}'^^<{typeUri}>` | every typed literal written |

Confirmed clean, and worth naming so the audit is not repeated: `SparqlQuery.Bind` routes through
`SerializeValue` → `SerializeUri`; the LINQ writer (`SparqlQueryWriter`) uses `OriginalString`
directly; `StoreBase` and the Virtuoso update paths use `OriginalString`; and Virtuoso's
`UnmarshalUri` uses `AbsoluteUri`, which — unlike `ToString()` — preserves escaping.

**This is not the .NET 10 `Uri` problem of [0025](0025-resource-identity-uriref-blanknodes.md)**, and
the distinction matters when diagnosing: that one is `EqualityComparer<T>.Default` preferring the
`IEquatable<Uri>` that .NET 10 added, which bypasses `UriRef.Equals` and makes *identity* in a
`HashSet`/`Dictionary` fragment-blind. This one is `ToString()` on the *serialization* path, it is
identical on .NET 8 and .NET 10, and it has been there since 2020. They are the same *family* —
`System.Uri` applying web-URI equivalences (escaping, fragments) that RDF treats as significant;
`Uri("a%20b").Equals(Uri("a b"))` is `true` on every runtime — but they are different members,
different symptoms and different fixes.

### The subjects are materialized once, and that is a correctness requirement

`ResourceCache.LoadCachedValues` passes its **live** `HashSet<UriRef>` as `uris` and removes from it
while consuming the result. That was safe only by accident — the old iterator drained `uris` on the
first `MoveNext()`, before the first `yield`. Batching re-enumerates, so the set is now copied in an
eager wrapper before any query runs. Without that, batch 2 would throw
`InvalidOperationException: Collection was modified`.

## Consequences

- The ceiling on a mapped collection goes from a server-dependent threshold — 1024 on these builds,
  157 on the consumer's — to **no practical ceiling**, since batching is transparent.
- Reads get faster at every size, not just large ones: 28 ms against 132 ms at 300 subjects, 83 ms
  against 957 ms at 1000.
- Argument validation is now **eager** rather than deferred to the first `MoveNext()`, matching
  `LayeredModel`. A caller that built the enumerable and never enumerated it would previously not
  have seen the `ArgumentException` for a non-`IResource` type.
- Test counts: in-memory `758 / 0 / 3 = 761`, Virtuoso `314 / 0 / 1 = 315` (was `303 / 0 / 1`),
  GraphDB `325 / 0 / 1`, Fuseki `327 / 0 / 1`.

### What the guard actually is

A 300-member round-trip **does not catch this bug** on a stock Virtuoso, and was verified not to:
with the fix reverted on purpose, the 300-member test still passed and only the 2000-subject test
failed, with `SP031`. The guard is therefore
`GetResourcesByUriCompilesBeyondTheEqualityChainLimit`, which asks for 2000 subjects that need not
exist — the failure was in *compiling* the query, not in answering it, so the guard costs one query
and no writes.

dotNetRDF has no nesting limit, so the in-memory run of any of these proves only that the round-trip
is correct. **The Virtuoso suite is the guard**, which is the same lesson as ADR-0043: a defect the
other backends hide is found by covering one more.

## Related
- [0023](0023-lazy-loading-via-resourcecache.md) — the lazy-load path this query serves
- [0025](0025-resource-identity-uriref-blanknodes.md) — blank-node identity, and why a label is not a reference
- [0039](0039-resource-write-semantics.md) — the delta write whose blank-node half stays quarantined
- [0041](0041-layered-read-views.md) — where `VALUES` was already the rule, and the 15x / 38x planner numbers
- [0043](0043-fuseki-store-revival.md) — covering one more backend is what finds these
