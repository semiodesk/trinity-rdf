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
| `Trinity.Virtuoso` | netstandard2.0 | Virtuoso backend (vendored OpenLink provider) — highest risk |
| `Trinity.GraphDB` | netstandard2.0 | GraphDB backend |
| `Trinity.Fuseki` | netstandard2.0 | Fuseki backend |
| `Trinity.Tests` | net8.0 | NUnit in-memory suite (fully generator-driven, no weaver) |
| `tests/Trinity.Generator.Tests` | net8.0 | Source-generator validation |
| `tests/Trinity.Tests.{Virtuoso,Fuseki,GraphDB}` | net8.0 | Store integration tests — need live servers, not run in CI |
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
dotnet test Trinity.Tests/Trinity.Tests.csproj         # 260 passed, 16 skipped (quarantined)
dotnet test tests/Trinity.Generator.Tests/Trinity.Generator.Tests.csproj   # 4 passed
dotnet pack Trinity/Trinity.csproj -c Release          # -> Semiodesk.Trinity.2.0.0.nupkg
```

- The 16 skipped tests are pre-existing / net8-environmental cases, `[Ignore]`d and tracked in
  `doc/known-test-failures.md` (inferencing, some LINQ-provider gaps, a DateTime tz difference,
  legacy app.config). None are generator regressions.
- Store integration tests (`tests/Trinity.Tests.*`) need a live Virtuoso/Fuseki/GraphDB and are
  excluded from CI.
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

- **RDF engine** (ADR-0006): dotNetRDF **2.7.0** (pinned; 3.x is a breaking upgrade — deferred).
- **LINQ-to-SPARQL** (ADR-0007): Remotion.Linq (re-linq) — abandoned upstream, **not used by
  either consumer**. Deprioritized; several LINQ-provider tests are among the quarantined ones.
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
- **External consumers:** `C:\Projects\elxgen` (ElectrixOS), `C:\Projects\DevHub` (Relay) — real
  usage; both migrate to 2.0 (`partial` properties) when they adopt it.
- Persistent cross-session notes live in Claude's auto-memory.
