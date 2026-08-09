# 0011. Retire the configuration subsystem for an imperative store API

Date: 2026-07-14 (original design 2015–2020; superseding decision made for 2.0)

## Status
Accepted (2.0) — supersedes the original `ontologies.config`/`app.config` design below.

## Context
Trinity shipped a declarative configuration subsystem so a store could be "initialized from
configuration": `IStore.InitializeFromConfiguration()` read an XML `ontologies.config` (or, on
.NET Framework, a `<TrinitySettings>` `ConfigurationSection` in `app.config`) listing ontologies
`(uri, prefix, filesource)`, and a `<stores>` section with Virtuoso rule-set graph groupings.
`StoreFactory.CreateStoreFromConfiguration` similarly read `ConfigurationManager.ConnectionStrings`.

With the ontology generator retired ([0014](0014-ontology-generator-modernization.md)) the config
file is no longer a **build** input, only a **runtime** one. Inspecting that runtime path shows how
little it does:

```
InitializeFromConfiguration()
  → LoadConfiguration()   // parse ontologies.config (or legacy app.config <TrinitySettings>)
  → LoadOntologies()
      → StoreUpdater.UpdateOntologies()
          → foreach ontology:  store.Read(onto.Uri, file, format, update: false)
```

The entire runtime job is a `foreach` of `store.Read(graphUri, file, format)` — seeding named
"background" graphs (schemas/ontologies) from files. Around that one loop sat: an XML model
(`Configuration`, `IConfiguration`, `IOntologyConfiguration`, `IStoreConfiguration`), a parser
(`ConfigurationLoader`), a nine-file legacy `System.Configuration` section (`TrinitySettings` et al.),
`StoreUpdater` with path-resolution and format-from-extension helpers, and the `ontologies.config`
convention — plus a hard dependency on `System.Configuration.ConfigurationManager`.

Crucially, **neither external consumer uses it.** ElectrixOS and DevHub register their vocab in code
(`OntologyDiscovery.AddAssembly`) and seed graphs with their own small loaders / `store.Read` calls;
only the tests fed `ontologies.config`. The legacy `app.config` path is already dead on .NET
(`[Ignore]`d on net8). The declarative layer solved a problem consumers solved differently, at the
cost of a cross-platform-hostile dependency and two overlapping mechanisms.

The subsystem also conflated two unrelated concerns:
1. **Seeding background graphs** — literally `store.Read(...)`.
2. **Prefix/term registration** for SPARQL/mapping — that is `OntologyDiscovery`, driven by scanning
   vocab assemblies ([0020](0020-discovery-is-global-static-state.md)), *not* by loading graph files.

## Decision
Retire the configuration subsystem in 2.0 and converge on the imperative primitives (all already
public):

- **Seed background graphs:** `store.Read(graphUri, file/stream, format)`, or the new thin
  convenience `store.LoadGraphs(...)` for reading several files at once (format inferred from the
  file extension). See `Trinity/Stores/StoreExtensions.cs`.
- **Register vocab/prefixes:** `OntologyDiscovery.AddAssembly(...)` / `AddNamespace(...)`
  ([0020](0020-discovery-is-global-static-state.md)).
- **Create a store:** `StoreFactory.CreateStore(connectionString)` with the connection string
  supplied by the caller (from its own `IConfiguration`/appsettings), not read by Trinity from
  `ConfigurationManager`.

Removed:
- `IStore.InitializeFromConfiguration` / `LoadOntologySettings` (and `StoreBase`/`VirtuosoStore`/
  `SparqlEndpointStore` implementations), `StoreBase.LoadConfiguration`/`LoadOntologies`.
- `StoreFactory.CreateStoreFromConfiguration` and its `ConfigurationManager.ConnectionStrings` read.
- `Trinity/Configuration/*` (the XML model + `ConfigurationLoader` + the `Legacy/` `TrinitySettings`
  `ConfigurationSection`), and `Trinity/Stores/StoreUpdater.cs`.
- The `IStoreSpecific` interface, `StoreUpdater.UpdateStorageSpecifics`, and the Virtuoso
  `VirtuosoSettings` rule-set applier (`Trinity.Virtuoso/VirtuosoSpecific.cs`). This "apply
  store-specific settings" path was only ever reachable through `InitializeFromConfiguration`
  (it deserialized `<rulesets>` from `ontologies.config` and issued Virtuoso `rdfs_rule_set`
  statements). It can return later as an explicit Virtuoso inference-rules API if a consumer needs it.
- The `System.Configuration.ConfigurationManager` package reference from the three stores and the
  test project. (Core never referenced it directly — it resolved transitively via dotNetRDF — so
  core needs no csproj change once its usage is gone.)
- The test `ontologies.config`/`ontologies-test.config`/`custom.config`/`without_store.config` and the
  legacy `App.config`, plus the tests that exercised the loader (`ConfigurationTest`,
  `LegacyConfigurationTest`, the three `DotNetRDFStoreTest.LoadOntologies*` cases).

Kept: `store.Read`/`store.Write`; `RdfSerializationFormat`; `OntologyDiscovery`/`MappingDiscovery`
(the code-based registration the tests and both consumers already use).

## Consequences
- **Cross-platform:** drops the last `System.Configuration.ConfigurationManager` dependency; nothing
  in the runtime depends on full-framework `System.Configuration` semantics anymore.
- **One obvious way** to seed graphs and register ontologies — the way both consumers already use.
- **Breaking (intended for 2.0):** callers of `InitializeFromConfiguration` / `CreateStoreFromConfiguration`
  migrate to `LoadGraphs`/`Read` + `AddAssembly` + `CreateStore(connectionString)`. Neither external
  consumer is affected (they never used the config path).
- The two quarantined config tests are **deleted** rather than perpetually `[Ignore]`d.
- If a declarative "graphs to seed" list is ever wanted for ops, do it the modern way — bind an options
  object from `appsettings.json` via `IConfiguration`/`IOptions` and pass it to a few `Read` calls —
  not by resurrecting bespoke XML + `ConfigurationManager`.

## Original decision (2015–2020, now superseded)
- Ontology generation + runtime seeding configured by an XML `ontologies.config`, deserialized with
  `XmlSerializer` into `Semiodesk.Trinity.Configuration.Configuration`
  (`<ontologies namespace>` → `<ontology uri prefix>` → `<filesource>`/`<websource>`).
- A **legacy path** additionally supported a `<TrinitySettings>` custom `ConfigurationSection` read from
  `app.config`/`web.config` via `ConfigurationManager`.
- Store configuration read `ConfigurationManager.ConnectionStrings`.

This carried two overlapping mechanisms, one tied to full-framework `System.Configuration` semantics
that behave differently (or not at all) on modern .NET; the legacy-detection heuristic in the old
`GenerateOntologyTask` was buggy (hardcoded to the test namespace); and `<websource>` implied network
fetches during build, at odds with deterministic/offline builds.

## Related
- [0008](0008-store-model-abstraction.md), [0014](0014-ontology-generator-modernization.md),
  [0016](0016-resource-centric-not-triple-centric.md), [0020](0020-discovery-is-global-static-state.md),
  [0024](0024-sparql-reuses-registered-ontology-prefixes.md)
