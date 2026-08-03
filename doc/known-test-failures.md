# Known test failures (quarantined)

These `Trinity.Tests` cases are `[Ignore]`d so the suite is green on CI while the underlying
issues are tracked. None are regressions from the SDK/CPM modernization (Milestone 1) — they
are pre-existing or net472→net8 runtime-behavior differences. Revisit as noted.

| Test | Bucket | Why | Follow-up |
|---|---|---|---|
| `LinqTestBase.CanExecuteCollectionWithInferencingEnabled` | inferencing | In-memory inferencing not applied — the query returns only the explicitly-typed resource (1 of 6) | Pre-existing store-level issue, not LINQ (git: "Only inferencing is not working"); ADR-0022 |
| `LinqTestBase.CanExecuteScalarWithInferencingEnabled` | inferencing | as above | as above |
| `LinqTestBase.CanSelectResourcesWithOperatorTypeOf` | semantics | Needs **polymorphic base-type queries**: its last assertion expects `Query<Agent>()` to also return resources typed with a subclass (`Person`). `is T`, `GetType() == typeof(T)` and `OfType<T>().Count()` are all implemented now — only that assertion fails | **Open decision** (ADR-0037): `GetTypes()` emits a class's own `[RdfClass]` only, so a `Person` is not typed `foaf:Agent`. Either expand a base-type constraint to a UNION over registered subclasses, or leave it to store-side `rdfs:subClassOf` inference |
| `LinqTestBase.CanSelectDateTimeWithBinaryExpression` | datetime | Round-trips one hour off (`1948-02-04 00:00` stored, `01:00` read) in a UTC+1 tz | **Real datatype bug, not LINQ** — unspecified-`Kind` `DateTime` is treated as local on write and UTC on read (or vice versa); fix in `XsdTypeMapper` (ADR-0026) |

These 4 `LinqTestBase` cases run under both `LinqModelTest` and `LinqModelGroupTest` (8 results),
which is the entire quarantined set. **No quarantined test is a missing LINQ translation any more** —
what remains is one semantics decision, one datatype bug and two store-level inferencing issues.

**Resolved by the LINQ provider rebuild (ADR-0037):** `CanSelectResourcesFromQuerySourceProperty`
(returned 1 of N resources), `ProjectionTest` (emitted invalid SPARQL) and `SelectAdditionalFrom`
(the multiple-`from` query form) now pass and are no longer `[Ignore]`d.

The former config cases (`DotNetRDFStoreTest.LoadOntologiesTest`, `LegacyConfigurationTest`) were
**deleted**, not ignored, when the configuration subsystem was retired (ADR-0011).

Update (Milestone 2 done): `Trinity.Tests` is now fully driven by the Roslyn source generator
(`Trinity.Generator`) — every mapped model class is `partial` — so the whole suite passes
cross-platform with **no** IL weaving. The cilg post-build weave, the `[Category("Weaver")]`
split, and the `-p:WeaveTests=false` harness have been removed.
