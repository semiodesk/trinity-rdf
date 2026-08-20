# 0042. Staged writes: the layered view as a working copy

Date: 2026-08-20

## Status
Accepted for staging, accept and discard. **Proposed** for materialization and for anything that needs a
retained ancestor.

The staging half is built and measured: `Commit()` through a view stages, `Accept()`/`Discard()` apply
or abandon, and the divergence precondition is in place, all verified on the in-memory store, Virtuoso
and GraphDB. Materialization, three-way merge and changeset validation remain proposals — see
*Deliberately unresolved*.

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

Materializing `(baseline − removals) ∪ additions` into a fourth graph lets every query run natively
against an ordinary model. The overlay macro **is** the materialization query — one `INSERT … WHERE`
reusing what 0041 already emits, so there is no new machinery:

```sparql
INSERT { GRAPH <effective> { ?s ?p ?o } } WHERE { <the overlay> }
```

What it lifts, all of them 0041 refusals: **unbounded property paths** (verified — a chain whose hops
straddle the layers resolves correctly, which no rewrite can achieve), `GRAPH` blocks, `CONSTRUCT`,
`DESCRIBE`, `SERVICE`, **inferencing** (a store can reason over an ordinary graph), and arbitrary caller
SPARQL with no whitelist, no `SparqlExpressionWriter` and no round-trip verification.

What it costs: **18.1 s** to materialize 1,000,000 triples in-memory, plus a full duplicate of the
baseline per view. Fine as opt-in, wrong as a default — which is why it is a mode and not a replacement.
Materialization at that scale has **not** been measured on Virtuoso.

What makes it practical is that it need not be recomputed. Staging changes exactly the layer contents,
so the effective graph can be maintained **incrementally**: a staged addition inserts, a staged removal
deletes unless the triple is still present via additions. That is **O(changes), not O(baseline)** — pay
18 s once at creation, then next to nothing per stage. Incremental maintenance depends on the view
owning staging, which is the fourth reason for it.

**Staleness is silent, and that is the real objection.** Measured: stage a change, and the materialized
graph still answers from before it while the rewriting view answers correctly, with no error either way.
A snapshot that quietly answers from the past is exactly the failure class 0041 exists to prevent. It
cannot be detected cheaply and reliably from outside — triple counts are unsound, since swapping one
triple for another leaves the count unchanged, and there is no change notification (ADR-0035 removed
`INotifyPropertyChanged`). So the mode is only defensible when the view observes every write, which is
the same decision again.

One limit survives regardless: the view can only know about **its own** layers. A third party writing to
the baseline leaves the materialization stale and undetectably so. That is arguably outside the
contract — the premise is that the baseline is untouched until accept — but it is an assumption, not a
problem solved.

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
  maintained by construction and asserted by tests, so reconstruction is viable; the choice remains.
- **Additions-side conflict detection**, which needs the mapping-derived cardinality described above.
- **Concurrent accepts** from two views over one baseline. That is branching, and needs a retained
  per-view ancestor.
- **Whether staging maintains the materialization incrementally or only marks it dirty**, and what a
  stale materialized view does on read — refuse, or refresh silently.
- **Whether `Accept()` should validate at all**, versus leaving that to the caller.

## Related
- [0041](0041-layered-read-views.md) — the read view this builds on, and the write-path claim it corrects
- [0039](0039-resource-write-semantics.md) — the per-value delta that makes staging store-independent
- [0029](0029-resource-commit-rollback-change-tracking.md) — `Commit`/`Rollback` semantics and no cascade
- [0017](0017-resources-open-mapped-and-dynamic.md) — why unmapped predicates bound conflict detection
- [0028](0028-store-level-transactions.md) — why accept is not atomic on most backends
- [0014](0014-ontology-generator-modernization.md) — the generated-plus-committed artifact pattern any
  generated shapes should follow
