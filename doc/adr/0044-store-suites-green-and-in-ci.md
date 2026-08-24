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

## Consequences

- **The whole suite is green**, on every backend:

  | suite | before | after |
  |---|---|---|
  | Virtuoso | 236 passed / 4 failed | **240 passed / 0 failed** |
  | GraphDB | 243 passed / 4 failed | **247 passed / 0 failed** |
  | Fuseki | 249 passed / 0 failed | 249 passed / 0 failed |
  | in-memory | 607 passed / 3 failed | 607 passed / 3 failed |
  | generator / vocabulary | 23 / 29 | 23 / 29 |

  The three in-memory failures are the pre-existing `UriRef` cases that fail only on a rolled-forward
  .NET 10 runtime (`doc/known-test-failures.md`); the test projects target net8.0 and CI installs it.

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
