# CLAUDE.md

Guidance for Claude Code (and humans) working in this repository.

## What this is

**Semiodesk.Trinity** — an enterprise object mapper for building RDF knowledge-graph
applications in .NET. It maps RDFS/OWL terms to POCOs (`Resource` + `[RdfClass]`/
`[RdfProperty]`), sits on top of **dotNetRDF**, and offers LINQ-to-SPARQL and pluggable
store backends. Original author: Moritz Eberl (Semiodesk). The project was unmaintained
for several years and is now being **revived**: stabilize on modern .NET first, then add
features. Read `doc/adr/README.md` for the decisions and the revival roadmap.

## Working agreement

- **Stability before features.** Prefer small, reviewable, well-scoped changes. Confirm
  before broad refactors.
- **The public mapping surface is a compatibility contract.** Two external consumers pin
  the published package **1.0.3.77**: ElectrixOS (`C:\Projects\elxgen`) and DevHub/Relay
  (`C:\Projects\DevHub`). Don't break `Resource`, `[RdfClass]`/`[RdfProperty]`,
  `IStore`/`IModel`, the memory + Virtuoso stores, or Turtle/JSON-LD I/O without a plan.
  When touching mapping or the generators, **validate against `elxgen`** (build it and
  check the woven output) as the reference consumer.
- **Verify, don't assume.** Run/build/observe before claiming something works. This repo
  has surprising behavior (see Gotchas) — check the actual build, not the intuition.
- Keep decisions in `doc/adr/`. Record a new ADR when a significant choice is made.

## Repo map

| Project | TFM | Role |
|---|---|---|
| `Trinity` | netstandard2.0 | Core: `Resource`, `PropertyMapping<T>`, `IStore`/`IModel`, `StoreFactory`, LINQ, in-memory + SPARQL-endpoint stores |
| `Trinity.Virtuoso` | netstandard2.0 | Virtuoso backend (vendored OpenLink provider) — highest risk |
| `Trinity.GraphDB` | netstandard2.0 | GraphDB backend (most recently maintained) |
| `Trinity.Fuseki` | netstandard2.0 | Fuseki backend |
| `Trinity.CilGenerator` | **net461** (legacy) | `cilg.exe` — Mono.Cecil IL weaver that implements the mapping |
| `Trinity.OntologyGenerator` | **net461** (legacy) | `OntologyGenerator.exe` — RDF/OWL → C# vocab classes |
| `Trinity.Tests`, `tests/*` | **net472** | NUnit test suites |
| `Documentation/` | — | DocFX API docs. `doc/adr/` | Architecture Decision Records |

## Build & test

Prereqs: **.NET 10 SDK** is installed. The net461/net472 projects additionally need the
.NET Framework targeting packs (or Visual Studio / full MSBuild).

```bash
# Builds with the .NET SDK alone (verified):
dotnet build Trinity/Trinity.csproj              # core library
dotnet build Trinity.Virtuoso/Trinity.Virtuoso.csproj   # + Fuseki, GraphDB likewise
```

- **The full solution (`dotnet build Semiodesk.Trinity.sln`) currently FAILS** on
  `Trinity.CilGenerator` and `Trinity.OntologyGenerator` with `MSB3644` (no net461
  targeting pack), and the net472 tests can't build without the 4.7.2 pack. This is a
  build-from-source gap, tracked in ADR-0015.
- `dotnet test` does **not** work yet — tests target net472. Retargeting them is the first
  revival step (ADR-0015). Historically tests ran via the NUnit console runner.
- `build.cake` / `appveyor.yml` are the **stale** legacy build (full MSBuild + `nuget
  restore` + VS2019). Don't rely on them; they'll be replaced (ADR-0015).

## Mental model (grounding decisions — ADR-0016…0024)

Invariants that surprise newcomers:
- **Resource-centric, not triple-centric** (0016): you load/mutate/persist whole `Resource`
  objects. There is no API to add or remove a single triple.
- **Resources are open** (0017): a mapped resource can still be annotated with arbitrary
  predicates at runtime; `ListValues` returns mapped *and* unmapped properties.
- **Attributes are only sugar** (0018): the real mapping is `PropertyMapping<T>` +
  `GetValue`/`SetValue` + `GetTypes()`. Attributes with no weaving/generation do nothing.
- **Models are named graphs; a `ModelGroup` is itself an `IModel`** spanning several (0019).
- **Discovery is global static state** (0020): consumers must `MappingDiscovery.RegisterAssembly`
  / `OntologyDiscovery.AddAssembly` at startup or mapping and SPARQL prefixes silently miss.
- **Stores own their capabilities** (0022): inferencing is a per-query `inferenceEnabled` flag
  a store may honor or ignore; there is no capability model. Custom stores implement `IStore`
  (its docstrings are stale — trust the code).
- **Lazy loading of linked resources is always on** (0023, via `ResourceCache`) — not disablable.
- **SPARQL reuses registered ontology prefixes** (0024): `foaf:name` needs no `PREFIX` line.
- **URI identity is fragment-aware** (0025): use `UriRef`, not raw `Uri` — .NET's `Uri.Equals`
  ignores the fragment, which is wrong for RDF. Blank nodes/URNs have their own identity.
- **`Commit()` does not cascade** (0029): it persists only that resource; linked resources you
  changed must be committed individually. `IsNew`/`IsSynchronized`/`IsReadOnly` are the (coarse)
  tracking flags; `Rollback()` re-fetches from the store.
- **Deleting a resource removes triples where it's subject *and* object** (0030) — broad by design.
- **Transactions are ADO-style but unevenly supported** (0028): Fuseki/GraphDB return `null`.
- **Query results are multi-modal** (0031): `GetResources`/`GetBindings`/`GetAnwser`(sic)/`Count` —
  pick the accessor matching the query form, with offset/limit paging + virtualizing collections (0032).
- **Datatype & i18n mapping** (0026/0027): `XsdTypeMapper` (culture-invariant via `XmlConvert`);
  localized strings are rudimentary and represented inconsistently.

## Architecture (orientation — see ADRs for depth)

- **Mapping engine** (ADR-0002/0003): mapped auto-properties are turned into
  `PropertyMapping<T>` fields + `GetValue`/`SetValue` calls, and `GetTypes()` is
  synthesized — done by **cilg IL weaving** at build time. Runtime mapping discovery is
  reflective (`Resource.InitializePropertyMappings`).
- **Ontology vocab** (ADR-0005): `OntologyGenerator` emits typed + string-constant vocab
  classes from `.ttl`/OWL. Both consumers currently hand-write vocab instead.
- **RDF engine** (ADR-0006): dotNetRDF **2.7.0** (pinned; 3.x is a breaking upgrade).
- **LINQ-to-SPARQL** (ADR-0007): Remotion.Linq (re-linq) — abandoned upstream, and **not
  used by either consumer**. Deprioritized.
- **Stores** (ADR-0008/0009): `IStore`/`IModel`/`StoreFactory`, providers registered
  **manually** via `StoreFactory.LoadProvider<T>()`.

## Gotchas (read before debugging the build)

- **cilg is a net461 EXE, but the *prebuilt* one runs fine under `dotnet build`.** Verified:
  the shipped `cilg.exe`/`OntologyGenerator.exe` load as MSBuild tasks on the .NET 10 SDK
  (Windows) and correctly weave a net9 assembly. "Runs" ≠ "builds from source" — building
  the generator projects needs the net461 pack (ADR-0003/0010).
- **The weaver forces `DebugType=Full`** because Mono.Cecil crashes on portable PDBs. Full
  PDBs are Windows-only ⇒ this breaks Blazor WASM / cross-platform / deterministic builds.
  This is *the* reason DevHub disabled the weaver and hand-wrote mappings while elxgen relies
  on it (ADR-0004). It's the crux of the modern-.NET story.
- **Core is netstandard2.0** — keep new core code netstandard2.0-compatible (no net-only
  APIs) unless we deliberately multi-target (ADR-0010/0015).
- **Dead MEF:** every store provider has `[Export(typeof(StoreProvider))]` and
  `System.Composition` is referenced, but nothing composes them — registration is manual.
- **Stardog** test projects and `provider=stardog` references exist, but there is **no
  Stardog provider**. Stale.
- **Version drift:** repo builds `1.0.3.50`; consumers use published `1.0.3.77`. Identify
  the branch/commit behind 1.0.3.77 before republishing (ADR-0012).

## Conventions

- Every `.cs` starts with the MIT license header block (authors Moritz Eberl / Sebastian
  Faubel, Copyright Semiodesk GmbH). Preserve it on new files.
- 4-space indent, Allman braces, XML-doc comments on public members. There is no
  `.editorconfig` yet (adding one is a revival task); match surrounding style.
- Many public members lack XML docs (CS1591 warnings on core build) — not errors.

## Pointers

- **Decisions & roadmap:** `doc/adr/README.md`
- **External consumers:** `C:\Projects\elxgen` (ElectrixOS), `C:\Projects\DevHub` (Relay) —
  study these for real-world usage and the two divergent coping strategies for the weaver.
- Persistent cross-session notes live in Claude's auto-memory (Trinity codegen + consumer facts).
