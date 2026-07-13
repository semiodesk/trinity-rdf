# 0008. Store/Model abstraction with manually-registered providers

Date: 2026-07-13 (decision original to 2015–2020 design)

## Status
Accepted

## Context
Applications should target one API and swap the backend (in-memory for tests, a triple
store in production) without rewriting data access. Trinity needed a backend-neutral
abstraction and a way to select and construct a concrete store.

## Decision
- `IStore` is the backend contract (models CRUD, `ExecuteQuery`/`ExecuteNonQuery`,
  resource update/delete, RDF `Read`/`Write`, transactions); `StoreBase` is the shared base.
- `IModel`/`Model` is a named-graph facade over an `IStore`, offering typed CRUD
  (`GetResource<T>`, `GetResources<T>`, `CreateResource<T>`) and the LINQ entry point.
- `StoreProvider` + `StoreFactory` construct stores from a connection string
  (`provider=…;host=…`). Two providers are hard-wired in `StoreFactory`
  (`dotnetrdf`, `sparqlendpoint`); others are registered explicitly at runtime via
  `StoreFactory.LoadProvider<T>()`.

See `Trinity/Stores/IStore.cs`, `Trinity/Model/Model.cs`, `Trinity/Stores/StoreFactory.cs`.

## Consequences
- Clean seam for swapping backends; the in-memory store doubles as the test/reference store.
- Provider registration is **manual**. Every provider is annotated
  `[Export(typeof(StoreProvider))]` and `System.Composition` (MEF) is referenced, but
  **nothing composes them** — the attributes and the MEF dependency are dead code.
  Consumers must call `LoadProvider<T>()` themselves.
- Store configuration also reads `ConfigurationManager.ConnectionStrings`
  ([0011](0011-configuration-model.md)), a full-framework-era pattern.

## Revival notes
Either wire up real discovery/DI or delete the dead MEF attributes and `System.Composition`.
Consider `IServiceCollection` registration extensions for modern hosts.

## Related
- [0009](0009-supported-store-backends.md), [0011](0011-configuration-model.md)
