# 0042. Staged writes: the layered view as a working copy

Date: 2026-08-20

## Status
Accepted for staging, accept, discard **and materialization**. **Proposed** for anything that needs a
retained ancestor, and for changeset validation.

All three are built and measured on the in-memory store, Virtuoso and GraphDB: `Commit()` through a view
stages, `Accept()`/`Discard()` apply or abandon with a divergence precondition, and an opt-in fourth
graph holds the effective triples so queries run natively. Three-way merge and validation remain
proposals — see *Deliberately unresolved*.

## Context

[0041](0041-layered-read-views.md) delivered `ILayeredModel` as a **read-only** view:
`(baseline − removals) ∪ additions`, with the two layer graphs exposed as ordinary `IModel`s so a
caller stages a change by writing to them directly. That was the smallest correct surface, and it left
three questions open that turn out to share a root:

- **How does a change get applied back to the baseline?** There is no API for it; a caller has to write
  the SPARQL themselves.
- **Is this a branch?** It looks like one, and if it is, applying a change back is a merge — with all
  that implies.
- **Can the view be materialized** into a fourth graph so queries run natively, lifting the rewriter's
  refusals (property paths, `GRAPH`, `CONSTRUCT`, inferencing)?

The root they share: **while the layer graphs are written to directly, the view knows nothing about
what changed.** That single fact is what makes staleness undetectable, what makes deleted values route
incorrectly, and what makes the pre-change state unrecoverable. Each is addressed below, and each is
addressed by the same decision.

## Decision (proposed)

### The view owns staging

`Resource.Commit()` already routes through `_model.UpdateResource(this)`
([Resource.cs:1196](../../Trinity/Resource.cs#L1196)). Implementing that on `LayeredModel` — to write
the delta into the layer graphs rather than into a single model — makes `Commit()` **mean "stage"** for
a resource read through a view. No new API, no caller change, which is the promise 0041 was built on.

Two facts make this smaller than it looks:

- The guard is `if (_model != null && IsReadOnly == false)`, and `LayeredModel.Attach()` currently sets
  `IsReadOnly = true`. So today, modifying a resource read through a view and calling `Commit()` is a
  **silent no-op** — the read-only-ness is enforced by silence rather than by an exception. Dropping
  that flag is most of the work.
- **This is not "a new write path in every store", which 0041 claimed.** That was wrong.
  `SparqlSerializer.TrySerializeResourceDelta` is a store-independent static, already shared by all four
  write paths (ADR-0039). Staging routes its two output lists to different graphs: one update builder in
  `LayeredModel`, zero per-store work.

**The trap: where deleted values go.** Routing every deleted value to the removals graph is wrong, and
wrong silently:

1. Stage `name = "new"` → additions gains `"new"`.
2. Change your mind: `name = "newer"` → the delta reports `"new"` deleted, `"newer"` inserted.
3. Put `"new"` in removals, and additions now holds `"new"` *and* `"newer"` while removals holds
   `"new"` — and **additions win**, so both values are visible.

The rule has to be conditional: for each deleted value, **remove it from additions if it is there**, and
**add it to removals only if it is in the baseline** — possibly both, when a baseline triple had been
re-added. This is the crux of staging, not a detail of it.

### `Accept()` and `Discard()`, not `Commit()` and not `Merge()`

Applying the change to the baseline is `ILayeredModel.Accept()`; abandoning it is `Discard()`.

Not `Commit()`: once the view owns staging, `resource.Commit()` *means stage*, so a `view.Commit()` that
means "push everything to the baseline" one level up is a genuine trap. `Accept`/`Discard` also matches
the language 0041 already uses.

Accept is O(changes) and server-side:

```sparql
DELETE { GRAPH <baseline> { ?s ?p ?o } } WHERE { GRAPH <removals>  { ?s ?p ?o } };
INSERT { GRAPH <baseline> { ?s ?p ?o } } WHERE { GRAPH <additions> { ?s ?p ?o } };
CLEAR GRAPH <additions>; CLEAR GRAPH <removals>
```

**Removals first, then additions** — so a triple in both survives, which is the same precedence the read
path gives it. Worth stating as an invariant: additions win in both directions. Discard is the two
`CLEAR`s alone.

**Accept is atomic on all three backends, by two different mechanisms — measured.** The original
assumption here was that it could not be, and that was wrong:

| store | a multi-operation request | an explicit transaction |
|---|---|---|
| dotNetRDF in-memory | **atomic** — injecting a failure into the second operation rolls the first back | `NoOpTransaction`; rollback provably undoes nothing |
| Virtuoso | unprobed — it silently succeeds on every failure that could be injected | **`VirtuosoTransaction`** — rollback *and* commit both honoured |
| GraphDB | **atomic** — same rollback observed | `NoOpTransaction`; rollback provably undoes nothing |

So `Accept()` starts a transaction unconditionally: real on Virtuoso, a no-op elsewhere where request
atomicity already covers it. Note the corollary for the other two stores — passing a transaction there
would give a false sense of safety, since rollback demonstrably does nothing; it is request atomicity
that protects the operation.

Worth recording separately: **Virtuoso swallows failures other stores raise.** `CLEAR` and `DROP` of an
absent graph, `CREATE` of an existing one, and `LOAD` of an unresolvable URL all succeed silently there,
several of which the specification makes errors absent `SILENT`. A failed `LOAD` cannot be detected on
Virtuoso at all.

### It is a working copy, not a branch

A **branch** can diverge, has its own history, and merging it needs *three-way* reconciliation against a
recorded common ancestor. A **working copy** has one changeset against a live baseline, and applying it
is a fast-forward when the baseline has not moved.

So this stays `ILayeredModel`. A `BranchedModel` with `Merge()` would promise three-way reconciliation
and conflict resolution that does not exist, and — the important part — **renaming would neither
introduce conflicts nor avoid them.** The conflict potential is entirely a function of whether the
baseline may move, which is orthogonal to what the type is called.

Real branching stays a separate feature for a concrete reason rather than a naming one: three-way merge
needs the ancestor, and it needs it *per branch*.

### The ancestor is reconstructible, under an invariant

The pre-change baseline `B₀` does not have to be stored. Given the materialized working copy:

```
effective = (B₀ − R) ∪ A        ⇒        B₀ = (effective ∖ A) ∪ R
```

Measured — exact for every disciplined changeset, and lossy in both directions without discipline:

| staging | reconstruction |
|---|---|
| value change (remove old, add new) | exact |
| pure addition | exact |
| pure removal | exact |
| re-add of a triple already in the baseline | **triple lost** from the reconstruction |
| removal of a triple not in the baseline | **triple invented** in the reconstruction |

The inversion holds exactly when `A ∩ B₀ = ∅` and `R ⊆ B₀` — never stage an addition that is already
there, never stage a removal of something that is not. **Those are precisely what
`TrySerializeResourceDelta` produces**, because a delta records only what changed; and precisely what a
caller writing to the layer graphs directly can violate. A third independent reason for the view to own
staging.

Note what this needs: `B₀` is **not** recoverable from the current baseline plus the layers, because
there is no way to tell which of the current baseline's triples were there originally and which a third
party added. Reconstruction requires the **materialized** graph. So materialization is not merely a
performance or coverage mode — it is the enabling condition for having an ancestor at all, and therefore
for any future three-way merge.

An alternative worth weighing: at creation the layers are empty, so the first materialization *is* `B₀`.
Freezing that graph and maintaining the working copy separately costs a second full copy but depends on
no invariant. Reconstruction is cheaper in storage and depends on discipline that is being enforced
anyway.

### Materialization as an opt-in mode

`store.CreateLayeredModel(baseline, additions, removals, materialized)` keeps
`(baseline − removals) ∪ additions` in a fourth graph, so queries run natively against an ordinary
graph. The overlay macro **is** the materialization query — one `INSERT … WHERE` reusing what 0041
already emits — so there is no new machinery.

**The mode switch is two seams — for reads.** Every authored read composes its query from the dataset
clause and from one pattern helper, so pointing both at a single graph is all it takes for the same
templates to run natively: the clause becomes `FROM <materialized>` and the helper emits a bare triple
pattern instead of the overlay. LINQ passes no overlay to the writer, and inferencing becomes possible
because the store is reasoning over one ordinary graph.

**But not every query about the view is a read of it**, and the two-seams framing hid that. A query that
reasons over the *layers* rather than the effective triples must keep the three-graph dataset: in
materialized mode the clause is a bare `FROM`, which leaves the named-graph set **empty**, so a
`GRAPH <removals>` block against it matches nothing and the query silently answers as though the graph
were empty. The divergence precondition is exactly such a query, and it was written from the effective
clause — so on a materialized view `HasDiverged()` returned `false` for every input and `Accept()`
applied stale changesets without complaint, disabling this ADR's central guard precisely when the caller
had opted into the faster mode. The field is now named for what it selects rather than for where it
goes, and the layer-level dataset is requested explicitly.

What it lifts, all of them rewriting-mode refusals: **unbounded property paths** (a chain whose hops
straddle the layers resolves correctly, which no rewrite can achieve), the bounded paths that were
merely unimplemented, `CONSTRUCT`, `DESCRIBE`, `SERVICE`, inferencing, and arbitrary caller SPARQL with
no whitelist, no `SparqlExpressionWriter` and no round-trip verification.

**What it does not lift: graph selection.** "Nothing is rewritten, so nothing has to be refused" holds
only for the constructs refused *for want of a faithful rewrite*. A caller dataset clause is not one of
them. The graph is selected with a plain `FROM`, so it is the query's **default** graph — and the
preprocessor **appends** the caller's `FROM` beside the view's rather than replacing it, so the query
reads the union of the two. Measured: `SELECT ?s FROM <baseline> WHERE { ?s a <Thing> }` is refused by a
rewriting view and, before the fix, returned from a materialized one a resource whose `rdf:type` was
staged for removal — an unsubtracted triple served out of a subtractive read-only view, which is the one
failure this design must not have. A caller `GRAPH` block is the same argument: it reaches for the layers
the view exists to combine. Both stay refused in **both** modes, so materialization lifts the
rewrite-shape restrictions and nothing else.

The check is **"no graph but this one"**, not "no graph at all", and getting that distinction wrong broke
more than it fixed. Assigning `ISparqlQuery.Model` *injects* `FROM <materialized>` into the query, so by
the time it is serialized for the guard the view's own clause is already there and is indistinguishable
in kind from a caller's. A blanket refusal therefore rejected **every LINQ query** on a materialized view
— the LINQ provider assigns `Model` at construction — and made re-executing any query object fail on the
second call, since execution mutates the query. The guard takes the effective graph's URI and refuses
only other names.

Two further consequences of doing this check by parsing:

- The parse is per execution, even though nothing is rewritten — the same cost 0041 notes, with the same
  placeholder-IRI caching available if it ever shows up in a profile.
- **Materialized mode now requires the strict SPARQL parser to accept the query**, which it did not
  before. Trinity's own tokeniser accepts a wider extended syntax, so a query a plain model runs can be
  refused by a materialized view. That is a real narrowing, traded for not serving unsubtracted triples.

`RootGraphPattern` is **null** for a query with no `WHERE` clause, and a bare `DESCRIBE <iri>` is the
common case — so the walk needs a null guard, or the form materialization exists to enable is the form
that throws `NullReferenceException`. A `SELECT … WHERE {}` and a bare `ASK {}` are *not* the same shape:
dotNetRDF gives those an empty but non-null pattern.

`EXISTS` / `NOT EXISTS` keep their pattern in the filter's **expression** tree, not in
`ChildGraphPatterns`, so a `GRAPH` block nested in one is reached by neither the child-pattern nor the
sub-`SELECT` walk. Left unchecked that was a *silent wrong answer* rather than a refusal: the dataset
clause is a bare `FROM`, so the named-graph set is empty and the nested block matched nothing — the caller
asked about a named graph and was told it was empty. Unlike rewriting mode, which refuses `EXISTS`
outright because it cannot weave the overlay into it, a materialized view evaluates one happily; only the
graph selection inside is refused.

**Maintenance is O(changes), which is what makes the mode usable.** Measured on the in-memory store with
a 1,000,000-triple baseline:

| operation | cost |
|---|---|
| full build, including verification | 31.5 s |
| full build, unverified | 19.1 s |
| stage a change and keep the graph in step | **0.7 ms** |
| a native property-path query over it | 3 ms |

**A filtered scan is not O(changes), wherever the filter sits.** Deleting a resource affects every triple
mentioning it on either side, and expressing that as `?s ?p ?o` with
`FILTER (?s = <r> || ?o = <r>)` cannot be answered from an index at all — the engine enumerates the whole
graph and tests each row. Measured on a 1,000,000-triple in-memory baseline:

| shape of the re-insert | cost |
|---|---|
| filter outside the `UNION` | 3,891 ms |
| filter interpolated into both branches | 3,695 ms |
| **two bound patterns, no filter** | **0–3 ms** |

Moving the filter inward — the obvious fix, since a filter outside the group lets the engine evaluate
both branches in full first — buys 1.05x on something that needed 1000x. The subject side and the object
side have to be *separate bound patterns*, which is what an index can answer. End to end that took
`DeleteResource` on a 1M baseline from **12.5 s to 3 ms**. The same shape was in the staging update, not
only in the materialized sync, so both were rewritten.

The graph is not recomputed after a stage. Each affected triple is deleted and then re-inserted **if the
overlay says it belongs** — asking the overlay rather than deriving from the delta what should have
happened to it. That is correct by construction and cannot drift from the read path, where a
hand-written rule per staging case could. `Discard()` does need the full rebuild, since the effective
graph reverts to the baseline; a clean `Accept()` needs none, because the baseline becomes exactly what
was materialized. A **forced** accept does, since the change was applied over a baseline that had moved.

**Virtuoso cannot materialize a baseline of this size in one request, and does not say so.** Measured: a
single `INSERT … WHERE` succeeds at 500,000 rows and writes **zero** at 1,000,000, reporting success
either way — its transaction log limit, silently hit. An empty materialized graph is indistinguishable
from an empty baseline, so a caller would read a view asserting the data does not exist. `Refresh()`
therefore **counts the overlay's solutions and compares** them against what landed, and throws when they
differ, naming the limit and the `log_enable(3,1)` workaround. That verification is the reason the build
costs 31.5 s rather than 19.1 s, and it is worth the difference.

The check runs **after** the truncated write is committed, and there is nothing to roll back — so
throwing is all it can do, and throwing *once* is not enough. The failure is therefore **latched**: every
later read is refused until a rebuild succeeds. Without the latch the view raised one exception from
`Refresh()` and then answered every subsequent query from a graph it knew was short, which is the same
silent wrong answer the verification exists to prevent, merely deferred by one call. Chunked materialization would lift the
ceiling and is a follow-up; `LIMIT`-bounded inserts were verified to work, but `LIMIT`/`OFFSET` paging
without `ORDER BY` has no stable order, so it needs a partitioning scheme rather than naive paging.

**Staleness is the standing limitation.** The view keeps the graph in step for changes made *through*
it — staging, accept, discard, **and delete**, the last of which built its own update and initially
skipped the synchronization every other write path performs, leaving a deleted resource readable through
the very view that deleted it. A write straight to a layer or to the baseline leaves it stale, and that
**cannot be detected cheaply**: triple counts are unsound, since swapping one triple for another leaves
the count unchanged, and there is no change notification (ADR-0035 removed `INotifyPropertyChanged`). So
the contract is that the view maintains what it changes and `Refresh()` repairs the rest — the same
assumption the baseline already carries, extended to the layers. A test pins the stale case rather than
leaving it implied.

### Conflicts do not fail. They merge, silently

Because a changeset is a set of triples and `INSERT`/`DELETE` are idempotent, applying a stale change
**never errors**. Measured, where a third party changed the same single-valued property:

| scenario | baseline after accept |
|---|---|
| no third-party change | `["mine"]` — correct fast-forward |
| third party set the same property to `"theirs"` | **`["mine", "theirs"]`** — two values for one property |

For a multi-valued property, union is often what was wanted. For a functional one it is a silent schema
violation.

**A cheap precondition catches it.** Every triple staged for removal must still be present in the
baseline; if one is not, the baseline moved on ground the change depends on:

```sparql
ASK { GRAPH <removals> { ?s ?p ?o } FILTER NOT EXISTS { GRAPH <baseline> { ?s ?p ?o } } }
```

Measured:

| scenario | detected | outcome if applied anyway |
|---|---|---|
| competing change to one property | **yes** | `["mine","theirs"]` — the real conflict |
| no third-party change | no | `["mine"]` — correct |
| third party made the *same* removal | yes | `["mine"]` — benign, flagged anyway |
| third party touched an unrelated triple | no | `["mine"]` — correct |

It is O(removals) rather than O(baseline), **sound** — it catches every conflict of this shape — and
deliberately **conservative**: it also flags the benign case where someone else already made the same
removal. That is the right trade, and it is what a version control system does in the analogous
situation, where the context a patch assumes no longer matches.

`Accept()` therefore throws on divergence by default, with an explicit `force` to apply anyway. That
puts the project's rule — never silently wrong — at the write boundary, where it has been missing.

**What the precondition cannot see, and the honest boundary.** It does not catch *additions*-side
conflicts, where a third party adds a competing value for a functional property the changeset also sets.
Detecting that needs cardinality, and Trinity **already has it for mapped properties**: a
`PropertyMapping<T>` with `IsList == false` is a functional-property declaration, and `GenericType` plus
`XsdTypeMapper` gives the datatype. No new schema artifact is required for those.

The gap is the **unmapped** surface, and it is not an oversight — ADR-0017 makes resources open, and
`ListValues` returns unmapped predicates deliberately. For those, two values cannot be distinguished
from a legitimate set. So the contract is: **conflict detection is exactly as good as mapping
coverage.** Which argues that `Accept()` should not merely throw or not-throw but **report the split** —
how many divergent triples fall on mapped functional properties, and how many on predicates it cannot
judge. That turns an unquantified gamble into a measured risk, and it is the difference between a
documented limitation and a trap.

## Consequences

- `resource.Commit()` works through a view with no caller change, which is what 0041 promised for reads
  and did not deliver for writes. It was previously a **silent no-op**, because `Attach` marked the
  resource read-only and `Commit()` is guarded by that flag.
- A forced accept produces a schema violation that the **mapped read layer hides**: two values land in
  the baseline for a single-valued property, and a `PropertyMapping<T>` read collapses them to one. The
  data is wrong where a caller would not think to look, which is the sharpest argument for the
  precondition refusing by default.
- Four separate problems — staleness detection, deleted-value routing, ancestor recoverability, and
  incremental maintenance — are all solved by the same decision, which is the strongest argument that it
  is the right one.
- Two modes to document and test rather than one: rewriting (live, no extra storage, refuses exotic
  forms) and materialized (full coverage, snapshot, extra storage). Refusal messages improve, because
  they can point at the other mode instead of being a dead end.
- Accept is non-atomic outside Virtuoso, which is a real exposure for a multi-triple changeset.
- **A correction to 0041:** it stated that a writable view means "a new write path in every store". It
  does not; the delta computation is store-independent and already shared. That overestimate made
  read-only look more necessary than it was.

## Deliberately unresolved

Recorded so these are not mistaken for oversights:

- **Validation of changesets.** Whether SHACL runs over a narrow graph derived from the changeset, what
  its extent must be, and how much of a shapes graph the mapping can generate. Deliberately excluded
  here; it touches parts of the system that are themselves unsettled.
- **Reconstruct the ancestor or freeze it.** Both are described above. The staging invariants are now
  maintained by construction and asserted by tests, and materialization — which reconstruction needs —
  exists, so the approach is viable; the choice remains.
- **Chunked materialization**, to lift the Virtuoso ceiling above its transaction log limit. Needs a
  stable partitioning scheme, not `LIMIT`/`OFFSET` paging.
- **Additions-side conflict detection**, which needs the mapping-derived cardinality described above.
- **Concurrent accepts** from two views over one baseline. That is branching, and needs a retained
  per-view ancestor.
- **Whether `Accept()` should validate at all**, versus leaving that to the caller.

## What is measured, and what is tested

This ADR was written from throwaway measurement harnesses, and two review rounds found defects in code
whose *claims* here nothing held it to. So the distinction is recorded explicitly rather than left to be
inferred: a claim that is only measured can rot silently, and both regressions found in review were of
exactly that kind.

**Pinned by the cross-store suites** (`LayeredModelStagingTest`, `LayeredModelMaterializationTest`, and
one subclass per backend):

| claim | test |
|---|---|
| `Commit()` through a view stages rather than writes | `CommitThroughTheViewStagesRatherThanWrites` |
| deleted-value routing (the crux) | `StagingTheSamePropertyTwiceLeavesOneValue`, `RestoringTheBaselineValueEmptiesBothLayers` |
| additions win, on read and on accept | `AdditionWinsOverRemovalForTheSameTriple`, `AcceptGivesAdditionsPrecedenceJustAsReadsDo` |
| the two staging invariants | `StagingMaintainsTheAncestorInvariants` |
| `B₀ = (effective ∖ A) ∪ R`, exact in all three disciplined cases | `TheAncestorIsRecoverableAfterAValueChange` / `…APureAddition` / `…APureRemoval` |
| …and lossy in both undisciplined ones | `ReAddingABaselineTripleLosesItFromTheReconstruction`, `StagingTheRemovalOfAnAbsentTripleInventsIt` |
| all four rows of the precondition table | `AcceptRefusesWhenTheBaselineMovedUnderTheChange`, `AnUnrelatedBaselineChangeIsNotDivergence`, `AcceptAppliesTheStagedChangeAndEmptiesTheLayers`, `TheSameRemovalByAThirdPartyIsFlaggedAnyway` |
| a forced accept merges, and mapped reads hide it | `ForcedAcceptMergesAndTheDamageIsInvisibleToMappedReads` |
| request atomicity protects accept where the transaction is a no-op | `AFailedOperationRollsBackTheOnesBeforeIt` |
| …and rollback there provably undoes nothing | `RollbackOnANoOpTransactionUndoesNothing`, with the Virtuoso subclass asserting the inverse |
| a clean accept needs no rebuild; discard and a forced accept do | `ACleanAcceptLeavesTheEffectiveGraphAlreadyCorrect`, `DiscardRebuildsTheEffectiveGraphFromTheBaseline`, `AForcedAcceptRebuildsTheEffectiveGraph` |
| staleness after an out-of-band write, repaired by `Refresh()` | `AnOutOfBandWriteGoesStaleUntilRefreshed` |
| a short materialization is latched, not served | `AShortMaterializationRefusesEveryLaterRead` |
| the two modes answer alike, LINQ included | `MaterializedAnswersAsTheRewritingViewDoes`, `Linq*AgreesBetweenTheModes` |
| materialization lifts the inferencing refusal | `InferenceIsRefusedRewritingButAcceptedMaterialized`, `InferenceThroughLinqIsAcceptedWhenMaterialized` |
| entailments are computed from the *effective* triples, so a staged removal withdraws what it entailed | `EntailmentsOverAMaterializedViewHonourTheOverlay` (in-memory only — see below) |

**Measured but not tested**, and deliberately so:

- **Virtuoso writes zero above its transaction-log limit.** The trigger is between 500,000 and 1,000,000
  rows; no test can reach it in reasonable time. The *latch* is tested by provoking the state directly,
  so the consequence is pinned even though the detection is not.
- **The performance figures** (31.5 s rebuild, 0.7 ms stage, 12.5 s → 3 ms on the delete path). Timings
  do not belong in a correctness suite, but the *shape* they justify does — bound patterns rather than a
  filtered scan — and that shape is what the delete tests exercise.
- **That any given store actually reasons.** ADR-0022 leaves inferencing a per-query flag a store may
  honour or ignore, and Fuseki has no per-query switch at all. So the shared fixture asserts only that a
  materialized view *accepts* an inference-enabled read; that entailments follow the overlay is pinned on
  the in-memory store, where [0045](0045-in-memory-rdfs-inferencing.md) guarantees a reasoner. Before
  0045 that combination could not be checked on this store at all — the flag was accepted and quietly did
  nothing — so it is a claim this ADR made and only the merge of the two made testable.
- **Virtuoso swallows failures other stores raise** (`CLEAR`/`DROP` of an absent graph, `CREATE` of an
  existing one, `LOAD` of an unresolvable URL). This is Virtuoso behaviour rather than Trinity's, and it
  is why `MultiOperationRequestIsAtomic` is overridden to `false` there — *unprobed*, not untrue.

## Related
- [0041](0041-layered-read-views.md) — the read view this builds on, and the write-path claim it corrects
- [0039](0039-resource-write-semantics.md) — the per-value delta that makes staging store-independent
- [0029](0029-resource-commit-rollback-change-tracking.md) — `Commit`/`Rollback` semantics and no cascade
- [0017](0017-resources-open-mapped-and-dynamic.md) — why unmapped predicates bound conflict detection
- [0028](0028-store-level-transactions.md) — why accept is not atomic on most backends
- [0014](0014-ontology-generator-modernization.md) — the generated-plus-committed artifact pattern any
  generated shapes should follow
