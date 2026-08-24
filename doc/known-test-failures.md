# Known test failures (quarantined)

These `Trinity.Tests` cases are `[Ignore]`d so the suite is green on CI while the underlying
issues are tracked. None are regressions from the SDK/CPM modernization (Milestone 1) — they
are pre-existing or net472→net8 runtime-behavior differences. Revisit as noted.

| Test | Bucket | Why | Follow-up |
|---|---|---|---|
| `LinqTestBase.CanExecuteCollectionWithInferencingEnabled` | capability | `Query<Agent>(inferenceEnabled: true)` returns only the explicitly-typed resource (1 of 6): the in-memory store **does not implement inferencing** — see the diagnosis below | Feature, not a bug (ADR-0022 lets a store ignore the flag). Overlaps the polymorphic-query decision in ADR-0037 |
| `LinqTestBase.CanExecuteScalarWithInferencingEnabled` | capability | as above | as above |
| `LinqTestBase.CanSelectResourcesWithOperatorTypeOf` | semantics | Needs **polymorphic base-type queries**: its last assertion expects `Query<Agent>()` to also return resources typed with a subclass (`Person`). `is T`, `GetType() == typeof(T)` and `OfType<T>().Count()` are all implemented now — only that assertion fails | **Open decision** (ADR-0037): `GetTypes()` emits a class's own `[RdfClass]` only, so a `Person` is not typed `foaf:Agent`. Either expand a base-type constraint to a UNION over registered subclasses, or leave it to store-side `rdfs:subClassOf` inference |
| `ResourceWriteSemanticsTest.CanRemoveBlankNodeValuedLink` | defect (read path) | Reading a mapped collection whose value is a **blank node** throws `RdfParseException: "Cannot resolve a Relative URI Reference since there is no in-scope Base URI"` from dotNetRDF's expression parser while it resolves the lazy-load filter. Confirmed to fail *before* the write by cutting the test short — so it is a read-path limitation, not a write-semantics one | Fix blank-node handling in the lazy-load query. Until then the hazard the test was written for is **uncovered**: blank nodes are illegal in SPARQL `DELETE` templates, and delta writes (ADR-0039) name triples directly where the old whole-resource rewrite deleted through variables. Extends item 6 of `doc/trinity-write-semantics.md` |

The 3 `LinqTestBase` cases run under both `LinqModelTest` and `LinqModelGroupTest` (6 results); with the
blank-node case that is the entire quarantined set in `Trinity.Tests`. The blank-node entry lives in the
shared `ResourceWriteSemanticsTest<T>` fixture, so it is also skipped once per store suite.

Of these, the three LINQ entries are an unimplemented capability or an open design decision. **The
blank-node entry is a genuine defect** — the first quarantined case that is. No quarantined test is a
missing LINQ translation or a datatype bug.

### Store suites: what each backend skips, and why

These are `Assert.Inconclusive` overrides in the per-store fixtures, not `[Ignore]`s, and each names a
store limitation rather than a Trinity defect. The shared blank-node case above is skipped once per
store on top of these.

| Store | Skipped | Why |
|---|---|---|
| **Fuseki** | `TestInferencing`, `GetTypedResourcesWithInferencingTest`, `MappingTypeWithInferencingTest`, `MappingTypeCollectionWithInferencingTest` | Fuseki has **no per-query inference switch**: a Jena reasoner is a property of the dataset, so it applies to every query or to none. Giving the test dataset a reasoner would make these four pass and make `inferenceEnabled: false` quietly lie. ADR-0022 makes inferencing a capability a store may ignore; ADR-0043 records the decision |
| **Virtuoso** | `Int64Test`, `Uint64Test`, `Int16Test`, `Uint16Test`, `UintTest`, `TimeSpanTest`, `TimeSpanResourceTest` | Virtuoso widens the small integer types into an integer box and does not support `xsd:long`/`xsd:duration`. The `Test<TValue>` helper reads the **unmapped** bag, which declares no target type to convert into (ADR-0040) |
| **GraphDB** | — | none |

**No store suite has a failing test.** Virtuoso and GraphDB each carried four *failing* inferencing
tests until ADR-0044; both turned out to be provisioning gaps — Virtuoso's rule set was declared only
in the `ontologies.config` that ADR-0011 retired, and GraphDB's reasoner had no `nco` class hierarchy
to work from because the shared `TestOntologies` never seeded it. Fuseki's four remain inconclusive
because it genuinely cannot switch inference per query.

Fuseki additionally requires a **5.x server**. Jena 4.x answers HTTP 500 *"Not a valid UUID string"* to
any query naming a `urn:uuid:` IRI — which is what `Model.CreateResource()` mints by default — so on
4.x such a resource is writable but permanently unreadable (ADR-0043).

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
