# 0041. Layered read views: baseline + additions − removals

Date: 2026-08-19

## Status
Accepted

## Context

Trinity can union named graphs but it cannot subtract from them. `IModelGroup` derives a cached
dataset clause of one `FROM <uri>` per member (`ModelGroup.UpdateDatasetClause` →
`SparqlSerializer.GenerateDatasetClause`), and `FROM` is set union — SPARQL offers no inverse. So a
model group can express "read these graphs together" and nothing else.

What was needed is git-working-tree semantics over RDF. An agent (or a user) works against a large
`baseline` graph and holds its pending work as an `additions` and a `removals` graph. Everything
reading through the view should see the baseline as if the pending change had been applied —
**deletions included** — while the baseline itself stays untouched until the change is accepted or
discarded.

The requirement that shapes the design is that mapped-object reads honour it: if `GetResource`,
`GetResource<T>`, `GetResources<T>` and `ContainsResource` apply the overlay, code written against
`IModel` needs no change at all.

Subtraction cannot be added by extending the dataset clause. It has to reach the **graph pattern** of
every query the view answers — which is precisely the part a model group does not own for arbitrary
queries. And the failure mode is silent: a read path that ignores the removals graph returns triples
the caller believes are deleted, with nothing to signal it.

## Decision

### A sibling abstraction, not an extension of model groups

`ILayeredModel : IModel` with `Baseline`, `Additions` and `Removals`, implemented by `LayeredModel`.
It is deliberately **not** an `IModelGroup`:

- A group is an `ISet<IModel>` of interchangeable members that are merely unioned. The three graphs
  here have distinct roles, so `Add`/`Remove` have no meaning for them.
- Keeping the types apart means the existing union-only code paths — the `is IModelGroup` branches in
  the `SparqlQuery.Model` setter and in `GenerateDatasetClause`, and GraphDB's inference path, which
  rebuilds a query's dataset as a `ModelGroup` — cannot mistake a layered view for a union. The
  `IModelGroup` contract is untouched and no existing consumer changes behaviour.

Created by an extension method, `IStore.CreateLayeredModel(...)` in `StoreExtensions`, rather than a
new `IStore` member. A layered model needs nothing store-specific beyond `ExecuteQuery`/`GetModel`, so
this avoids a breaking change to the interface every custom backend implements — and avoids
reproducing the `CreateModelGroup(params IModel[])` empty-group bug that three stores still carry.

### Semantics: additions win

`effective = (baseline − removals) ∪ additions`. A triple in both `additions` and `removals` is
**visible**. This is what makes an ordinary edit — remove the old value, add the new one — behave the
way callers expect, and it means staging an addition always makes it readable. The alternative reading
of the formula, `(baseline ∪ additions) − removals`, would let a stale removal mask a fresh addition.

### One overlay macro, four consumers

`LayeredModelSparql` is the single source of truth. Every triple pattern becomes

```sparql
{ { GRAPH <baseline>  { S P O } <guard> }
  UNION
  { GRAPH <additions> { S P O }
    FILTER NOT EXISTS { GRAPH <baseline> { S P O } FILTER NOT EXISTS { GRAPH <removals> { S P O } } } } }
```

The guard applies **only** to the baseline branch — that asymmetry is what makes additions win. The
dataset clause is `FROM NAMED` for all three graphs, never bare `FROM`, because the overlay addresses
them with `GRAPH` and merging them would union the removals graph straight back in.

**The two branches are disjoint by construction, and that is load-bearing.** SPARQL `UNION` is a
**bag** union, so overlapping branches yield *two* solutions for a triple present in both the baseline
and the additions — inflated `COUNT`s and duplicated rows — where the view promises the *set*
`(baseline − removals) ∪ additions`. Re-adding a value that is already there is precisely what
"additions win" invites a caller to do, so the overlap is the ordinary case, not an exotic one. The
additions branch therefore excludes whatever the baseline branch already produced. Its inner guard is
always `FILTER NOT EXISTS`, never `MINUS`: nested inside `NOT EXISTS` the pattern is tested for
existence rather than joined, so a `MINUS` with no shared variables would remove nothing and invert the
answer for a triple present in all three graphs.

Used by exactly four call sites: `LayeredModel`'s own templates, `SparqlQueryWriter` (LINQ),
`SparqlSerializer.GenerateDatasetClause` and `SparqlSerializer.SerializeOffsetLimit`.

### The guard primitive is chosen for correctness, not speed

**`MINUS` when the pattern binds at least one variable; `FILTER NOT EXISTS` when it is fully ground.**

`MINUS` is much the faster primitive — engines evaluate it as an anti-join rather than a per-row
nested-loop probe, worth 18x → 1.5x on an unselective scan. But SPARQL `MINUS` removes nothing when
the two sides share no variables, and a fully ground triple pattern has none. There, a `MINUS` guard
**silently fails to subtract**. Measured, with a triple present in both `baseline` and `removals`:

| | in-memory | Virtuoso | GraphDB |
|---|---|---|---|
| via `FILTER NOT EXISTS` | subtracted | subtracted | subtracted |
| via `MINUS` | **not subtracted** | subtracted | **not subtracted** |

dotNetRDF and GraphDB are spec-correct; Virtuoso is the deviation. A ground pattern is a single
existence check, so `FILTER NOT EXISTS` costs nothing there and is the only safe choice.
`LayeredModelSparqlTest.MinusWouldNotSubtractAGroundPattern` pins the trap so the rule cannot be
simplified away later.

### Emission rules that are requirements, not optimisations

Two further rules came out of measurement, and getting either wrong is a four-orders-of-magnitude
regression rather than a wrong answer:

1. **Bind a known subject with `VALUES ?s { <u> }` before the overlay**, never as a trailing
   `FILTER (?s = <u>)`. Neither the in-memory engine nor GraphDB pushes a filter into a `UNION`
   containing an anti-join — 15x and 38x respectively, against ~1.0x for the `VALUES` form.
2. **Emit selective patterns before the wildcard `?s ?p ?o`.** An engine that cannot reorder joins
   across the overlay's `UNION` evaluates patterns as written, so a leading wildcard materialises the
   whole effective graph before applying any constraint: **61,000x** on the in-memory engine.
   `SparqlQueryTranslator.BuildResourceQuery` previously emitted the wildcard *first*, so this
   required changing it — the naive rewrite lands exactly on that case. Pattern order inside a basic
   graph pattern is semantically irrelevant, so the change is safe for plain models, which reorder
   freely.

A third constraint is structural: resource materialization refuses any query for which
`ISparqlQuery.ProvidesStatements()` is false, and that is decided by a token-level heuristic wanting
exactly three same-ordered global variables. The overlay wraps the patterns in nesting, `UNION` and
`VALUES`, so this is easy to break unnoticed; `EveryEmittedShapeProvidesStatements` asserts it for
every shape the implementation emits.

### LINQ is honoured natively; caller SPARQL is rewritten

LINQ is honoured because Trinity **owns** its LINQ AST. `SparqlQueryWriter.WritePattern` switches over
eight node types with `default: throw`. Only `TriplePattern` resolves against data — groups,
`OPTIONAL`, `UNION`, `MINUS` and sub-selects merely contain patterns, and `BIND`/`VALUES` touch no
graph — so wrapping **one case arm** is exhaustive by construction, and a node type added later throws
rather than leaking.

Caller-supplied SPARQL is handled by `OverlayQueryRewriter`, which works on the **parse tree** rather
than the text. That is what makes it tractable: the abbreviations that would defeat a token-level
rewrite — predicate-object lists (`;`), object lists (`,`), blank-node property lists (`[ ]`) — are
already expanded into plain triple patterns by the time the parser is done. It is a **whitelist**: every
pattern kind and every filter expression must be recognised as safe, and anything else throws with the
reason.

Accepted and covered by the corpus in `LayeredModelQueryCorpusTest`: basic graph patterns, multiple
joined patterns, `FILTER` (numeric, string via `str()`, negated equality), `;` and `,` lists,
IRI-valued objects, `OPTIONAL`, `UNION`, `MINUS`, sub-`SELECT`, `VALUES`, `BIND`, `DISTINCT`,
`ORDER BY`/`LIMIT`/`OFFSET`, `SELECT *`, aggregates with `GROUP BY`/`HAVING`, `ASK`, `IN`/`NOT IN`,
nested function calls, arithmetic precedence, and negation wrapped around a comparison, conjunction or
disjunction.

Refused, each detected from the parse tree rather than guessed:

| Form | Why |
|---|---|
| property path — see below | the unbounded forms cannot be supported at all; the bounded forms are simply not implemented yet |
| explicit `GRAPH` block | a view is itself built from three graphs; naming one reads past the overlay, and `GRAPH ?g` exposes the removals graph as ordinary data |
| `SERVICE` | the remote endpoint knows nothing of the overlay |
| `CONSTRUCT` | its template describes triples to build, not to match, so the overlay must not be applied there |
| `DESCRIBE` | the store chooses the triples; there is no pattern to rewrite |
| `FILTER EXISTS` / `NOT EXISTS` | not a child pattern but an `ExistsFunction` inside the filter expression, whose nested pattern would read the baseline directly |
| a blank node, including the `[ … ]` property-list form | the overlay repeats each pattern across three basic graph patterns, and SPARQL forbids a blank-node label appearing in more than one of them |
| a negation inside `HAVING`, a projected expression, `GROUP BY` or `ORDER BY` | dotNetRDF mangles it and none of those four are re-emitted by the rewriter — see below |
| a query with its own `FROM` / `FROM NAMED` | the view defines the dataset, so a query cannot also choose one |

### Why property paths are refused, and which of them could not be otherwise

The **unbounded** forms — `p+`, `p*`, `p{n,}` — cannot be supported, and the reason is sharper than
"paths are hard": **transitive closure does not distribute over the union of the layers.** Measured —
with `a p b` in the baseline and `b p c` in the additions, so the chain crosses the layer boundary:

| evaluation | result |
|---|---|
| closure over the merged graph (correct) | `b, c` |
| closure inside each graph, then unioned | `b` |

The chain exists in the effective graph but in neither layer alone, so the path cannot be pushed inside
the overlay's `GRAPH` blocks. And being unbounded it has no finite expansion into triple patterns to
push the overlay into instead. Evaluating one correctly would mean materializing the effective graph
first — a write, and O(baseline) per query.

The **bounded** forms are a different matter, and are refused only because the expansion is unwritten:
`^p` is a subject/object swap, `p1/p2` expands into two patterns joined by a fresh variable, `p1|p2`
into a `UNION`, and `!p` into a variable predicate with a filter. Each then goes through the overlay one
pattern at a time; the sequence case was verified to give the same answer as the native path. The
refusal message distinguishes the two groups so a caller can tell which side of the line they are on.
Implementing the bounded forms is a worthwhile follow-up — the original blanket claim that paths "cannot
be rewritten" was too broad.

### Expressions are serialized by Trinity, not by dotNetRDF

dotNetRDF's expression serialization is **not faithful**: it drops the parentheses around the operand of
a negation, so `!(?r < 3)` comes back as `!?r < 3` — that is `(!?r) < 3`, a different question — and the
result still parses, so nothing flags it. `SparqlFormatter` has the same defect, and
`!(!(?r < 3))` produces text that does not re-parse at all. Parsing is fine; the tree is correct. Only
writing it out is wrong.

`SparqlExpressionWriter` therefore serializes expressions itself, and is used for every `FILTER` and
`BIND` the rewrite emits. It **parenthesises every operator application** rather than emitting the
minimum a precedence table would allow: redundant parentheses cannot change meaning, so correctness
needs no precedence knowledge — and a precedence table is exactly the kind of thing that is subtly wrong
in one corner. Self-delimiting forms (function calls, aggregates) keep their own shape, with arguments
serialized recursively so a bad expression cannot hide inside one. Measured over a 22-expression corpus:
faithful in 22/22, where dotNetRDF is faithful in 17/22.

**Four things it cannot repair, only detect.** `HAVING`, projected expressions, `GROUP BY` and
`ORDER BY` are not re-emitted by the rewriter — they come from dotNetRDF's serialization of the query
head and solution modifiers, which is reused verbatim precisely so that projection, grouping and
ordering need not be reimplemented. A negation inside any of them is mangled exactly as it is inside a
filter, so such a query is **refused** with a suggested rephrasing. Extending the writer to the head
would lift that restriction and is a reasonable follow-up; it was not worth reimplementing projection
emission for.

**The rewrite is verified, not trusted.** The result is re-parsed and everything that had to be
preserved is compared structurally against the original: query form, `LIMIT`, `OFFSET`, projected
variables, `HAVING`, projected expressions, and every filter and `BIND` expression. A mismatch throws
rather than executes.

That check earns its keep. It caught two defects during development that no test of the *output* would
have found: filters and `BIND`s held in `GraphPattern.UnplacedFilters` / `UnplacedAssignments` rather
than among a group's triple patterns were being dropped from the rewrite entirely — a silently lost
constraint — and the same filter reachable through both `Filter` and `UnplacedFilters` was being
double-counted. Filter signatures are compared as a **distinct set** for that reason; a repeated filter
is idempotent, so a count difference carries no meaning. Comparing them at all is only sound because a
caller's own `EXISTS`/`NOT EXISTS` is refused, which is what lets the overlay's own `FILTER NOT EXISTS`
guards be excluded from the comparison unambiguously.

### Parameters, and a deferred optimisation

The strict SPARQL parser rejects Trinity's `@parameter` syntax
(`Unexpected Character (Code 64) @ encountered`), so the rewrite runs on `ISparqlQuery.ToString()`,
after every `Bind()` call has been substituted.

**The accepted cost:** a caller query is parsed, walked, re-emitted and re-parsed on *every* execution.
For a query executed in a loop with different parameter values, that work is repeated each time even
though only the bound values changed. This is a deliberate trade — correctness and a small
implementation over speed — and it affects only caller-supplied SPARQL; the mapped reads and LINQ build
their queries with the overlay already in place and never go near the rewriter.

**The optimisation, when it matters:** substitute each `@parameter` with a placeholder that *is* legal
SPARQL — an IRI such as `<urn:trinity:param:subject>` — before parsing, rewrite once, cache the result,
and map the placeholders back to `@parameter` in the output so Trinity's own preprocessor can keep
binding values into it. That makes the rewrite a per-query-shape cost instead of a per-execution one,
and needs no change to either parser. It is purely lexical, and the round-trip verification would still
apply to the cached form. Worth doing if profiling ever shows caller queries on a layered view are hot;
not worth doing before that.

A related consequence worth knowing: Trinity's own tokeniser accepts a wider extended syntax than the
strict parser, so a query may be accepted by a plain `Model` and refused by a view.

### Read-only, and no inferencing

Every mutating member throws, as `ModelGroup` does. Stage a change by writing to `Additions` and
`Removals`, which are ordinary models.

*Correction, added later:* this originally justified read-only by claiming that routing writes would mean
"a new write path in every store". That is wrong. `SparqlSerializer.TrySerializeResourceDelta` is
store-independent and already shared by all four write paths (ADR-0039), so staging is one update builder
that routes its two output lists to different graphs — no per-store work. Read-only remains the right
call for this release, but on the grounds of scope rather than cost. See
[0042](0042-staged-writes-and-accept.md).

`inferenceEnabled: true` throws. Every store's inference path defeats the overlay — GraphDB rebuilds
the dataset as a plain model group, Virtuoso answers with a bare `DESCRIBE` that cannot carry a guard —
and entailment over a subtracted graph is not something any store reasoner defines.

`SparqlEndpointStore` cannot host a layered model at all: it calls `ClearDefaultGraphs()` /
`ClearNamedGraphs()` on every query before sending it, stripping the `FROM NAMED` the overlay relies
on. `CreateLayeredModel` refuses it rather than adding a capability model (ADR-0022 declines one).

### All three graphs must live in one store

The overlay is **one SPARQL query over one dataset**, so a graph held in a different store is simply
not in scope — `GRAPH <g>` matches nothing there. Composing a view across stores (say a Virtuoso
baseline with an in-memory pending change) was measured, and it fails *silently* in either direction:

| Arrangement | Result |
|---|---|
| Virtuoso executes, layers in memory | returns the triple staged for **removal**; no exception |
| in-memory executes, baseline in Virtuoso | **drops the baseline entirely**; no exception |

Both are precisely the failure this design exists to prevent, so it is made unrepresentable rather
than documented: `LayeredModel`'s constructor is **internal** and the factory resolves all three URIs
against one store, while the `IModel` overload rejects a model belonging to another store
(`StoreExtensions.RequireSameStore`). Every `IStore.GetModel` returns the concrete `Model`, so the
check covers every model a caller can obtain.

To overlay a change that lives elsewhere, either copy the additions and removals graphs into the store
holding the baseline — they are small by nature, being one pending change — or merge the layers
client-side, which is tractable for the subject-bound reads but not for LINQ selection or type scans.
Federating with SPARQL `SERVICE` is possible in principle (dotNetRDF exposes
`VDS.RDF.Query.Algebra.Service`) but would ship a subquery per pattern and defeat the guard pushdown
the measured numbers depend on. None of that is in v1.

### Coverage

| Read path | v1 |
|---|---|
| `GetResource` (all 5 overloads), `GetResources(uris, type)`, `GetResources<T>()` | honoured |
| `ContainsResource`, `IsEmpty` | honoured |
| `AsQueryable<T>()` — every form the provider supports | honoured |
| Lazy loading of linked resources | honoured |
| Result `Count()` and paged `GetResources(offset, limit)` | honoured |
| `ExecuteQuery(ISparqlQuery)`, `GetResources(query)`, `GetResources<T>(query)`, `GetBindings` | **rewritten**, or throws for a form outside the whitelist |
| any read with `inferenceEnabled: true` | **throws** |
| all mutators, `ExecuteUpdate`, `Read`, `Write`, `Clear`, `BeginTransaction` | **throws** |
| construction over `SparqlEndpointStore` | **throws** |

Note the deliberate asymmetry: `GetResources<T>()` and `AsQueryable<T>()` cover the mapped cases while
the `ISparqlQuery` overloads throw.

## Consequences

**Performance is a non-issue on the server stores and acceptable in memory.** Measured through the
production code path at 1,000,000 baseline triples, with ~100 staged additions and ~100 staged removals:

| Read | in-memory | Virtuoso | GraphDB |
|---|---|---|---|
| `GetResource` (subject-bound) | 1.31x | 0.94x | 1.29x |
| `ContainsResource` (`ASK`) | 3.45x | 1.18x | 1.04x |
| caller query, unselective scan | 2.24x | 0.97x | 0.94x |
| caller query, selective | 4.23x | 1.47x | 2.33x |

The in-memory ratios above 2x are on sub-millisecond absolutes where fixed cost dominates — the caller
rows include parse, rewrite and re-parse, which the mapped reads skip entirely. The one number worth
noting is the in-memory unselective scan: 1.50x before the set-semantics fix, 2.24x after, because the
additions branch now carries a nested `NOT EXISTS`. Still inside the original 5x gate, and the price of
a correct answer.

**The cheap alternative was measured and rejected.** `COPY <baseline> TO <working>` works on all three
stores — this contradicts the initial expectation that Virtuoso's `SPARQL { … }` update wrapper would
reject a graph-management form; it does not. But copying costs 3.9–6.0 s and a full duplicate of the
baseline *per working copy* at 1M triples, against a ~1x read cost, and accepting the change then needs
an O(baseline) diff instead of reading an O(changes) delta. For the intended use — a small pending
change against a large baseline — the overlay wins. A `CopyModel` primitive remains worth having for
long-lived, write-heavy working copies; it is not this feature.

**Costs and limits.**
- Caller-supplied SPARQL against a layered model is a hard error, which will surprise anyone used to
  `ModelGroup.ExecuteQuery`. That is the intended trade: loud over silently wrong.
- No inferencing through a view.
- The overlay defeats join reordering on engines that cannot see through `UNION`, so emission order is
  load-bearing. The two ordering rules are commented at their emission sites and asserted by tests;
  a future refactor that "tidies" them will regress performance by orders of magnitude without
  changing any result.
- `LayeredModel` duplicates a good deal of `ModelGroup`'s interface boilerplate. Extracting a shared
  read-only `IModel` base was considered and skipped: `ModelGroup` is load-bearing for existing
  consumers and this release favours not touching it.

**Five defects were found in review after the first implementation**, three of them silent wrong
answers, and they are recorded because each says something about where this design is fragile:

1. **A union reached as an alternative of another union lost its disjunction** — emitted as a join.
   Only the child-iteration path checked `IsUnion`, so `{{A} UNION {B}} UNION {C}` degraded. Since
   `A UNION B UNION C` parses *left-nested*, this was the ordinary three-way case, not an edge one.
2. **The overlay was a bag, not a set** — the branches overlapped, so a re-added triple produced two
   solutions. Fixed by making them disjoint (above).
3. **`GROUP BY` and `ORDER BY` expressions escaped the round-trip check**, so the dotNetRDF negation
   defect reached them unnoticed. Both are now checked, direction included.
4. **Blank-node patterns produced an unparseable rewrite** and were reported as a rewriter defect
   rather than as the unsupported form they are.
5. **`BIND` was relocated after all triple patterns**, which is invalid when its variable is used by
   one of them — pattern reordering is now confined to contiguous runs of match patterns, since `BIND`
   is order-sensitive and a triple pattern is not.

The lesson is that a corpus of hand-written expectations only catches what someone thought to write
down: the mislabelled corpus case ("blank node property list", which was a predicate-object list) is
exactly why #4 survived it. `LayeredModelDifferentialTest` is the answer — with empty layers the view
must answer exactly as the plain baseline model does, or refuse, so the baseline *is* the oracle and no
expectations have to be authored. It catches #1, #3 and #5 directly, and was verified to fail when #1
is reintroduced.

**Explicitly out of scope for v1:** branching, merge, conflict detection, history, nested or stacked
overlays, and views spanning more than one store. One baseline, one additions graph, one removals
graph, one store.

**Verified:** in-memory 589 passed / 3 pre-existing failures / 7 skipped; Virtuoso 221 passed / 4
pre-existing failures / 1 skipped; GraphDB 228 passed / 4 pre-existing failures / 1 skipped. In each
case the failure set is byte-identical to the same suite at the previous commit, and the delta is
exactly the new tests (+27 in-memory, +16 per store). Fuseki is not covered: its suite is a
hand-written non-generic copy and the backend is 4/86 on an upstream `FusekiConnector` bug (ADR-0036).

## Related
- [0019](0019-models-are-named-graphs-modelgroups.md) — models are named graphs; model groups union them
- [0016](0016-resource-centric-not-triple-centric.md) — resource-centric reads are what the view has to serve
- [0022](0022-store-capabilities-and-istore-extension.md) — no capability model; hence the single negative store check
- [0029](0029-resource-commit-rollback-change-tracking.md), [0039](0039-resource-write-semantics.md) — the write path a writable v2 would have to split
- [0037](0037-linq-provider-rebuild.md) — the owned LINQ AST that makes the LINQ rewrite exhaustive
- `Trinity/Model/ILayeredModel.cs`, `Trinity/Model/LayeredModel.cs`, `Trinity/Query/LayeredModelSparql.cs`
