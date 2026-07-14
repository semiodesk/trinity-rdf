# Architecture Decision Records

This directory records the significant architecture decisions for Semiodesk.Trinity.

ADRs 0001–0012 were **extracted retrospectively (2026-07-13)** from the existing
implementation and documentation as part of reviving the project. They describe
decisions that are *already in force* in the code today, with the context and
consequences as they actually stand — including the parts that now hurt on modern
.NET. ADRs 0013+ are **proposals** for the revival and are not yet decided.

## Format

Each ADR is one file, `NNNN-kebab-title.md`, using this template:

```
# NNNN. Title
Date: YYYY-MM-DD
## Status         Accepted | Proposed | Superseded by NNNN | Deprecated
## Context        the forces at play — why a decision was needed
## Decision       what was decided (present tense, active voice)
## Consequences   results, good and bad
## Revival notes  (optional) how this looks under the modern-.NET revival
## Related        links to other ADRs / files
```

New decisions get the next free number; never renumber. Supersede rather than
edit a decided ADR (change its status, add the superseding link).

## Index

### Accepted (current state, extracted)
| # | Title | Status |
|---|-------|--------|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-attribute-based-object-mapping.md) | Attribute-based semantic object mapping on a `Resource` base | Accepted |
| [0003](0003-mapping-via-il-weaving.md) | Implement mapping via compile-time IL weaving (cilg / Mono.Cecil) | Accepted |
| [0004](0004-force-full-pdb-symbols.md) | Force full PDB symbols so the weaver can rewrite assemblies | Accepted |
| [0005](0005-ontology-code-generation.md) | Generate C# ontology vocabularies from RDF/OWL at build time | Accepted |
| [0006](0006-build-on-dotnetrdf.md) | Build on dotNetRDF as the RDF/SPARQL engine (pinned 2.7.0) | Accepted |
| [0007](0007-linq-via-relinq.md) | LINQ-to-SPARQL via Remotion.Linq (re-linq) | Accepted |
| [0008](0008-store-model-abstraction.md) | Store/Model abstraction with manually-registered providers | Accepted |
| [0009](0009-supported-store-backends.md) | Supported store backends; Stardog removed | Accepted |
| [0010](0010-target-frameworks.md) | Target frameworks: netstandard2.0 core, net461 tools, net472 tests | Accepted |
| [0011](0011-configuration-model.md) | Retire the configuration subsystem for an imperative store API | Accepted (2.0) |
| [0012](0012-packaging-and-distribution.md) | One NuGet package bundling libraries + build tools via `.targets` | Accepted |

### Accepted (grounding conceptual model)
These capture the foundational "how Trinity thinks" decisions — the resource model, the
mapped/dynamic duality, models & groups, discovery, stores, and querying.
| # | Title | Status |
|---|-------|--------|
| [0016](0016-resource-centric-not-triple-centric.md) | Resource-centric persistence, not triple-centric | Accepted |
| [0017](0017-resources-open-mapped-and-dynamic.md) | Resources are open: mapped + dynamic properties coexist (`ListValues` returns both) | Accepted |
| [0018](0018-decorators-are-syntactic-sugar.md) | Mapping attributes are syntactic sugar over `PropertyMapping<T>` | Accepted |
| [0019](0019-models-are-named-graphs-modelgroups.md) | Models are named graphs; ModelGroups query several at once | Accepted |
| [0020](0020-runtime-metadata-discovery.md) | Runtime metadata via global MappingDiscovery / OntologyDiscovery | Accepted |
| [0021](0021-store-creation-via-connection-strings.md) | Stores are created from connection strings | Accepted |
| [0022](0022-store-capabilities-and-istore-extension.md) | Store-defined capabilities & inferencing; extend via `IStore`; no capability negotiation | Accepted |
| [0023](0023-lazy-loading-via-resourcecache.md) | Lazy loading of linked resources via ResourceCache (not disablable) | Accepted |
| [0024](0024-sparql-reuses-registered-prefixes.md) | SPARQL queries reuse registered ontology prefixes | Accepted |
| [0025](0025-resource-identity-uriref-blanknodes.md) | Resource identity: fragment-aware URIs (UriRef), URNs, blank nodes | Accepted |
| [0026](0026-xsd-dotnet-datatype-mapping.md) | XSD ↔ .NET datatype mapping (culture-invariant literals) | Accepted |
| [0027](0027-localized-literals.md) | Language-tagged (localized) literals (rudimentary) | Accepted |
| [0028](0028-store-level-transactions.md) | Store-level transactions (ADO-style `ITransaction`) | Accepted |
| [0029](0029-resource-commit-rollback-change-tracking.md) | Resource change tracking & object-level Commit/Rollback (no cascade) | Accepted |
| [0030](0030-delete-removes-subject-and-object-triples.md) | Deleting a resource removes all triples referencing it (subject + object) | Accepted |
| [0031](0031-multimodal-query-results.md) | Multi-modal query results (resources / bindings / ASK / count) | Accepted |
| [0032](0032-data-virtualization-paged-collections.md) | Data virtualization via lazy, paged collections | Accepted |
| [0033](0033-exception-model.md) | Explicit exception model | Accepted |
| [0034](0034-rdf-serialization-formats.md) | RDF (de)serialization formats & JSON-LD resource converter | Accepted |

### Accepted (revival changes made)
| # | Title | Status |
|---|-------|--------|
| [0013](0013-replace-il-weaving-with-source-generator.md) | Replace IL weaving with a Roslyn source generator (partial properties) — weaver retired in 2.0 | Accepted |
| [0035](0035-remove-inotifypropertychanged.md) | Remove INotifyPropertyChanged support (resource NPC + virtualizing collections) | Accepted |

### Proposed (revival)
| # | Title | Status |
|---|-------|--------|
| [0014](0014-ontology-generator-modernization.md) | Reimplement ontology vocabulary generation as a compile-time generator/tool | Proposed |
| [0015](0015-modernize-target-frameworks-and-ci.md) | Modernize target frameworks, build, and CI | Proposed |

## Revival north star

Stabilize on modern .NET first, then add features. Two external consumers pin the
published package **1.0.3.77** — ElectrixOS (`elxgen`) and DevHub (`Relay`). The
public mapping surface (`Resource`, `[RdfClass]`/`[RdfProperty]`, `IStore`/`IModel`,
memory + Virtuoso stores, Turtle/JSON-LD I/O) is a **compatibility contract**;
changes are measured against those consumers.
