# 0051. A LINQ equality on a mapped string looks the resource up by value

Date: 2026-10-01

## Status
Accepted

## Context
`AsQueryable<T>().Where(p => p.FirstName == name)` binds the member to a variable and compares it in a
`FILTER`, as `LANG(?v) = ""` and `STR(?v) = "…"` (ADR-0048, #52). Every row of the property has to be
read to evaluate that filter, so the lookup's cost grew with the model (#64): at 100k resources, 28.5 ms
on Virtuoso and 746 ms in memory per lookup (the first row of the table below), against 0.2–1.3 ms for
the same lookup with the literal bound in the pattern. Called once per resource, a lookup like that
makes a loop quadratic.

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

Measured at 100k resources with a probe that sent each shape as raw SPARQL, with data in all three
Virtuoso spellings and a unique value (median time per lookup; ✗ = wrong answer). These are the figures
this ADR quotes unless it says otherwise:

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
- **A lookup of a unique value no longer grows with the model on four backends.** From the table:
  Virtuoso 28.5 → 1.6 ms, in memory 746 → 0.7 ms, Fuseki 94 → 7.1 ms, GraphDB 130 → 5.1 ms. Through the
  mapper (`PointReadBenchmarks.LinqPoint`, which materializes the resource too) a Virtuoso lookup is
  2.5 ms at both 1k and 100k, and an in-memory one 1.5 ms and 1.0 ms.
- **A lookup of a common value can get slower.** A sub-select is evaluated before it is joined, so the
  lookup costs one row per resource holding the value, whatever the outer query needs. That swaps a
  cost that grew with the model for one that grows with the number of matches: a large win for a unique
  value, and a loss where the old plan could stop at the first match. `Any`, `First` and `Take` can no
  longer do that, and `Count` pays to join the whole set. Measured in the review of #74 with 100k
  `PersonContact`s, half of them holding the value, median ms with the lookup off → on:

  | | point read (unique) | `Take(10)` | `Any` | `First` | `Count` |
  |---|---|---|---|---|---|
  | in memory | 646 → 1.1 | 747 → 902 | 0.7 → 714 | 709 → 789 | 660 → 781 |
  | Oxigraph | 134 → 133 | 50 → 98 | 0.6 → 154 | 32 → 75 | 141 → 205 |
  | GraphDB | 93 → 5.8 | 5.8 → 8.9 | 2.9 → 4.2 | 3.6 → 4.8 | 135 → 233 |
  | Fuseki | 337 → 8.1 | 10.8 → 10.3 | 6.7 → 7.3 | 6.9 → 7.3 | 229 → 252 |
  | Virtuoso | 34.8 → 0.8 | 1.4 → 1.6 | 1.0 → 1.1 | 1.6 → 1.4 | 71.7 → 52.5 |

  Virtuoso does not regress, because it pushes the join down. In memory, `Any` over a common value goes
  from constant time to O(matches), about 1000× at 100k. `LinqSelectivityBenchmarks` keeps this side
  measured.

  The lookup is kept for every predicate anyway, on purpose. #64's case, a lookup called once per
  resource, is the common one, and so is `FirstOrDefault(p => p.Name == x)` as a point lookup.
  Skipping the lookup for `Any` or the limit-1 terminals would give up exactly those. And the new cost
  is bounded by the size of the answer rather than the model.
- **Oxigraph is not fixed, and loses on common values.** It pushes a binding into neither a sub-select
  nor a `UNION`, so a unique-value lookup does not improve (214 → 153 ms in the table, 134 → 133 ms in
  the review). Every query over a common value gets 1.5–250× slower. Every shape Oxigraph does answer
  from an index was wrong or slow on another backend. A fix there needs store-specific SPARQL, which the
  translator does not produce; #64 stays open for it.
- **Small Virtuoso models pay about 1 ms per lookup** for the sub-select: `LinqPoint` at 1k went from
  1.6 to 2.5 ms. The break-even is near 2k resources.
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
