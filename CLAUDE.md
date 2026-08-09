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
| `Trinity.Tests` | net8.0 | NUnit in-memory suite (fully generator-driven, no weaver) |
| `tests/Trinity.Generator.Tests` | net8.0 | Source-generator validation |
| `tests/Trinity.Tests.{Virtuoso,Fuseki,GraphDB}` | net8.0 | Store integration tests — self-provision the server via Testcontainers/Docker (ADR-0036); not in the default CI job |
| `doc/adr/` | — | Architecture Decision Records |

Retired in 2.0: `Trinity.CilGenerator` (the cilg weaver, ADR-0013), `Trinity.OntologyGenerator`
(net4x vocab tool — ADR-0014 will reintroduce vocab generation as a source generator), the
`build/Semiodesk.Trinity.targets`, `build.cake`/`appveyor.yml`, and the `Documentation` docfx
project (removed from the solution; docs build separately).

## Build & test

Prereq: **.NET 10 SDK only** — no .NET Framework targeting packs, no Visual Studio. Everything
is netstandard2.0 / net8.0 and builds cross-platform.

```bash
dotnet build Semiodesk.Trinity.sln -c Release          # whole solution, SDK-only
dotnet test Trinity.Tests/Trinity.Tests.csproj         # 386 passed, 6 skipped (quarantined)
dotnet test tests/Trinity.Generator.Tests/Trinity.Generator.Tests.csproj   # 4 passed
dotnet pack Trinity/Trinity.csproj -c Release          # -> Semiodesk.Trinity.2.0.0.nupkg
```

- The 6 skipped tests are `[Ignore]`d and tracked in `doc/known-test-failures.md`: in-memory
  inferencing (store-level, ADR-0022) and one open semantics decision (polymorphic base-type queries,
  ADR-0037). No missing LINQ translation or datatype bug remains, and none are generator regressions.
- Store integration tests (`tests/Trinity.Tests.*`) **self-provision** their server in Docker via
  Testcontainers on a random host port (ADR-0036): run `dotnet test tests/Trinity.Tests.{Virtuoso,GraphDB,Fuseki}`
  with a Docker daemon running. Excluded from the default CI job (Docker + large images). Current:
  Virtuoso 90/97 and GraphDB 97/101 pass; Fuseki is 4/86 — a pre-existing dotNetRDF `FusekiConnector`
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
override from `[RdfClass]`. It handles scalars, collections (seeded with a default instance),
language-invariant strings, resource references, multiple `[RdfClass]`, and inheritance
(including `GetTypes`-only subclasses). Only `partial` members are processed. The runtime engine
(`Resource`, `PropertyMapping<T>`, reflective `InitializePropertyMappings`) is unchanged from 1.x.

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
- **`Commit()` does not cascade** (0029): it persists only that resource; linked resources you
  changed must be committed individually. `IsNew`/`IsSynchronized`/`IsReadOnly` are the (coarse)
  tracking flags; `Rollback()` re-fetches from the store.
- **Deleting a resource removes triples where it's subject *and* object** (0030) — broad by design.
- **Transactions are ADO-style but unevenly supported** (0028): Fuseki/GraphDB return `null`.
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
