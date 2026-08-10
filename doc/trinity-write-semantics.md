# Trinity write semantics — defects found while fixing ADO 129003

**Not a plan for this repository.** This is a hand-off to the Semiodesk Trinity work stream, expecting a
NuGet release back. Electrix OS works correctly against published **1.0.3.77** without any of it
(ADR-068); landing these lets us delete compensations, it is not a dependency.

Findings are against the `develop` checkout at `c5c344f`, which is the source of the published
1.0.3.77 — the build number is not stamped in the repo.

Each item lists what we measured, not what we inferred.

---

## Response from the Trinity work stream (2.0)

All six items were re-verified against `develop` at `ad74b73` and **every one still reproduced** — the
2.0 revival never touched the write path. Resolution below; the design is recorded in
[ADR-0039](adr/0039-resource-write-semantics.md).

| # | Item | Status in 2.0 |
|---|---|---|
| 1 | `UpdateResource` erases concurrent writes | **Fixed.** Commits now write a per-value delta instead of replacing the resource |
| 2 | Dangling references read back hollow | **Detectable.** New `Resource.IsUnresolved` distinguishes them from genuinely empty resources |
| 3 | No aggregate dirty state | **Partly.** `Resource.HasUnsavedChanges()` supplies the per-resource primitive; there is still no aggregate/cascade state |
| 4 | `ListValues` omission deletes the property | **Dissolved.** Omission can no longer delete — see below |
| 5 | In-memory `BeginTransaction` returns `null` | **Fixed.** All non-transactional stores return a `NoOpTransaction` |
| 6 | Blank-node children re-mint their id | **Unchanged, and worse than reported** — see below |

**On item 1.** The fix is snapshot-and-diff: a resource records its values whenever `IsSynchronized`
becomes true (materialization and after commit), and `Commit()` emits only the triples that changed.
Note that *per-predicate* granularity would **not** have fixed the measured case — six writers each
adding a child all touch the same predicate. It had to be per-value, which is the same shape as
`GraphWrites.cs`. Setter-based dirty tracking was not an option either: list properties are seeded with
a plain `List<T>`, so `parent.Children.Add(x)` is invisible to setters but visible to a diff.

**On item 4.** The hazard needed no repair. A property that fails to materialize is absent from both the
snapshot and the current values, so it lands in neither the delete nor the insert set. What was
"omission means deletion" is now "omission means untouched".

**On item 6.** Confirmed as a live trap, and the delta makes one aspect sharper: blank nodes are illegal
in SPARQL `DELETE` templates, and delta writes name triples directly where the old code deleted through
variables. The test written to cover this is **quarantined**, because it never gets that far — reading a
mapped collection whose value is a blank node already fails on the *query* side with
`"Cannot resolve a Relative URI Reference since there is no in-scope Base URI"`. So blank-node handling
is broken on read before it is questionable on write. Tracked in `doc/known-test-failures.md`.

**Also found, not in this report:** `UpdateResource(..., ignoreUnmappedProperties: true)` was a data-loss
trap — the INSERT omitted unmapped triples while the DELETE still removed all of them, so the flag
deleted unmapped properties rather than preserving them. Delta writes compute removals against the
resource's complete value list, so the flag can now only suppress a write, never cause a delete.

**Not addressed:** the aggregate/cascade state of item 3, and a Fuseki regression fixture (its test
project has a different structure and the backend is 4/86 on an upstream connector bug).

---

## 1. `UpdateResource` silently erases concurrent writes to a shared resource

**Severity: high — silent data loss, no exception.**

`StoreBase.UpdateResource` (`Trinity/Stores/StoreBase.cs:347`) on an existing resource is:

```sparql
WITH <graph> DELETE { <uri> ?p ?o. } INSERT { …full serialization… } WHERE { OPTIONAL { <uri> ?p ?o. } }
```

The read-modify-write cycle a caller performs to add one link therefore rewrites the whole resource, and
concurrent writers erase each other's additions.

**Measured.** Six concurrent `GetResource(parent) → add child → UpdateResource(parent)` cycles against
one parent:

| | dotNetRDF memory store | Virtuoso |
|---|---|---|
| Independent child resources created (`AddResource`, INSERT-only) | 6 of 6 | **3 of 6** |
| Links surviving on the shared parent | **1 of 6** | **1 of 6** |
| Exceptions raised | none | none |

Store-independent, so it is the pattern rather than the backend. The Virtuoso creation loss disappeared
once the rewrites were removed, suggesting the rewrite churn on the shared connection was also
disrupting unrelated INSERTs.

**Ask.** A targeted-update API — write only what changed — so callers need not overwrite a whole
resource to modify one property. Gating whole-resource overwrite on a dirty flag will not work: see
item 3.

We worked around it with SPARQL `INSERT DATA` / scoped `DELETE`+`INSERT` per predicate, in
`Elxos.Domain/Persistence/GraphWrites.cs`. That is a reasonable shape for the upstream API.

---

## 2. Dangling references commit silently and read back hollow

**Severity: high — a broken graph is indistinguishable from a valid one.**

Committing a parent that references a new, uncommitted child persists `<parent> <p> <child>` with **zero
triples for `<child>`**. Nothing errors.

On read, `ResourceCache.LoadCachedValues` (`Trinity/ResourceCache.cs:120`) does
`Activator.CreateInstance(baseType, uri)` for any cached URI the store did not return, so the dangling
link materializes as an **empty resource instance** rather than a miss. The caller cannot distinguish a
resource that exists and is empty from one that was never written.

**Ask.** At minimum a way to detect it — a diagnostic, or a distinguishable "unresolved" state on the
materialized instance. Refusing the write is defensible too, but detection is the part callers cannot
build themselves.

---

## 3. No aggregate dirty state, so overwrite cannot be gated

`IsNew` / `IsSynchronized` are per-resource: a parent can be `IsSynchronized == true` while a child it
references is dirty. There is no aggregate state to drive a cascade or a safety check from, and any
future cascade needs a traversal with a cycle guard rather than a loop.

Relevant because it rules out the obvious fix for item 1 ("refuse to overwrite a resource that was not
fully loaded") — worth recording so nobody re-derives it.

---

## 4. `ListValues` omits a mapped property it failed to materialize

`Resource.ListValues` (`Trinity/Resource.cs:868`) unions three sources: the unmapped `_properties` bag,
mapped values that are set, and pending lazy-load URIs still in the per-instance `ResourceCache`. A value
absent from all three is skipped — and because `UpdateResource` deletes everything first, skipping it
means **deleting it**.

The `else if (ResourceCache.HasCachedValues(...))` branch covers the not-yet-materialized case, so this
is narrower than it first appears: it needs a mapped property that failed to marshal *and* left no cache
entry. We did not reproduce it in isolation, so this is reported as a hazard in the design rather than a
confirmed defect — omission-as-deletion has no safe failure mode and is worth an assertion either way.

---

## 5. In-memory `BeginTransaction` returns `null`

`dotNetRDFStore.BeginTransaction` (`Trinity/Stores/dotNetRDF/dotNetRDFStore.cs:450`) returns `null`,
while `VirtuosoStore.BeginTransaction` returns a real ADO.NET transaction handle.

Two costs: every caller must null-check, and transactional behaviour is **untestable** in the fast
in-memory fixture — a guard written there passes and means nothing. For a project whose test strategy is
"same suite against both stores", that is a silent coverage hole.

**Ask.** A no-op transaction object rather than `null`.

---

## 6. Blank-node children re-mint their id at commit

`StoreBase.cs:257` issues `SELECT BNODE()` on first commit of a blank-identified resource, so a parent
serialized beforehand no longer denotes the committed child; SPARQL INSERT templates also mint fresh
bnodes per execution. Effectively blank-node children must be committed before their parent, and
ordering matters beyond that.

**Not affecting us** — every Electrix OS project resource is an explicitly minted
`urn:{ns}:{kind}:{id}`. Reported because it is a live trap for other consumers and the exact behaviour
should be pinned by a test before anyone relies on it either way.

---

## Reproduction

The concurrency case is `ConcurrentAgentRunCreation_LandsEveryLinkOnTheSharedStepRun` in
`test/ToolTests/RuntimeValidationToolsTests.cs`, which runs against both stores. Reverting
`ToolBase.LinkToParent` to `AddUnique(...) + UpdateResource(parent)` reproduces 1-of-6 immediately.

A damaged production graph is attached to ADO 129003 as `waschanlage-01-…​.elxproj`.