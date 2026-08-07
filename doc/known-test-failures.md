# Known test failures (quarantined)

These `Trinity.Tests` cases are `[Ignore]`d so the suite is green on CI while the underlying
issues are tracked. None are regressions from the SDK/CPM modernization (Milestone 1) — they
are pre-existing or net472→net8 runtime-behavior differences. Revisit as noted.

| Test | Bucket | Why | Follow-up |
|---|---|---|---|
| `LinqTestBase.CanExecuteCollectionWithInferencingEnabled` | capability | `Query<Agent>(inferenceEnabled: true)` returns only the explicitly-typed resource (1 of 6): the in-memory store **does not implement inferencing** — see the diagnosis below | Feature, not a bug (ADR-0022 lets a store ignore the flag). Overlaps the polymorphic-query decision in ADR-0037 |
| `LinqTestBase.CanExecuteScalarWithInferencingEnabled` | capability | as above | as above |
| `LinqTestBase.CanSelectResourcesWithOperatorTypeOf` | semantics | Needs **polymorphic base-type queries**: its last assertion expects `Query<Agent>()` to also return resources typed with a subclass (`Person`). `is T`, `GetType() == typeof(T)` and `OfType<T>().Count()` are all implemented now — only that assertion fails | **Open decision** (ADR-0037): `GetTypes()` emits a class's own `[RdfClass]` only, so a `Person` is not typed `foaf:Agent`. Either expand a base-type constraint to a UNION over registered subclasses, or leave it to store-side `rdfs:subClassOf` inference |

These 3 `LinqTestBase` cases run under both `LinqModelTest` and `LinqModelGroupTest` (6 results),
which is the entire quarantined set. **Every remaining entry is an unimplemented capability or an open
design decision — none is a defect.** No quarantined test is a missing LINQ translation or a datatype
bug any more.

### Why in-memory inferencing does not work (diagnosed on dotNetRDF 3.5.2)
Three independent gaps, any one of which would be enough:
1. **The flag is ignored.** `dotNetRDFStore` never reads `inferenceEnabled` — it accepts the parameter
   and drops it, so `Query<Agent>(true)` emits exactly the same SPARQL as `Query<Agent>()`.
2. **No reasoner is ever created** in these tests. `dotNetRDFStore` only builds an `RdfsReasoner` when
   the connection string carries a `schema=` key; the tests use plain `provider=dotnetrdf`.
3. **Even with a reasoner it would not help.** dotNetRDF materializes inference when a graph is *added*
   to the store (`AddInferenceEngine` + `Add`), but Trinity writes data via SPARQL UPDATE through
   `LeviathanUpdateProcessor`, which bypasses the store's inference engine entirely.

What the tests want is `rdfs:subClassOf` reasoning (`foaf:Person`/`foaf:Group` ⊑ `foaf:Agent`) gated
behind `inferenceEnabled` — **the same capability as the polymorphic base-type query** that
`CanSelectResourcesWithOperatorTypeOf` wants ungated (ADR-0037). Implementing it is a design choice:
query-time expansion (e.g. `?s rdf:type/rdfs:subClassOf* <T>`, which needs the schema axioms in the
queried dataset — today the ontologies live in separate graphs from the model) versus materializing
inferred triples on write. Deciding it would resolve all three remaining quarantined cases at once.

**Resolved by the LINQ provider rebuild (ADR-0037):** `CanSelectResourcesFromQuerySourceProperty`
(returned 1 of N resources), `ProjectionTest` (emitted invalid SPARQL) and `SelectAdditionalFrom`
(the multiple-`from` query form) now pass and are no longer `[Ignore]`d.

### Note: three URI-equality tests fail on the .NET 10 *runtime*
`UriRefTest.EqualsTest`, `ResourceTest.Equal` and `ResourceTest.ResourceConstructorTest` pass on the
targeted **net8.0** runtime (Windows and Linux alike) but fail if the same assembly is rolled forward
onto the .NET 10 runtime — a URI with a fragment then compares *equal* to the same URI without one,
which is exactly what `UriRef` exists to prevent (ADR-0025). Not currently reachable: the test projects
target net8.0 and CI installs that runtime explicitly. Worth investigating before retargeting the tests
to a newer TFM.

**Resolved by fixing the `DateTime` deserializer (ADR-0026):**
`CanSelectDateTimeWithBinaryExpression` — a stored UTC `DateTime` read back one hour off in UTC+1
because `DateTime.TryParse` with default styles shifts a `…Z` value into the local zone. Now parsed
with `DateTimeStyles.RoundtripKind` + `InvariantCulture`. It was neither a net8 nor a dotNetRDF 3.x
behaviour change — it reproduced identically on both engine versions.

The former config cases (`DotNetRDFStoreTest.LoadOntologiesTest`, `LegacyConfigurationTest`) were
**deleted**, not ignored, when the configuration subsystem was retired (ADR-0011).

Update (Milestone 2 done): `Trinity.Tests` is now fully driven by the Roslyn source generator
(`Trinity.Generator`) — every mapped model class is `partial` — so the whole suite passes
cross-platform with **no** IL weaving. The cilg post-build weave, the `[Category("Weaver")]`
split, and the `-p:WeaveTests=false` harness have been removed.
