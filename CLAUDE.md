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
| `Trinity.Vocabulary` | netstandard2.0 | Vocabulary **parse+emit engine** — reads RDF, emits the `Ontology` classes `OntologyDiscovery` reflects on (ADR-0014) |
| `Trinity.Vocabulary.Cli` | net8.0 | `dotnet tool` front end, command `trinity-vocab`, package `Semiodesk.Trinity.Vocabulary.Tool` |
| `Trinity.Tests` | net8.0 | NUnit in-memory suite (fully generator-driven, no weaver) |
| `tests/Trinity.Generator.Tests` | net8.0 | Source-generator validation, incl. the TRIN diagnostics |
| `tests/Trinity.Vocabulary.Tests` | net8.0 | Vocabulary generator + `trinity-vocab`: term classification, all four RDF formats, determinism, sanitization/collisions, manifest reading, the check-mode exit codes, a member-compatibility check against the committed vocabularies, and a round-trip that compiles generated source and asserts `OntologyDiscovery` finds it |
| `tests/Trinity.Tests.{Virtuoso,Fuseki,GraphDB}` | net8.0 | Store integration tests — self-provision the server via Testcontainers/Docker (ADR-0036); not in the default CI job |
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
dotnet test Trinity.Tests/Trinity.Tests.csproj         # 398 passed, 7 skipped (quarantined)
dotnet test tests/Trinity.Generator.Tests/Trinity.Generator.Tests.csproj   # 23 passed
dotnet test tests/Trinity.Vocabulary.Tests/Trinity.Vocabulary.Tests.csproj # 29 passed
dotnet pack Trinity/Trinity.csproj -c Release          # -> Semiodesk.Trinity.2.0.0.nupkg
```

- The 7 skipped tests are `[Ignore]`d and tracked in `doc/known-test-failures.md`: in-memory
  inferencing (store-level, ADR-0022), one open semantics decision (polymorphic base-type queries,
  ADR-0037), and blank-node values in mapped collections failing on the read path (ADR-0039) — the only
  quarantined case that is an outright defect. No missing LINQ translation or datatype bug remains, and
  none are generator regressions.
- Store integration tests (`tests/Trinity.Tests.*`) **self-provision** their server in Docker via
  Testcontainers on a random host port (ADR-0036): run `dotnet test tests/Trinity.Tests.{Virtuoso,GraphDB,Fuseki}`
  with a Docker daemon running. Excluded from the default CI job (Docker + large images). Current:
  Virtuoso 99/107 and GraphDB 106/111 pass; Fuseki is 4/86 — a pre-existing dotNetRDF `FusekiConnector`
  query-endpoint bug (POSTs `/ds/query`, which the server 404s), unrelated to the container wiring.
- **CI:** `.github/workflows/ci.yml` (ubuntu, .NET 10) — restore → build → test → pack. NuGet
  publishing is **manual** (no publish job).
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

The generator handles scalars, collections (seeded with a default instance), language-invariant
strings, resource references, multiple `[RdfClass]`, and inheritance (including `GetTypes`-only
subclasses). The implementing half copies the declaring declaration's **modifiers verbatim**, so
accessibility and `new`/`virtual`/`override`/`sealed` match — C# requires both halves to agree, and
hiding a `Resource` member (`Language`, say) needs `new` on both or it is an unfixable CS8800. Only `partial` members are processed. The runtime engine (`Resource`,
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
- **Discovery is global static state** (0020): consumers must `MappingDiscovery.RegisterAssembly`
  / `OntologyDiscovery.AddAssembly` at startup or mapping and SPARQL prefixes silently miss.
- **No configuration subsystem** (0011, 2.0): the `ontologies.config`/`app.config` loading,
  `InitializeFromConfiguration`, and `CreateStoreFromConfiguration` are gone. Seed schema/background
  graphs with `store.Read` / the thin `store.LoadGraphs(...)` helper, register vocab via
  `OntologyDiscovery`, and create stores from a connection string the caller supplies.
- **Stores own their capabilities** (0022): inferencing is a per-query `inferenceEnabled` flag
  a store may honor or ignore; there is no capability model. Custom stores implement `IStore`
  (its docstrings are stale — trust the code).
- **Lazy loading of linked resources is always on** (0023, via `ResourceCache`) — not disablable.
  A latent bug (#30) lives here: `SetValue` doesn't invalidate the cache (ADR-0029).
- **SPARQL reuses registered ontology prefixes** (0024): `foaf:name` needs no `PREFIX` line.
- **URI identity is fragment-aware** (0025): use `UriRef`, not raw `Uri` — .NET's `Uri.Equals`
  ignores the fragment, which is wrong for RDF. Blank nodes/URNs have their own identity.
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
- **Datatype & i18n mapping** (0026/0027): `XsdTypeMapper` (culture-invariant via `XmlConvert`);
  localized strings are rudimentary and represented inconsistently.

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
- **Stores** (ADR-0008/0009): `IStore`/`IModel`/`StoreFactory`; providers registered **manually**
  via `StoreFactory.LoadProvider<T>()`. The `[Export]`/`System.Composition` MEF wiring is dead code.
  `provider=stardog` references and a Stardog test project exist but there is **no Stardog provider**.

## Conventions

- Every `.cs` starts with the MIT license header block (authors Moritz Eberl / Sebastian
  Faubel, Copyright Semiodesk GmbH). Preserve it on new files.
- 4-space indent, Allman braces, XML-doc comments on public members. No `.editorconfig` yet.
- Nullable/ImplicitUsings are **not** enabled repo-wide (a deliberate later pass).

## Pointers

- **Decisions & history:** `doc/adr/README.md`; known-failing tests: `doc/known-test-failures.md`.
- **Releasing:** `RELEASING.md` — manual publish to nuget.org (CI builds/tests/packs only, no publish).
- **External consumers:** `C:\Projects\elxgen` (ElectrixOS), `C:\Projects\DevHub` (Relay) — real
  usage; both migrate to 2.0 (`partial` properties) when they adopt it.
- Persistent cross-session notes live in Claude's auto-memory.
