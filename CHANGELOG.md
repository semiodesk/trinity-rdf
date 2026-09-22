# Changelog

All notable changes to Semiodesk.Trinity are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Architectural decisions live in [`doc/adr/`](doc/adr/README.md); entries here link to the ADR that
records the reasoning. Release mechanics are in [`RELEASING.md`](RELEASING.md).

## [Unreleased]

## [2.0.0-rc.4] - 2026-09-22

### Fixed

- **Mapped collections are no longer capped at a few hundred members on Virtuoso.**
  `IModel.GetResources(IEnumerable<Uri>, Type, ITransaction)` — the bulk lazy load under *every*
  mapped-property dereference — constrained its subjects with an equality chain
  (`FILTER(?s = <a>||?s = <b>||…)`). Virtuoso parses that as nested binary pairs and its SPARQL
  compiler caps the depth, so past a threshold every read of such a collection failed with
  `SP031: The nesting depth of subexpressions exceed limits of SPARQL compiler` — **and every write
  too**, because `Add`/`Remove`/`Link` read the collection before mutating it. Subjects are now
  bound with `VALUES`, in batches of 1000.
  Measured through the real store path on Virtuoso 7.2.12 and 7.2.14: the chain compiles 1024
  subjects and fails at 1025; `VALUES` compiles 4094. The threshold is a compile-time constant of
  the server build — one consumer reported it as low as 157 — and is not tunable via
  `ThreadStackSize`, which is why the shape was removed rather than chunked.
  Reads also got faster at every size: 28 ms vs 132 ms at 300 subjects, 83 ms vs 957 ms at 1000.
  ([ADR-0046](doc/adr/0046-bulk-subject-binding-with-values.md))
- **`GetResources` with an empty or `null` subject set no longer reads the whole model.**
  On `Model` the constraint was simply omitted, leaving `SELECT ?s ?p ?o WHERE { ?s ?p ?o. }` —
  every triple in the model, materialized as resources. On `ModelGroup` there was no guard at all:
  an empty set emitted the syntactically invalid `FILTER ( )`, and `null` threw
  `NullReferenceException`. Both now return an empty result without issuing a query.
- **Subjects are no longer corrupted by `Uri.ToString()`.** The chain interpolated `Uri.ToString()`,
  which unescapes percent-encoding, so a resource stored under an escaped spelling was silently not
  found. Subjects are serialized through `SparqlSerializer.SerializeUri`, which uses `OriginalString`.
- **A blank-node-valued link no longer breaks the read of its whole collection.** A blank node label
  cannot be addressed by any SPARQL query — it is not a legal `VALUES` operand, and in a query it
  means an existential variable rather than a reference. The old code emitted it as the invalid
  relative IRI `<_:0>`, taking every addressable subject in the query down with it. Blank ids are now
  skipped, and `ResourceCache` materializes them as unresolved resources, so the link still appears
  in its mapped collection with its identity. Removing such a link still fails — see Known issues.
- `Model.GetResources` no longer throws `NullReferenceException` on the null entries a missing
  `rdf:type` can produce, matching `ModelGroup` and `LayeredModel`; and a long-dead `if` whose body
  had drifted out from under it was removed.

### Changed

- `Model.GetResources(IEnumerable<Uri>, …)` and the `ModelGroup` equivalent now validate their
  arguments **eagerly** rather than on the first `MoveNext()`, matching `LayeredModel`. A caller that
  builds the enumerable and never enumerates it will now see `ArgumentException` for a non-`IResource`
  type at the call.
- `LayeredModelSparql.BindSubjects` was promoted to `SparqlSerializer.GenerateSubjectBinding`, so
  `Model`, `ModelGroup` and `LayeredModel` share one implementation. Both are internal; no public API
  changed.

### Known issues

- Removing a **blank-node-valued** link still fails: a blank node is not legal in a SPARQL `DELETE`
  template. The read half of this is fixed and covered; the write half remains quarantined as
  `ResourceWriteSemanticsTest.CanRemoveBlankNodeValuedLink`. See
  [`doc/known-test-failures.md`](doc/known-test-failures.md) and
  [ADR-0039](doc/adr/0039-resource-write-semantics.md).

## [2.0.0] - unreleased

2.0 is a deliberate breaking release. See [`README.md`](README.md) for the full migration notes.

### Changed

- **Breaking: mapped classes and their `[RdfProperty]` properties must now be `partial`**
  (C# 13 / .NET 9+). Mapping is produced by a Roslyn source generator that ships inside the package
  as an analyzer; the `cilg` IL weaver and its MSBuild targets are gone, so builds are cross-platform
  with no post-build tooling. ([ADR-0013](doc/adr/0013-replace-il-weaving-with-source-generator.md))
- **Breaking: the configuration subsystem was removed** — `ontologies.config`/`app.config` loading,
  `InitializeFromConfiguration` and `CreateStoreFromConfiguration`. Seed graphs with `store.Read` or
  `store.LoadGraphs(...)`, register vocabularies via `OntologyDiscovery`, and create stores from a
  connection string. ([ADR-0011](doc/adr/0011-configuration-model.md))
- **Breaking: vocabulary classes are generated author-time and committed**, by the
  `Semiodesk.Trinity.Vocabulary.Tool` dotnet tool (`trinity-vocab`), replacing the net4x
  `Trinity.OntologyGenerator`. ([ADR-0014](doc/adr/0014-ontology-generator-modernization.md))
- Target frameworks are netstandard2.0 (libraries) and net8.0 (tools and tests); dotNetRDF upgraded
  to 3.5.2; the LINQ provider was rebuilt in-house and re-linq retired.
  ([ADR-0015](doc/adr/0015-modernize-target-frameworks-and-ci.md),
  [ADR-0038](doc/adr/0038-upgrade-dotnetrdf-3.md), [ADR-0037](doc/adr/0037-linq-provider-rebuild.md))

### Added

- Layered read views with working-copy semantics — `(baseline − removals) ∪ additions` — including
  staged writes, `Accept`/`Discard`, and optional materialization.
  ([ADR-0041](doc/adr/0041-layered-read-views.md), [ADR-0042](doc/adr/0042-staged-writes-and-accept.md))
- `Commit()` writes a per-value delta instead of rewriting the whole resource, so concurrent writers
  touching different values no longer erase each other.
  ([ADR-0039](doc/adr/0039-resource-write-semantics.md))
- Fuseki is a first-class backend, and all three store suites run in CI against Dockerized servers.
  ([ADR-0043](doc/adr/0043-fuseki-store-revival.md), [ADR-0044](doc/adr/0044-store-suites-green-and-in-ci.md))
- In-memory RDFS inferencing. ([ADR-0045](doc/adr/0045-in-memory-rdfs-inferencing.md))

### Fixed

- `UriRef` identity survives .NET 10, which added `IEquatable<Uri>` to `System.Uri` and thereby made
  every `HashSet<Uri>`/`Dictionary<Uri,…>` fragment-blind without a recompile.
  ([ADR-0025](doc/adr/0025-resource-identity-uriref-blanknodes.md))

[Unreleased]: https://github.com/semiodesk/trinity-rdf/compare/develop...HEAD
