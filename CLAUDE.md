# CLAUDE.md

Guidance for Claude Code (and humans) working in this repository.

## What this is

**Semiodesk.Trinity** — an enterprise object mapper for building RDF knowledge-graph
applications in .NET. It maps RDFS/OWL terms to POCOs (`Resource` + `[RdfClass]`/
`[RdfProperty]`), sits on top of **dotNetRDF**, and offers LINQ-to-SPARQL and pluggable
store backends. Original author: Moritz Eberl (Semiodesk). After several unmaintained years
it is being **revived on modern .NET as a breaking 2.0**: the mapping is now produced by a
Roslyn **source generator** (not the old IL weaver), so builds are cross-platform with no
post-build tooling. Read `doc/adr/README.md` for the decisions and history.

## Working agreement

- **Stability before features.** Prefer small, reviewable, well-scoped changes. Confirm
  before broad refactors. Keep decisions in `doc/adr/` (add an ADR for significant choices).
- **2.0 is a deliberate breaking release.** The public runtime surface (`Resource`,
  `[RdfClass]`/`[RdfProperty]`, `IStore`/`IModel`, the stores, Turtle/JSON-LD I/O) is
  preserved, but the **authoring model changed**: mapped classes and their `[RdfProperty]`
  properties must now be `partial` (C# 13 / .NET 9+). The two consumers — ElectrixOS
  (`C:\Projects\elxgen`) and DevHub/Relay (`C:\Projects\DevHub`) — pin the old published
  **1.0.3.77** and will migrate to 2.0 on their own schedule; don't contort 2.0 to keep them
  building on the old style.
- **Verify, don't assume.** Run/build/observe before claiming something works — the whole
  suite is `dotnet test`-able now, so use it.

## Repo map

| Project | TFM | Role |
|---|---|---|
| `Trinity` | netstandard2.0 | Core: `Resource`, `PropertyMapping<T>`, `IStore`/`IModel`, `StoreFactory`, LINQ, in-memory + SPARQL-endpoint stores |
| `Trinity.Generator` | netstandard2.0 | **Roslyn source generator** (ships as an analyzer in the package): emits `PropertyMapping<T>` fields + `GetValue`/`SetValue` + `GetTypes()` for `partial` `[RdfClass]`/`[RdfProperty]` classes |
| `Trinity.Virtuoso` | netstandard2.0 | Virtuoso backend — OpenLink provider vendored as a self-recompiled netstandard2.0 DLL (cross-platform) |
| `Trinity.GraphDB` | netstandard2.0 | GraphDB backend |
| `Trinity.Fuseki` | netstandard2.0 | Fuseki backend |
| `Trinity.Oxigraph` | netstandard2.0 | Oxigraph backend — own `OxigraphConnector` over `SparqlHttpProtocolConnector` (`/store`, `/query`, `/update`) |
| `Trinity.Vocabulary` | netstandard2.0 | Vocabulary **parse+emit engine** — reads RDF, emits the `Ontology` classes `OntologyDiscovery` reflects on (ADR-0014) |
| `Trinity.Vocabulary.Cli` | net8.0 | `dotnet tool` front end, command `trinity-vocab`, package `Semiodesk.Trinity.Vocabulary.Tool` |
| `Trinity.Tests` | net8.0 | NUnit in-memory suite (fully generator-driven, no weaver) |
| `tests/Trinity.Generator.Tests` | net8.0 | Source-generator validation, incl. the TRIN diagnostics |
| `tests/Trinity.Vocabulary.Tests` | net8.0 | Vocabulary generator + `trinity-vocab`: term classification, all four RDF formats, determinism, sanitization/collisions, manifest reading, the check-mode exit codes, a member-compatibility check against the committed vocabularies, and a round-trip that compiles generated source and asserts `OntologyDiscovery` finds it |
| `tests/Trinity.Tests.{Virtuoso,Fuseki,GraphDB,Oxigraph}` | net8.0 | Store integration tests — self-provision the server via Testcontainers/Docker (ADR-0036); run in the `stores` CI matrix job, not the fast `build` job |
| `doc/adr/` | — | Architecture Decision Records |

Retired in 2.0: `Trinity.CilGenerator` (the cilg weaver, ADR-0013), `Trinity.OntologyGenerator`
(the net4x vocab tool — replaced by `Trinity.Vocabulary` + `trinity-vocab`, ADR-0014), the
`build/Semiodesk.Trinity.targets`, `build.cake`/`appveyor.yml`, and the `Documentation` docfx
project (removed from the solution; docs build separately).

## Build & test

Prereq: **.NET 10 SDK only** — no .NET Framework targeting packs, no Visual Studio. Everything
is netstandard2.0 / net8.0 and builds cross-platform.

```bash
dotnet build Semiodesk.Trinity.sln -c Release          # whole solution, SDK-only
dotnet test Trinity.Tests/Trinity.Tests.csproj         # 940 passed, 3 skipped (quarantined), 0 failed
dotnet test tests/Trinity.Generator.Tests/Trinity.Generator.Tests.csproj   # 42 passed
dotnet test tests/Trinity.Vocabulary.Tests/Trinity.Vocabulary.Tests.csproj # 29 passed
dotnet pack Trinity/Trinity.csproj -c Release          # -> Semiodesk.Trinity.2.0.0.nupkg
```

- The 3 skipped tests are `[Ignore]`d and tracked in `doc/known-test-failures.md`: one open semantics
  decision counted twice (polymorphic base-type queries, ADR-0037), and **removing** a blank-node-valued
  link (ADR-0039) — the only quarantined case that is an outright defect. Its *read* half was fixed by
  ADR-0046 and is covered by `CanReadBlankNodeValuedLink`; what remains is that a blank node is not legal
  in a SPARQL `DELETE` template. No missing LINQ translation or datatype bug remains, and none are
  generator regressions.
- Store integration tests (`tests/Trinity.Tests.*`) **self-provision** their server in Docker via
  Testcontainers on a random host port (ADR-0036): run `dotnet test tests/Trinity.Tests.{Virtuoso,GraphDB,Fuseki,Oxigraph}`
  with a Docker daemon running. **They run in CI** as the `stores` matrix job (ADR-0044); the ADR-0036
  exclusion no longer applies, because GitHub-hosted runners ship Docker and this repo is public, so
  standard runners are free. The fast `build` job still runs only the in-memory suites, so a Docker
  hiccup cannot redden it. Current: **all four green** — Oxigraph 357/358, Fuseki 356/357,
  GraphDB 355/356, Virtuoso 339/340 (0 failed each; the 1 skipped is the shared blank-node-removal
  quarantine).

  The eight inferencing failures that stood here until ADR-0044 were **provisioning gaps, not store
  limitations**: Virtuoso's rule set was declared only in the `ontologies.config` that ADR-0011 retired,
  and GraphDB's reasoner had no `nco` class hierarchy to reason over because the shared `TestOntologies`
  never seeded it. Fuseki's four remain inconclusive — it has no per-query inference switch (ADR-0022).

  Fuseki's long-standing "4/86, blocked on an upstream `FusekiConnector` bug" was a **misdiagnosis**
  (ADR-0043): the test container never created a dataset (`FUSEKI_DATASET_1` belongs to a different image
  and is ignored), so every path under `/ds/*` 404d. It now runs the same shared fixtures as GraphDB and
  needs **Fuseki 5.x** (Jena 4.x answers HTTP 500 to any query naming a `urn:uuid:` IRI, which is what
  `CreateResource()` mints by default).
  Virtuoso's `Int16Test`/`Uint16Test`/`UintTest` are now `Assert.Inconclusive` in `VirtuosoResourceTest`,
  alongside the pre-existing `Int64Test`/`Uint64Test` overrides for the same phenomenon: Virtuoso widens
  `xsd:short`/`xsd:unsignedShort`/`xsd:unsignedInt` into an integer box, and the `Test<TValue>` helper reads
  the *unmapped* bag and casts with `(TValue)`. Not a mapping defect — a mapped property declares a target
  type so Trinity converts into it (ADR-0040), whereas the unmapped bag declares nothing. If `ListValues`
  is ever given a CLR-type-fidelity guarantee, they must come back.
- **CI:** `.github/workflows/ci.yml` (ubuntu, .NET 10) — a fast `build` job (restore → build → test →
  coverage → pack), a `duplication` job, a `stores` matrix job running the four Dockerized store suites (ADR-0044),
  and a `coverage` job merging both kinds of report.
  NuGet publishing is **manual** (no publish job).
- **Coverage** is collected by the collector bundled with `Microsoft.NET.Test.Sdk` (no package or tool
  to add), merged by `.github/scripts/coverage.py`, printed to the job summary as a per-assembly table,
  and **gated at a 78% floor**. Two things about that number are easy to get wrong. The three
  **in-memory** suites the `build` job runs overlap — core is exercised by all of them — so reports are
  merged by taking the highest hit count per `(file, line)`; summing totals would count shared lines
  repeatedly. And **test assemblies are excluded**: they are ~96% covered by construction, and counting
  them reported 88.9% where the product was at 80.9%. The floor sits deliberately *below* the current
  figure rather than at it — a ratchet pinned to the exact value turns any honest refactor that deletes
  well-covered code red. The floor lives in the script (as the ceiling and the jscpd pin live in
  `duplication.py`), so CI and the local check below cannot disagree about it.
  Only **product source files of this repo** count, decided by path, not assembly: the store suites
  also instrument **Testcontainers** (its package ships symbols — 2930 lines, a merged 69.8%), and
  rewriting its `/_/src/…` build paths to repo-relative makes them *look* like product files, so a
  file must also be one git knows (tracked or untracked-not-ignored).
  The **store suites' coverage** (`--stores`; merged in CI by the `coverage` job) is **reported, not
  gated**, one row per adapter and no merged grand total — adapters entering the denominator would pull
  it below the gated figure for no reason. A floor there now would mostly measure
  `Trinity.Virtuoso/VirtuosoManager.cs`, a vendored connector whose unused API is ~400 of Virtuoso's
  ~550 uncovered lines; trim it before adding one. An adapter counts only from **its own** suite, and a
  **failed** suite's report is dropped — partial data makes covered code look uncovered.
- **Duplication** is measured by jscpd (pinned `4.3.0`, run via `npx` — no install) over **product code
  only**, comments ignored; scope lives in `.jscpd.json`. `.github/scripts/duplication.py` counts the
  lines in a clone on **either side** — 9.6% when added, where jscpd's own headline says 5.4% because it
  counts roughly one side — and **gates at a 10.5% ceiling**, above the current figure for the coverage
  floor's reason inverted (deleting unduplicated code raises the share). Both sides count because that is
  this codebase's recurring defect: a fix landing in one copy. The store adapters are the bulk of it —
  Fuseki is 67% cloned, mostly with Oxigraph, and the `AbsoluteUri` quarantine is exactly a fix present
  in Oxigraph's copy only.
- **Changed-line reports** (`--diff <rev>` on both scripts; CI passes `HEAD^1` on pull requests and
  annotates the diff): which changed product lines no test covers, which edits landed on **one side of
  a clone only**, and which new code repeats existing code. Reported, **never gated**. Two things are
  load-bearing. One-sided edits are judged against clones **scanned at the base revision**: editing one
  copy is exactly what stops the copies matching, so a scan of the result no longer contains the clone —
  the first version scanned the result and missed every real edit, finding only comment-only ones. And
  a changed file nothing measured is listed **with the reason** (its store suite did not run, failed,
  or nothing loads the file) rather than counted uncovered or dropped. On PRs the `coverage` job makes
  this report, so annotations wait for the slowest store leg; it runs even when a leg failed.
- **Local check:** `.github/scripts/check.sh [<rev>]` (default `HEAD`, i.e. everything uncommitted,
  untracked files included) runs the `build` and `duplication` jobs' gates plus both changed-line
  reports, and the **store suites the change affects** (`.github/scripts/stores.py`, the one store
  list): an adapter or its test project runs that suite; core, the generator, `Trinity.Tests/` (the
  shared fixtures) or a root build file runs **all four** — about 82% of commits. They start in the
  background after the build, 300 s limit each. Without Docker or a pinned image a suite is reported
  *not run*, never failed; once it runs, its failures fail the check. Its in-memory suite list must stay
  in step with CI's Test step.
  **Claude Code runs it before every `git commit`** (`.claude/settings.json` →
  `.claude/hooks/pre-commit.sh`): a failing gate blocks the commit and hands Claude the report; a
  passing one hands it the report as context. Markdown-only changes skip it. The hook's `if:
  "Bash(git commit *)"` is only a pre-filter — documented best-effort, it runs the hook for any command
  with a variable it cannot resolve, where a failing gate would *block an unrelated command* — so the
  script re-checks the command itself. And a hook that outlives its timeout is **killed and the command
  proceeds, silently**, so the script bounds the check at 840 s (under the 900 s hook limit) and blocks
  when that is exceeded. To bypass it deliberately,
  disable the hook via `/hooks` — there is intentionally no in-command escape hatch Claude could use.
- Central Package Management: versions live in `Directory.Packages.props`; shared metadata +
  the single `Version` (2.0.0) in `Directory.Build.props`. Projects use versionless `PackageReference`.

## Mapping (how 2.0 works)

Author a `partial` class deriving from `Resource`, decorate it with `[RdfClass(uri)]` and give
each mapped property `[RdfProperty(uri)]` **and** the `partial` keyword (no body):

```csharp
public partial class Person : Resource
{
    public Person(Uri uri) : base(uri) { }
    [RdfProperty("http://xmlns.com/foaf/0.1/name")]
    public partial string Name { get; set; }
}
```

`Trinity.Generator` supplies the implementing half at compile time: a `protected
PropertyMapping<T>` field, the getter/setter calling `GetValue`/`SetValue`, and a `GetTypes()`
override from `[RdfClass]`.

**Authoring mistakes are diagnostics, not silence.** Each of these compiles fine and produces no
mapping at all, so they used to surface only as a query returning nothing at runtime — all are
warnings, declared in `Trinity.Generator/AnalyzerReleases.Unshipped.md`. The class-level checks are
**independent** — a class that is neither `partial` nor has a `(Uri)` constructor reports both, because
someone migrating a large model wants the whole list from one build:

| Id | Fires when |
|---|---|
| `TRIN001` | `[RdfProperty]` on a property that is not `partial` |
| `TRIN002` | a mapped class is not `partial` — fires for `[RdfClass]` **and** for a class that merely has `[RdfProperty]` members, once per class, independently of the property-level `TRIN001` |
| `TRIN003` | the mapped type is nested rather than top-level |
| `TRIN004` | a mapped class does not derive from `Resource` |
| `TRIN005` | a mapped class has no accessible `(Uri)` constructor, so `Activator.CreateInstance(type, uri)` cannot materialize it when reading |
| `TRIN006` | a URI belongs to a **generated** vocabulary but is not one of its terms — a typo. Only vocabularies marked `[GeneratedCode("trinity-vocab", …)]` are trusted, since only those list every term; an unknown namespace is never reported |
| `TRIN009` | a localized-text container property declares a setter. The container is a mutable view owned by the mapping; assigning one either nulls it or aliases another resource's instance |
| `TRIN010` | a mapped property is typed with an `ILocalizedText` that is not `LocalizedString` or `LocalizedStringCollection`. The interface is their shared surface, **not an extension point** — the engine dispatches on the two concrete types — so anything else is refused at registration. The check asks the type *and* its interfaces, because `AllInterfaces` alone never includes the type itself, which made `ILocalizedText` (the one shape that cannot work at all, since `Activator` cannot instantiate an interface) the one shape no diagnostic saw |
| `TRIN008` | `[RdfProperty]` passes the obsolete `languageInvariant` flag. It no longer has any effect — the declared type decides (ADR-0048) — and a flag that silently does nothing is indistinguishable from one that works, so it is reported rather than ignored |
| `TRIN007` | a mapped property is typed `System.Uri` (or is a collection of them) instead of `UriRef` — `Uri` equality ignores the fragment. **The one diagnostic that does not mean "nothing was generated"**: the mapping is emitted, the declared type is wrong. Matched on exact type identity, so `UriRef` — which derives from `Uri` — is not flagged. `PropertyMapping<T>` **throws** for `System.Uri` at runtime (Release too), which is what catches hand-written mappings the generator never sees |

The generator handles scalars, collections (seeded with a default instance), localized-text
containers (seeded likewise, by `PropertyMapping<T>`), resource references, multiple `[RdfClass]`, and inheritance (including `GetTypes`-only
subclasses). The implementing half copies the declaring declaration's **modifiers and accessors verbatim** — it
declares exactly the accessors the declaration did, each with its own modifiers, or it is CS9253 — so
accessibility and `new`/`virtual`/`override`/`sealed` match — C# requires both halves to agree, and
hiding a `Resource` member (`Model`, say — a car has one) needs `new` on both or it is an unfixable CS8800. Only `partial` members are processed. The runtime engine (`Resource`,
`PropertyMapping<T>`, reflective `InitializePropertyMappings`) is unchanged from 1.x.

## Vocabularies (ADR-0014)

Vocabulary classes are generated by an author-time tool, not at build time, and the output is
committed:

```bash
dotnet tool install -g Semiodesk.Trinity.Vocabulary.Tool
trinity-vocab vocabularies.json            # write the generated file(s)
trinity-vocab vocabularies.json --check    # exit 3 if committed output is stale (for CI)
```

The manifest lists local RDF files with their prefix and namespace URI; `WebSource` and
`MetadataSource` from the 1.x `ontologies.config` are gone, so resolve remote vocabularies to local
files yourself. Emitted names are public API and reproduce 1.x exactly — notably keywords are
prefixed with `_` (`rdf:object` → `_object`), and terms that are neither class nor property are
emitted as `Resource` (`rdf:nil`, the datatypes).

The generator is **optional and stays that way**: `Trinity.Tests` has **28 hand-written** vocabulary
classes and **2 generated** ones (`dces`, `owl` — see `Trinity.Tests/Ontologies/vocabularies.json`).
Each vocabulary is emitted to its own `<prefix>.g.cs`, so regenerating one never rewrites another and
`--check` names the file that drifted. Both of a vocabulary's classes go in that one file: a file per
*class* would give `dces.g.cs` and `DCES.g.cs`, which collide on Windows. The two classes are both
required — attribute arguments must be constants, so `[RdfProperty(FOAF.age)]` needs the `const string`
companion, while the typed class is what `OntologyDiscovery` and runtime code use.
`OntologyDiscovery` cannot tell them apart, and `OntologyTest.DiscoversHandWrittenAndGeneratedVocabulariesAlike`
asserts both routes stay first-class. The hand-written `dc` and generated `dces` deliberately cover the
same namespace with the same terms, which is the plainest demonstration that they are equivalent. CI runs
`trinity-vocab --check` so the two committed generated files cannot drift.

`OntologyDiscovery` finds vocabularies **by reflection, not by an interface**: the class must derive
*directly* from `Ontology`, have a parameterless constructor, and expose static fields named exactly
`Prefix` and `Namespace` (`Trinity/OntologyDiscovery.cs:93,124-133`). Nothing enforces this at compile
time, so the round-trip test in `tests/Trinity.Vocabulary.Tests` — which compiles generated source and
asserts discovery registers it — is the guard.

## Mental model (grounding decisions — ADR-0016…0035)

Invariants that surprise newcomers:
- **Resource-centric, not triple-centric** (0016): you load/mutate/persist whole `Resource`
  objects. There is no API to add or remove a single triple.
- **Resources are open** (0017): a mapped resource can still be annotated with arbitrary
  predicates at runtime; `ListValues` returns mapped *and* unmapped properties.
- **Attributes are only sugar** (0018): the real mapping is the `PropertyMapping<T>` field +
  `GetValue`/`SetValue` + `GetTypes()`, now emitted by the source generator (ADR-0013). A
  `[RdfProperty]` on a non-`partial` member is not generated (and no longer woven) — it does nothing.
- **Models are named graphs; a `ModelGroup` is itself an `IModel`** spanning several (0019).
- **A `ModelGroup` can only union; subtraction is a separate abstraction** (0041): `ILayeredModel`
  (`store.CreateLayeredModel(baseline, additions, removals)`) reads `(baseline − removals) ∪ additions`
  with git-working-tree semantics — additions win on overlap, the baseline stays untouched. It is read-only,
  and it is deliberately **not** an `IModelGroup`. The mapped reads and LINQ honour the overlay natively; caller-supplied
  SPARQL is rewritten by `OverlayQueryRewriter` (whitelist over the dotNetRDF parse tree — property paths, `GRAPH`
  blocks, `SERVICE`, `CONSTRUCT`/`DESCRIBE`, `FILTER EXISTS` and a caller `FROM` are **refused**), and
  `inferenceEnabled: true` throws. Nothing silently returns unsubtracted triples. **dotNetRDF cannot serialize a
  negated comparison faithfully** (`!(?r < 3)` → `!?r < 3`, which still parses), so `SparqlExpressionWriter` writes
  `FILTER`/`BIND` expressions instead; `HAVING`, projections, `GROUP BY` and `ORDER BY` come from dotNetRDF and are
  only checked, so a negation there is refused. Every rewrite is re-parsed and compared structurally against the
  original. The overlay's two branches are **disjoint by construction** — `UNION` is a bag union, so overlapping
  branches would duplicate a re-added triple. Blank nodes (incl. `[ … ]`) are refused: the overlay repeats each
  pattern across three BGPs and SPARQL forbids a label spanning them.
  Caller queries are parsed on **every** execution — see ADR-0041 for the placeholder-IRI caching optimisation
  if that ever shows up in a profile. The SPARQL is
  built only by `LayeredModelSparql`, whose emission rules (MINUS vs FILTER NOT EXISTS, `VALUES` before the
  overlay, selective patterns before the wildcard) are correctness/perf requirements — see the ADR before touching them.
  **All three graphs must be in the same store** — the overlay is one query over one dataset, so a cross-store view
  is refused at construction (it used to fail silently in both directions); create views only via
  `store.CreateLayeredModel`, never by constructing `LayeredModel` directly.
- **A layered view is a working copy you stage into** (0042): `Commit()` on a resource read through one
  **stages** into the additions/removals graphs rather than writing — it was a silent no-op before — and
  `Accept()`/`Discard()` apply or abandon. Accept applies removals before additions (additions win, as on
  read) and **refuses** when a triple staged for removal is no longer in the baseline, because a stale
  changeset never fails on its own: it merges, leaving two values for a single-valued property that a
  mapped read then hides. `Accept(force: true)` overrides. Deleted values route conditionally — un-stage
  from additions, add to removals only if the baseline holds it — which is what keeps the ancestor
  reconstructible. Never write `WHERE { FILTER … }` with no pattern: **Virtuoso ignores a filter-only
  WHERE** and applies the operation unconditionally.
- **A layered view can be materialized** (0042): pass a fourth graph to `store.CreateLayeredModel(...)` and the
  effective triples are kept there, so queries run natively — which lifts every refusal that existed *for want of a
  faithful rewrite*, including unbounded property paths and inferencing. **Graph selection is not one of those and
  stays refused in both modes**: the view's `FROM` is *appended to* a caller's rather than replacing it, so a caller
  `FROM`/`GRAPH` reads the union of the two and serves triples staged for removal — including one nested in a
  `FILTER EXISTS`, which dotNetRDF keeps in the filter's *expression* tree where a child-pattern walk never
  reaches it. The check is **"no graph but this one"**: assigning `ISparqlQuery.Model` injects `FROM <effective>`,
  so refusing every dataset clause breaks all of LINQ and makes re-execution non-idempotent. A materialized view
  therefore still parses each caller query — which also means the **strict** parser must accept it, so Trinity's
  wider extended syntax is refused there, and a pattern-less query (`DESCRIBE <iri>`) has a null root pattern. Queries that reason over the *layers* (the divergence
  precondition) must keep the three-graph dataset — the materialized clause is a bare `FROM`, so its named-graph set
  is empty and a `GRAPH <removals>` block against it silently matches nothing. Staging keeps it in step at **O(changes)** (0.7 ms vs a 31.5 s rebuild at
  1M) — but only via **bound patterns**: `?s ?p ?o` plus `FILTER (?s = <r> || ?o = <r>)` cannot use an index, so
  that shape is O(baseline) wherever the filter sits (measured 3.9 s / 3.7 s / 3 ms; `DeleteResource` 12.5 s → 3 ms); `Discard()` and a *forced* `Accept()` rebuild, a clean `Accept()` needs none. **Virtuoso silently writes zero**
  when one `INSERT … WHERE` exceeds its transaction log limit (fine at 500k, zero at 1M), so `Refresh()` counts and
  compares and **throws** rather than serving an empty view. Out-of-band writes to a layer leave it stale
  undetectably — call `Refresh()`.
- **Discovery is global static state** (0020): consumers must `MappingDiscovery.RegisterAssembly`
  / `OntologyDiscovery.AddAssembly` at startup or mapping and SPARQL prefixes silently miss.
  `AddMappingClasses` attempts **every** class and reports the failures together as an
  `AggregateException`; it must never abort the batch, because an unregistered mapping raises no error
  at all — resources just come back as base `Resource` — and which classes survived would depend on
  `Assembly.GetTypes()` ordering. `RegisterAssembly` marks the assembly registered only *after* the
  batch succeeds, so a retry after fixing the offender is not an early-return no-op.
- **No configuration subsystem** (0011, 2.0): the `ontologies.config`/`app.config` loading,
  `InitializeFromConfiguration`, and `CreateStoreFromConfiguration` are gone. Seed schema/background
  graphs with `store.Read` / the thin `store.LoadGraphs(...)` helper, register vocab via
  `OntologyDiscovery`, and create stores from a connection string the caller supplies.
- **Stores own their capabilities** (0022): inferencing is a per-query `inferenceEnabled` flag
  a store may honor or ignore; there is no capability model. Custom stores implement `IStore`
  (its docstrings are stale — trust the code).
- **Lazy loading of linked resources is always on** (0023, via `ResourceCache`) — not disablable.
  A latent bug (#30) lives here: `SetValue` doesn't invalidate the cache (ADR-0029).
- **Bulk subject constraints are `VALUES`, never an equality chain** (0046): the lazy load under every
  mapped-property dereference binds its subjects with `VALUES ?s { … }`, emitted *before* the pattern,
  in batches of 1000, via the shared `SparqlSerializer.GenerateSubjectBinding(s)`. The chain it
  replaced (`FILTER(?s = <a>||…)`) is a **correctness** problem, not just a slow one: Virtuoso parses it
  as nested binary pairs and refuses past a compile-time depth with `SP031` — measured at 1024 subjects
  on 7.2.12/7.2.14, reported at 157 by a consumer, and **not** movable via `ThreadStackSize`. Because it
  sits under reads *and* writes (`Add`/`Remove` read before mutating), a capped collection is unusable
  in both directions. Three things are load-bearing and easy to break: the projection stays `?s ?p ?o`
  **in that order**, the triple pattern keeps its **trailing `.`** (both required for
  `ProvidesStatements()`, or materialization refuses the query outright), and **blank ids are skipped**
  rather than serialized — no SPARQL query can address a blank node by label. An empty or null subject
  set returns empty; it must never degrade to a whole-model scan. **All three `IModel` implementations
  share one loop** (`BulkResourceReader`) — the fix originally landed in two of them and `LayeredModel`
  kept issuing one unbounded block, which is why the reader exists rather than three copies. Neither a
  300-member nor a 2000-subject store test guards the shape: with batching at 1000, 2000 subjects are
  two queries of 1000, both under the 1024-term chain limit. `BulkResourceQueryShapeTest` captures the
  SPARQL each model actually emits and is the guard — verified by reverting the shape while keeping
  the batching.
- **Every IRI reaching SPARQL text goes through `SparqlSerializer.SerializeUri`** (0046). Interpolating
  a `Uri` calls `Uri.ToString()`, which returns the *display* form and unescapes percent-encoding;
  where the unescaped character is one SPARQL forbids in an `IRIREF` (`%20`, `%3E`) the whole query
  becomes `RdfParseException: Illegal white space in URI` — so it fails loudly, and takes unrelated
  subjects in the same query with it. **`OriginalString` is the only correct source.** `AbsoluteUri` is
  *not* a safe alternative, as this file previously claimed: it normalizes host casing, default ports,
  dot-segments and percent-encoding case, while `Resource.Equals`/`GetHashCode` compare the **ordinal**
  `OriginalString` and the LINQ provider joins two result sets on it — so normalizing silently breaks
  mapped-collection dedup, drops LINQ rows, and hands Virtuoso the lower-cased host `XsdTypeMapper`
  warns about. An IRI that cannot be written verbatim is therefore **refused** by `SerializeUri`,
  naming itself, rather than rewritten. `SerializesVerbatimAndNeverNormalizes` is the guard.
  Blank nodes split two ways and conflating them is a real defect. `IsBlankId()` asks *is this a blank
  node*; `IsBlankNodeLabel()` asks *is it spelled `_:`*. Virtuoso's blank ids are `nodeID://`
  **absolute IRIs**, so they must be **bracketed** — deciding serialization on the flag emits them
  bare and breaks writing them. **Serialization decides on the spelling.** But *using* one as a query
  subject is refused on **every** store, uniformly and by decision: Virtuoso's look addressable and
  `ContainsResource` does find them (the identifier goes straight into a pattern), yet `GetResource`
  binds the subject and a bound IRI term never matches a blank-node subject — a half-working
  capability on one backend is worse than none. Guards therefore call `CanBeQuerySubject()`, which is
  named for the decision so a guard asking the other question *looks* wrong; `IsBlankId` stays where
  the question really is "is this a blank node" (`CreateResource`'s existence check, the stores'
  mint-an-identifier branch). The guard itself is **one shared `QuerySubject.Require`**, because the
  contract was documented as uniform while `ModelGroup` had none — and its `ContainsResource` puts the
  identifier in a bare pattern position, where a `_:` label matches *everything* and answered `true`
  for any non-empty group. The test that enforces this **discovers** its call sites by reflecting over
  `IModel` (every method taking a `Uri` first, minus a reasoned exclusion list), because the previous
  hand-written list of nine was green while a tenth accessor went unguarded — a hand-maintained list
  cannot detect its own omission.
  The invariant underneath is **bind versus interpolate, not read versus write**: a bound term fails
  closed (matches nothing), an interpolated label fails open (an existential variable matching
  *everything*). So `Model.DeleteResource` is deliberately unguarded — it binds, the store refuses the
  blank `DELETE` template loudly (ADR-0039) and nothing changes — while `LayeredModel.DeleteResource`
  interpolates and must guard: that shape stages the whole baseline for removal, silently, on both
  backends. This is **not** the .NET 10 `Uri`
  equality problem (0025): that one is identity, this one is serialization, and it is identical on
  .NET 8/9/10. An audit fixed four sites;
  `Trinity.Tests/ObjectModel/EncodedUriContact.cs` is a mapped class with `%20` in its class and
  property IRIs that exists purely to keep query builders honest.
- **SPARQL reuses registered ontology prefixes** (0024): `foaf:name` needs no `PREFIX` line.
- **URI identity is fragment-aware** (0025): use `UriRef`, not raw `Uri` — .NET's `Uri.Equals`
  ignores the fragment, which is wrong for RDF. Blank nodes/URNs have their own identity.
  This is a **rule, not a preference**, and overriding `Equals` was never enough to enforce it.
  **.NET 10 added `IEquatable<Uri>` to `System.Uri`**, and `EqualityComparer<T>.Default` prefers it —
  so every `HashSet<Uri>`/`Dictionary<Uri,…>`/`Contains`/`Distinct` bypassed `UriRef.Equals` and went
  fragment-blind, without a recompile, because the core is netstandard2.0. `UriRef` now implements
  `IEquatable<Uri>` and declares `==`/`!=` (operators bind **statically**, so `Equals` alone never
  covered them). **One hazard cannot be fixed**: `Uri a = someUriRef; a == b` still binds to
  `Uri.operator ==`. Hence `TRIN007` plus the `PropertyMapping<T>` throw. Internal identity collections
  are typed `UriRef` (`ResourceCache`, the LINQ translator's type constraints, `RdfClass/RdfPropertyAttribute.MappedUri`);
  everything keyed on `Uri.OriginalString` was left alone — it is fragment-safe already.
  **Run the suite on .NET 8, 9 and 10** (`DOTNET_ROLL_FORWARD=LatestPatch|Major|LatestMajor`) — passing
  on one runtime is exactly what let this hide for a release.
- **`Commit()` writes a per-value delta, not the whole resource** (0039): it diffs against a snapshot
  taken whenever `IsSynchronized` became true, so concurrent writers touching different values no longer
  erase each other. It still **does not cascade** (0029) — linked resources you changed must be committed
  individually. `HasUnsavedChanges()` is a per-resource dirty check (there is no aggregate one);
  `IsNew`/`IsSynchronized`/`IsReadOnly` remain the coarse flags and `Rollback()` re-fetches.
- **Deleting a resource removes triples where it's subject *and* object** (0030) — broad by design.
- **Transactions are ADO-style but unevenly supported** (0028): the non-transactional stores return a
  `NoOpTransaction` rather than `null` (0039) — never null, but never isolating either.
- **Query results are multi-modal** (0031): `GetResources`/`GetBindings`/`GetAnwser`(sic)/`Count` —
  pick the accessor matching the query form (with offset/limit paging).
- **Datatype & i18n mapping** (0026/0027/0048): `XsdTypeMapper` (culture-invariant via `XmlConvert`).
  A language-tagged literal is a **`LangString`** — one type, replacing the four shapes 0027 lived with.
  It **validates** the tag against the SPARQL/Turtle `LANGTAG` grammar `[a-zA-Z]+('-'[a-zA-Z0-9]+)*`
  and normalizes it with `ToLowerInvariant` at construction — one implementation, which
  `LocalizedValueStore.Normalize` and the LINQ translator both call rather than repeating, so a
  validated constructor cannot end up beside an unvalidated indexer, and a query cannot disagree with
  a read about which tag was asked for. **Not BCP-47 well-formedness**: an earlier version also capped
  subtags at eight characters, and because every literal read from a store is constructed here, one
  triple another writer tagged `@en-abcdefghij` — legal everywhere — made *every* read of that
  resource throw, untyped `GetResource` included. The grammar is the serialization boundary; length is
  no part of it, and policing a tag registry is not this type's job. This is
  why `AddProperty`/`HasProperty` cannot disagree about casing and why the 0039 delta is stable across a
  read/commit cycle. Validation is **load-bearing, not cosmetic**: a tag reaches the store as *syntax*
  (`'x'@de` has no place for a quoted tag), so `SparqlSerializer` escapes the value and interpolates the
  tag raw — an unvalidated tag from request data would carry query text into an update on `Commit()`. An
  **untagged literal is a plain `string`**: `LangString.Language` is never null, so there is no second
  way to spell "no tag", and `"Hallo"` can never equal `new LangString("Hallo","de")`. There is
  deliberately **no conversion to or from `string`** (ADR-0025 applied, not repeated — `label ==
  "Hallo"` must not compile), but `==`/`!=` between two `LangString`s are declared, or a class compares
  by reference.
- **The property's declared type decides how tags are handled** (0048). `Resource.Language` and the
  `languageInvariant` flag are **gone** — there is no ambient state, so a resource is safe to read in
  two locales at once:

  | Declared type | Sees |
  |---|---|
  | `string`, `List<string>` | untagged literals only (what `languageInvariant: true` used to mean) |
  | `LocalizedString` | every language, one value each |
  | `LocalizedStringCollection` | every language, several values each |
  | `LangString`, `List<LangString>` | tagged literals, raw triple view |

  `HasProperty`/`RemoveProperty` with a tag **answer rather than throw** — a value cannot carry a
  malformed tag, so the answer is `false` and the removal a no-op; `AddProperty` still throws, because
  naming a tag to write is an intention.
  Containers hold **all** languages at once, so `Languages`/`ListLanguages()` can answer which exist and
  editing one never disturbs another. `Best()` is RFC 4647 **Lookup**, which truncates the *request* —
  `de-DE` finds a `de` value, `de` does not find `de-DE`. Indexers are exact-match on get *and* set, so
  `t[k] = t[k]` cannot move a value between languages. Declaring `LocalizedString` against genuinely
  multi-valued data is **lossy** — it keeps the last value — exactly as a mapped `string` already does
  to a multi-valued predicate; `LocalizedStringCollection` is the escape hatch. The dropped value is
  **orphaned, not deleted**, and this file previously claimed the opposite. The commit snapshot comes
  from `ListValues()`, i.e. the resource *after* the container dropped the duplicate, so it sits in
  neither side of the 0039 delta: `HasUnsavedChanges()` is `false` and `Commit()` emits nothing. The
  value stays in the store, permanently invisible through that property, and nothing will ever remove
  it. Same for a mapped `string` over a multi-valued predicate — both measured, and pinned by
  `AValueDroppedByASingleValuedContainerSurvivesInTheStore`.
  `[RdfProperty(uri, languageInvariant)]` still compiles for one release and raises
  **TRIN008**; `RdfPropertyAttribute`'s two-argument constructor is `[Obsolete]` and goes in 2.1.
  Containers are declared **get-only** — they are mutated in place, not assigned, and a setter both
  admits `null` and aliases one container across two resources, so **TRIN009** warns. The diagnostic is
  not the guarantee, though: a hand-written mapping never reaches the generator, so `SetValue` **copies
  into** the container the mapping owns rather than assigning the reference. Assigning `null` empties it
  (it used to leave the mapping holding none, so the next `ListValues()` threw and took `Commit()`,
  `HasUnsavedChanges()` and the snapshot with it) and `a.Title = b.Title` copies rather than aliases.
  Only `LocalizedString` and `LocalizedStringCollection` can be mapped — `ILocalizedText` is their
  shared surface, **not an extension point**, and anything else is refused at registration rather than
  accepted and silently dropped. The generator emits
  exactly the accessors the declaring half declares (each with its own modifiers, so `private set` and
  `init` round-trip); emitting `get`+`set` unconditionally used to make the get-only form CS9253.
  **LINQ** queries one language at a time: `Where(d => d.Label["de"] == "Hallo")`. The tag must be a
  constant, because it becomes part of the query text — a closure is folded to one by the partial
  evaluator, a per-row value is refused.
  **The tag is attached where a pattern is emitted, and it is part of the binding's cache key.** Both
  halves are load-bearing and neither fails loudly. Keying by predicate path alone gave `Label["de"]`
  and `Label["en"]` in one query *the same* variable, so the constraints met as
  `LANG(?v)="de" && LANG(?v)="en"` — unsatisfiable. Constraining at comparison time instead left every
  other consumer of that variable reading it bound to all languages.
  **Four functions own this and nothing else may re-derive it:** `MemberLanguageConstraint` decides
  *whether and to what*; `AddMemberPattern` emits a member's triple with it; `BindingKey` computes the
  cache key; `MemberOperand` turns a constrained binding into a filter operand (`STR(?v)`). Fixing
  this per call site is how it keeps regressing — first the tag was applied only on `== constant`,
  then only on paths going through `BindChain`, leaving `.Count`'s sub-select, `.Any()`'s EXISTS group
  and `SelectMany`'s element each emitting an unconstrained pattern of their own, and the projection
  paths computing the pre-tag key by hand so their reuse branch never ran.
  **`ChainKind.Localized` deliberately has no case in `TranslateChainComparison`.** It had one, ahead
  of `default`, and that position *was* the bug: `default` is where `== null` becomes a (NOT) EXISTS
  and where `inDisjunction` decides optional binding, so a case in front of it silently skipped both —
  `Title["de"] == null` threw, and `Title["de"] == x || Title["en"] == y` quietly meant *and*.
  Indexing a `LocalizedStringCollection` in a query is **refused**: its indexer yields every value for
  the tag, and recording that as a scalar made ordering drop rows.
  The emitted form is `LCASE(LANG(?v)) = "de"` plus `STR(?v) = "…"`, **not** `?v = "…"@de`: measured on
  dotNetRDF 3.5.2, a language-tagged literal inside a `FILTER` comparison matches regardless of its tag
  (`"x"@fr` matched a `@de` value), while the same literal in a *triple pattern* matches correctly and
  `STR`/`LANG` evaluate correctly. `LCASE` wraps `LANG` because stores disagree about the case they hand a
  tag back in. Measured by removing it: **Fuseki and GraphDB fail, Oxigraph and the in-memory engine
  pass** — so the defect is real on half the backends and invisible to the fast suite, and it needs a
  **region subtag** to show at all, since a bare `de` has no case to disagree about. Every localized
  test written before it used bare tags, which is why four green suites said nothing about it;
  `QueriesALocalizedPropertyByTagAcrossStores` is the guard and lives in the shared store fixture.
  Three things are **refused rather than approximated** (the ADR-0041 posture):
  `Best()`/`TryGetBest()`, because RFC 4647 lookup is a client-side fallback walk that `langMatches`
  would answer differently; projecting a single language (`Select(d => d.Label["de"])`), because the
  bound variable carries every language; and `.Count`/`.Any()` on a container, because
  `LocalizedString` counts *languages* while `LocalizedStringCollection` counts *values*, so no single
  triple count is right for both. Filter on the language in `Where` and project the resource instead.
  A mapped `string` binds with `LANG(?v) = ""`, so it never matches a tagged literal — previously true
  only for `==`, since a plain literal term matches only a plain literal, but *not* for `StartsWith`
  (SPARQL argument compatibility makes `STRSTARTS("x"@de, "x")` true) nor for `Select`, which returned
  tagged values unwrapped. Measured before keeping: over 20k documents the added filter is below the
  run-to-run noise floor.
  Such a constrained variable is then compared by **`STR(?v)`, never term equality** — on **Virtuoso
  7.2**, `?v = "x"` is false whenever a `LANG()` constraint on the same variable is in the query, even
  though `?v = "x"` alone and `LANG(?v) = ""` alone each return the rows. Every spelling of the
  language test fails that way and every one passes with `STR()`. It converges on the same comparison
  form the localized path uses for the opposite reason (a tagged term in a `FILTER` is matched
  tag-blind). It stayed invisible because Virtuoso's **only** LINQ coverage was two layered-model
  tests — a mapped-string `Where` was never queried against it directly.

## Other architecture notes

- **RDF engine** (ADR-0038, supersedes the 0006 pin): dotNetRDF **3.5.2**, split across
  `dotNetRdf.Core` (engine) + `dotNetRdf.Client` (HTTP connectors) + `dotNetRdf.Inferencing`
  (`RdfsReasoner`); all netstandard2.0. A graph's identity is **`IGraph.Name` (an `IRefNode`) and it is
  immutable** — construct graphs with their name. Gotcha: a parsed `@base` overwrites `Graph.BaseUri`,
  and the Sesame/Fuseki connectors still pick the graph they *write* to from `BaseUri`, so the store
  read paths re-assign it after parsing.
- **LINQ-to-SPARQL** (ADR-0037, supersedes 0007): an **owned provider** under `Trinity/Query/Sparql`
  (own SPARQL AST → serializer → `Model.ExecuteQuery`/`GetResources`); re-linq / Remotion.Linq retired.
  `IModel.AsQueryable<T>()` routes to it, and it emits SPARQL strings — so it's decoupled from
  dotNetRDF's Query Builder and the 3.x upgrade won't touch it. A few LINQ-provider gaps stay quarantined.
- **Oxigraph refuses what it cannot do** (ADR-0047). It is the thinnest backend: no reasoner, no
  client-visible transactions, no auth, and no dataset/repository concept — one server is one store,
  so `host` is the whole connection string. `inferenceEnabled: true` **throws** rather than being
  ignored the way Fuseki ignores it: ADR-0022 permits either, but its Consequences name the silent
  no-op as the defect, and an un-inferred answer is indistinguishable from a correct one. That
  inverts the second half of two `LayeredModelMaterializationTest` cases — materialization lifts
  *Trinity's* refusal (ADR-0042), it cannot conjure a reasoner. Covering a strict backend also found
  three defects the lenient ones hide: dotNetRDF emits **invalid RDF/XML** (unquoted DTD entity
  values) and **BOM-prefixed Turtle**, and its catch-all `Accept` header lets an ASK come back as the
  plain text `false`. The adapter writes BOM-less Turtle and picks `Accept` by query form.
  `StoreBase.TryParse` is shared for the same reason `GroupByTargetGraph` is — GraphDB's copy had no
  TriG case, so TriG read from a string or stream was handed to the RDF/XML parser.
  **A Graph Store `SaveGraph` is a `PUT`, i.e. a replace**: `Read(update: true)` must add through
  `UpdateGraph` (or Oxigraph's `AppendGraph`), and until the PR #46 review Fuseki and GraphDB lost data
  here. **Name graphs by `OriginalString`, never `AbsoluteUri`**, which lower-cases the host. dotNetRDF's
  connectors and its SPARQL results parsers both normalize that way; Oxigraph works around both, while
  Fuseki/GraphDB/Virtuoso are quarantined (`doc/known-test-failures.md`).
- **Fuseki is a first-class backend** (ADR-0043), not the experimental one the older ADRs describe. Covering
  it found three defects the other backends hid. The layered view's `FROM NAMED` dataset clause was emitted
  **twice**, because `SparqlPreprocessor` never recorded a `FROM NAMED` graph and so could not suppress the
  duplicate — Virtuoso and GraphDB tolerate the repetition, Jena rejects it (HTTP 400). Then moving the
  `ContainsModel` test into the shared `StoreCatalogTest<T>` found that **Virtuoso answered `true` for every
  URI** (it ran an `ASK` and counted rows, so it never read the boolean) and that the in-memory store
  answered `true` for a null URI. `SparqlPreprocessor` keeps `DefaultGraphs` and `NamedGraphs` apart for the
  same reason: `FROM <g>` and `FROM NAMED <g>` are independent clauses, and conflating them makes assigning
  a model silently empty the default graph. If you add a backend, or share a per-store test, expect it to
  find things — that is the point of having more than one.
- **Stores** (ADR-0008/0009): `IStore`/`IModel`/`StoreFactory`; providers registered **manually**
  via `StoreFactory.LoadProvider<T>()`. The `[Export]`/`System.Composition` MEF wiring is dead code.
  `provider=stardog` references and a Stardog test project exist but there is **no Stardog provider**.

## Conventions

- Every `.cs` starts with the MIT license header block (Copyright Semiodesk GmbH). Preserve it on new
  files. **New files name only Moritz Eberl** in the `AUTHORS` block — Sebastian Faubel no longer
  contributes. Existing files keep both names; the attribution was accurate when they were written, so
  there is nothing to correct.
- 4-space indent, Allman braces, XML-doc comments on public members. No `.editorconfig` yet.
- Nullable/ImplicitUsings are **not** enabled repo-wide (a deliberate later pass).

## Pointers

- **Decisions & history:** `doc/adr/README.md`; known-failing tests: `doc/known-test-failures.md`.
- **Releasing:** `RELEASING.md` — manual publish to nuget.org (CI builds/tests/packs only, no publish).
- **External consumers:** `C:\Projects\elxgen` (ElectrixOS), `C:\Projects\DevHub` (Relay) — real
  usage; both migrate to 2.0 (`partial` properties) when they adopt it.
- Persistent cross-session notes live in Claude's auto-memory.
