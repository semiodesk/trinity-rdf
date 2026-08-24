# 0044. Provision the inference configuration the store suites depend on, and run them in CI

Date: 2026-08-24

## Status
Accepted (2.0).

## Context

After [0043](0043-fuseki-store-revival.md) the store suites stood at **Virtuoso 4 failed, GraphDB 4
failed, Fuseki 0**. All eight failures were the inferencing tests, and they had been red long enough
to read as settled facts about the backends. Both readings were wrong, and both were provisioning
gaps rather than store limitations — the same category of defect as the missing Fuseki dataset.

Confirmed pre-existing by running the two suites against a clean `develop` in a worktree: identical
counts and identical test names, before any of the 0043 work.

**Virtuoso** failed to compile the query at all:

```
SP031: SPARQL compiler: 'define input:inference refers to
undefined inference rule set "urn:semiodesk/test/ruleset"
```

`VirtuosoContainer` puts `rule=urn:semiodesk/test/ruleset` into the connection string, but nothing
creates that rule set. It was declared in `tests/Trinity.Tests.Virtuoso/ontologies.config` and built
by the configuration subsystem that **[0011](0011-configuration-model.md) retired**. The file is still
in the repository and nothing reads it: a dead declaration with a live reference to it.

**GraphDB** compiled the query and returned nothing. Its reasoner *is* configured — the container
POSTs a repository config carrying `graphdb:ruleset "rdfsplus-optimized"` — but the axiom it would
reason over was never loaded. `GetTypedResourcesWithInferencingTest` needs
`nco:PersonContact rdfs:subClassOf nco:Contact`, and the shared `TestOntologies` seeds rdf, rdfs, owl,
foaf and the space-test ontology — **not nco** — even though `nco.trig` sits in the same folder. The
per-store setups that the shared version replaced all loaded it.

0043 recorded GraphDB's failures as "an unresolved issue there rather than a declared limitation".
That was wrong, and this ADR corrects it: it was a missing seed graph.

## Decision

Four changes, each measured.

1. **Seed `nco.trig`** in `TestOntologies`. One line. GraphDB goes 4 failed → 0; the in-memory and
   Fuseki suites are unchanged, so nothing else depended on its absence.

2. **Create Virtuoso's rule set in the container**, the way `GraphDBContainer` creates its repository
   and `FusekiContainer` creates its dataset — `rdfs_rule_set` over the five schema graphs, via
   `isql` through Testcontainers' `ExecAsync`, failing loudly if it does not.

3. **Refresh the rule set after seeding**, via a new `IStoreTestSetup.AfterSeed(IStore)` hook with a
   default no-op implementation. This is the part that is easy to get wrong: `rdfs_rule_set` builds
   the rule set from a **snapshot of the graphs taken when it runs**, so registering it at container
   start — before anything is seeded — produces a rule set with no rules. Measured on a live server:

   | when `rdfs_rule_set` runs | entailed row returned |
   |---|---|
   | before the data is loaded | no |
   | re-registered, same name, after the data | yes |
   | fresh name, after the data | yes |

   Registering at container start is kept as well, so the name resolves and a query naming it compiles
   even if seeding fails — the difference between one clear error and a silently empty result.

4. **`SELECT DISTINCT` in `SparqlQueryTest.TestInferencing`.** The last Virtuoso failure was
   `Expected: 3, But was: 8`. The assertion counts contact media, but under inferencing
   `?m rdf:type nco:ContactMedium` is *entailed*, and SPARQL `SELECT` is a **bag**: a store that
   back-chains (Virtuoso) yields one solution per derivation path where one that materializes
   (GraphDB) yields one per subject. Neither is wrong, so the query has to say which it means. The
   three media have distinct dates, so `DISTINCT` collapses only the duplicates.

**And run the store suites in CI.** [0036](0036-integration-tests-testcontainers.md) excluded them for
"Docker availability + large image pulls". Both objections are answered: GitHub-hosted Linux runners
ship Docker, and this repository is **public**, so standard runners carry no minute cost. The third,
unstated objection — that they were red — no longer holds either. They run as a separate `stores`
job, a matrix over the three backends so each pulls only its own image, they run in parallel, and a
failure names the store in the check title. `fail-fast: false`, because the useful question is which
backends a change breaks, not whether any does.

### Seeding a multi-graph TriG file exposed a defect in the read path

`nco.trig` declares **two** named graphs — `nco#` (542 triples, including the class hierarchy) and
`nco_metadata#` (12). Both are *explicitly* named, but only one is easy to see: `nco_metadata#` uses an
absolute IRI label while `nco#` uses a **prefixed name** (`nco: { … }`), so a grep for `^<http…> {`
finds one and suggests the other block is the file's unnamed default graph. It is not, and the
distinction matters — see the third rule below.

Seeding this file made a defect that ADR-0043 had listed as a deliberate follow-up into a live one, so
it is fixed here rather than deferred again. Three things were wrong in the TriG branch of
`Read(Uri, Uri, …)`, in both `FusekiStore` and `GraphDBStore`:

- The delete targeted the caller's `graphUri` once **per iteration**, not the graph being written, so
  the second graph's turn deleted what the first had just written.
- `BaseUri` was never assigned per graph. The connector derives its target from it
  ([0038](0038-upgrade-dotnetrdf-3.md)), so every graph went to the **default** graph and the last one
  silently overwrote the rest. Measured on Fuseki before the fix: the store ended up with 12 triples in
  the default graph and **nothing** under `<nco#>`. The non-TriG branch had always assigned `BaseUri`
  for exactly this reason.
- Triples carrying **no graph name of their own** are written to `graphUri`, the graph the caller asked
  to read into. The first attempt at this fix skipped them, which would silently lose data from any
  TriG mixing unnamed triples with named ones — and `Read` returns the same URI either way, so the
  caller could not tell. `nco.trig` has no such triples, which is precisely why nothing in the suite
  would have caught it.
- Graphs are **grouped by target and merged** before writing, because two of them can resolve to one
  target — a file whose unnamed triples and one of its named graphs both belong in `graphUri`.
  `SaveGraph` is a PUT on these connectors, so writing twice would leave only the second: half the
  file gone, silently.

The routing lives in `StoreBase.GroupByTargetGraph`, not in each backend. It was written twice,
identically, and two copies of a routing rule drift into triples landing in the wrong graph on one
backend only — the hardest version of this bug to notice.

This is also the answer to "is GraphDB's 4→0 real, or an artifact of seed order?" — it is real. GraphDB's
`exists` is computed from `ListGraphs()`, which never reported `nco#` because `nco#` was never created,
so the delete never fired; the axioms reached the repository's default graph and GraphDB reasons
repository-wide. Verified directly against a live server, where `nco:PersonContact rdfs:subClassOf
nco:Contact` came back along with GraphDB's own materialized entailments. After the fix the graphs land
under their own names and survive re-reading, which `MultiGraphTrigTest<T>` now pins on both backends.

### Polymorphism is gated behind the flag

## Consequences

- **The whole suite is green**, on every backend:

  Totals included, so the next drift is self-evident: `passed / failed / skipped = total`. Measured on
  the branch as merged, not when this table was first written — it went stale twice inside the PR as
  tests were added, which is the argument for showing the total.

  | suite | before | after |
  |---|---|---|
  | Virtuoso | 236 / **4** / 1 = 241 | **241 / 0 / 1 = 242** |
  | GraphDB | 243 / **4** / 1 = 248 | **251 / 0 / 1 = 252** |
  | Fuseki | 249 / 0 / 1 = 250 | 253 / 0 / 1 = 254 |
  | in-memory *(.NET 10)* | 607 / 3 / 7 = 617 | 608 / 3 / 7 = 618 |
  | in-memory *(.NET 9)* | 610 / 0 / 7 = 617 | **611 / 0 / 7 = 618** |
  | generator / vocabulary | 23 / 29 | 23 / 29 |

  The "after" counts are higher than the "before" ones by more than the fixes: `MultiGraphTrigTest<T>`
  (three cases) and `ReadFromUrlTest` were added during review, which is also why this table was
  rewritten three times before it settled.

  **The in-memory row depends on the runtime, not on this change.** The assembly targets net8.0; rolled
  forward onto .NET 10 the three `UriRef` equality tests fail, onto .NET 9 they pass. Both rows are true
  and neither is a defect here — CI installs the net8.0 runtime, so it sees neither
  (`doc/known-test-failures.md`).

- **Inferencing is now genuinely covered on two backends.** Before this, no suite anywhere exercised a
  working reasoner, though for three different reasons:
  - **The in-memory store does not implement inferencing.** dotNetRDF ships reasoners and
    `dotNetRDFStore` holds one, but the feature was started and never finished: the flag is never
    read, no reasoner is created for the way these tests build the store, and the write path bypasses
    materialization regardless. Half-wired parts are not support. The three-gap diagnosis is in
    `doc/known-test-failures.md`.
  - **Fuseki** cannot switch inference per query at all ([0043](0043-fuseki-store-revival.md)).
  - **Virtuoso and GraphDB** were both misconfigured, which is what this ADR fixes.

  So `inferenceEnabled` was, in effect, untested everywhere.

- **`IStoreTestSetup` gains a member.** It is a default-implemented interface method, so the other
  setups are untouched, but the contract is no longer three members.

- `tests/Trinity.Tests.Virtuoso/ontologies.config` is now doubly dead — nothing reads it, and the rule
  set it declared is created in code. It should be deleted along with the other `ontologies.config`
  leftovers; not done here to keep this change to one subject.

- The `stores` job adds roughly 2 GB of image pulls per run. Free on a public repository, but not free
  in wall-clock: expect the matrix to take a few minutes. It does not gate the fast `build` job.

## Related
- [0043](0043-fuseki-store-revival.md) — the Fuseki revival, and the same class of provisioning defect
- [0036](0036-integration-tests-testcontainers.md) — Testcontainers, and the CI exclusion this reverses
- [0011](0011-configuration-model.md) — the retired configuration subsystem that used to build the rule set
- [0022](0022-store-capabilities-and-istore-extension.md) — inferencing as a per-store capability
