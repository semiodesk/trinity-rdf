# 0011. Configuration via `ontologies.config` with legacy `app.config`

Date: 2026-07-13 (decision original to 2015–2020 design)

## Status
Accepted — legacy paths to be trimmed in revival

## Context
The tooling needs to know which ontologies to generate code for (source location,
namespace, prefix, version) and the runtime needs store/connection configuration.
This was designed in the .NET Framework era, where `System.Configuration` and
`app.config`/`web.config` were the norm.

## Decision
- Ontology generation is configured by an XML `ontologies.config`, deserialized with
  `XmlSerializer` into `Semiodesk.Trinity.Configuration.Configuration`
  (`<ontologies namespace>` → `<ontology uri prefix>` → `<filesource>`/`<websource>`).
- A **legacy path** additionally supports a `<TrinitySettings>` custom
  `ConfigurationSection` read from `app.config`/`web.config` via
  `ConfigurationManager.OpenMappedExeConfiguration`.
- Store configuration reads `ConfigurationManager.ConnectionStrings`.

## Consequences
- Two overlapping configuration mechanisms, one of them tied to full-framework
  `System.Configuration` semantics that behave differently (or not at all) on .NET Core.
- The legacy-detection heuristic in `GenerateOntologyTask` is buggy (hardcoded to the test
  namespace), so in practice consumers rely on `ontologies.config`.
- `<websource>` implies network fetches during build — at odds with deterministic/offline builds.

## Revival notes
Drop the `app.config`/`ConfigurationManager` legacy path; make `ontologies.config` (or,
better, MSBuild item metadata on `AdditionalFiles` — see
[0014](0014-ontology-generator-modernization.md)) the single source. Move runtime store
config toward `IConfiguration`/appsettings. Resolve web-sourced ontologies to local files
at author time rather than fetching during compilation.

## Related
- [0005](0005-ontology-code-generation.md), [0008](0008-store-model-abstraction.md),
  [0014](0014-ontology-generator-modernization.md)
