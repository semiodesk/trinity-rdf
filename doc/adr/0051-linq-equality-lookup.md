# 0051. A LINQ equality on a mapped string looks the resource up by value

Date: 2026-10-01

## Status
Accepted

## Context
`AsQueryable<T>().Where(p => p.FirstName == name)` binds the member to a variable and compares it in a
`FILTER`, as `LANG(?v) = ""` and `STR(?v) = "…"` (ADR-0048, #52). Every row of the property has to be
read to evaluate that filter, so the lookup's cost grew with the model (#64). At 100k resources, per
lookup: Virtuoso 30 ms, in memory 725 ms, Oxigraph 150 ms, against 0.2–1.3 ms for the same lookup with
the literal bound in the pattern. Called once per resource, a lookup like that makes a loop quadratic.

#64 asked for a fix that keeps the filter's answers, and proposed
`VALUES ?v { "x" "x"^^xsd:string }`. Measuring that proposal and its alternatives on all five backends
showed that no single form is right everywhere, for two reasons nobody had written down:

- **The four RDF 1.1 stores** (in-memory, Oxigraph, Fuseki, GraphDB) treat `"x"` and
  `"x"^^xsd:string` as one term. Anything naming both spellings matches the same triple twice and
  returns every row twice, and a duplicated row is a doubled value in a mapped collection.
- **Virtuoso stores an untagged string in one of two forms, depending on how it was written.**
  `Commit()`, `INSERT DATA` and an updating `Read` store a term only the plain constant matches. A
  replacing `Read` (the vendored manager's `SaveGraph`) stores one only the `xsd:string` constant
  matches, even when the Turtle wrote it plain. A one-row `VALUES` matched neither form there. A
  test seeded only through Turtle sees just one of the two, which is how the first version of this
  fix passed the Virtuoso suite and returned nothing in the benchmark.

Measured at 100k resources, with data in all three Virtuoso spellings (time per lookup; ✗ = wrong
answer):

| Shape | Virtuoso | in memory | Oxigraph | Fuseki | GraphDB |
|---|---|---|---|---|---|
| `FILTER` alone (before) | 28.5 ms | 746 ms | 214 ms | 94 ms | 130 ms |
| `?s p "x"` | ✗ misses half | 0.7 ms | 2.4 ms | 8.9 ms | 3.7 ms |
| `VALUES ?v { "x"^^xsd:string }` | ✗ none | 0.5 ms | 0.6 ms | 6.3 ms | 3.2 ms |
| `VALUES ?v { "x" "x"^^xsd:string }` | 1.0 ms | ✗ dup | ✗ dup | ✗ dup | ✗ dup |
| the same, guarded against duplicates | 65–148 ms | 1.0–1.4 ms | 0.6–181 ms | 6–68 ms | 5–7 ms |
| `UNION` + `FILTER NOT EXISTS` | 2.1 ms | 0.7 ms | 217 ms | 9.4 ms | 5.1 ms |
| **`DISTINCT` sub-select over a `UNION`** | **1.6 ms** | **0.7 ms** | **153 ms** | **7.1 ms** | **5.1 ms** |

## Decision
For every `member == "constant"` that a root predicate requires (reached through `&&` only, not under
`||` or `!`), on a direct mapped `string` member of the selected resource, the translator adds in
front of the selection:

```sparql
{ SELECT DISTINCT ?s WHERE { { ?s <p> "x" . } UNION { ?s <p> "x"^^xsd:string . } } }
```

The member's binding and both filters stay exactly as they were. Both spellings make Virtuoso right;
`DISTINCT ?s` keeps the RDF 1.1 stores from doubling every row. Projecting only `?s` leaves the
member's variable alone, so the lookup holds however that variable ends up bound, an `OPTIONAL`
introduced by a disjunct included: the conjunct is required of every row, so one of these triples is
too. It leads the group, ahead of the type constraints, because an engine that joins in written order
would otherwise start from every resource of the type. The type constraints bind `?s` too, so the
position does not change the answer, and the lookup is added only when there are type constraints.

`Where`, `Any`, `Count`, `First`, `Last` and `Single` all take their predicate through one
`AddRootPredicate`, so every one of them gets the lookup.

## Consequences
- Flat on four backends. Per lookup at 1k → 100k: Virtuoso 2.5 → 2.5 ms (was 1.6 → 30 ms), in memory
  1.5 → 1.0 ms (was ~725 ms at 100k). Fuseki and GraphDB already optimized the filter and gain little.
- **Oxigraph is not fixed.** It pushes a binding into neither a sub-select nor a `UNION`, and every
  shape it does answer from an index was wrong or slow on another backend. It stays at ~150 ms per
  lookup at 100k, no worse than before. A fix there needs store-specific SPARQL, which the translator
  does not produce; #64 stays open for it.
- **Small Virtuoso models pay about 1 ms per lookup** for the sub-select (1.6 → 2.5 ms at 1k). The
  break-even is near 2k resources.
- **The answer narrows in one way.** The filter alone matched an untagged literal of any datatype
  with the right lexical form, `"5"^^xsd:int` for `== "5"`. The lookup matches only the plain and
  `xsd:string` spellings, which are the ones a mapped `string` reads; a mapped `string` does not
  convert an `xsd:int`, so such a row came back with the property unset anyway.
- Not covered: chained members (`p.Group.Name == "x"`), localized indexers, value types, and anything
  inside `||`, `!` or a nested scope. Those keep the filter alone.
- `Any(predicate)` made the first `ASK` with a sub-select Trinity ever generated, and the SPARQL
  preprocessor got two things wrong about that shape. It took the query form from the sub-select's
  `SELECT`, and it put the model's `FROM` before the sub-select's `WHERE`. Both are fixed for every
  caller, not only LINQ.

## Related
- ADR-0037 (the LINQ provider), ADR-0048 (the language constraint the filter carries), ADR-0050 (the
  `PointReadBenchmarks` that measure this, and whose answer check caught the first version)
- #64, #52
- `Trinity/Query/Sparql/SparqlQueryTranslator.cs` (`RecordEqualityLookups`, `LookupPattern`)
- `Trinity.Tests/Query/Sparql/LinqEqualityLookupTest.cs` (the shape),
  `ResourceMappingTest.QueriesAMappedStringByValueAcrossStores` (the answers, on every store)
