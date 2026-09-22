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
- **A percent-encoded subject IRI no longer breaks the query.** The chain interpolated
  `Uri.ToString()`, which returns the *display* form and unescapes percent-encoding. Where the
  unescaped character is one SPARQL forbids inside an `IRIREF` — `%20` and `%3E`, measured — the query
  became a parse error (`RdfParseException: Illegal white space in URI`) that took every other subject
  in it down too. Subjects are now serialized through `SparqlSerializer.SerializeUri`, which uses
  `OriginalString`. Unrelated to the .NET 10 `Uri` equality change in
  [ADR-0025](doc/adr/0025-resource-identity-uriref-blanknodes.md): this is the serialization path, not
  identity, and it behaves identically on .NET 8 and .NET 10.
- **`LayeredModel` had none of the above.** The `VALUES` fix, the batching and the blank-id skipping
  landed in `Model` and `ModelGroup` only, so a mapped collection read through
  `store.CreateLayeredModel(...)` still issued one unbounded block — and a blank-node member made the
  *entire* collection unreadable, where a plain model returns the rest. All three implementations now
  share one loop (`BulkResourceReader`), so the emitted shape cannot diverge again.
- **Blank-node handling distinguished the label from the flag.** Virtuoso returns blank nodes as
  `nodeID://b10000` — flagged as blank ids, but absolute IRIs that can be queried. Deciding
  serialization on the flag emitted them bare and dropped them from bindings; deciding on the
  `IsBlankId` property alone missed a consumer-built `new UriRef("_:0", UriKind.RelativeOrAbsolute)`
  and let a bare label into the query. Both now key on the spelling (`IsBlankNodeLabel`).
- **An IRI that cannot be written verbatim is now refused, naming itself**, instead of becoming an
  `RdfParseException` inside an unrelated batch. `SerializeUri` still serializes from `OriginalString`
  and deliberately **not** from `AbsoluteUri`, which would normalize host casing, ports, dot-segments
  and percent-encoding case — and Trinity compares and hashes resources on the ordinal
  `OriginalString`, so normalizing would break mapped-collection dedup and drop LINQ rows.
- `PREFIX` declarations and datatype IRIs use a strict serializer that always brackets, rather than the
  term serializer that emits a blank node label bare.
- **The same defect, found by audit in three more query builders**, each of which would turn into a
  parse error for a percent-encoded IRI: `Model.GetResources<T>()` (the `?s a <type>` constraint built
  from `[RdfClass]`), `SparqlPreprocessor.AddPrefix` (the `PREFIX` line injected into **every** query
  that uses a registered namespace prefix), and `SparqlSerializer.SerializeTypedLiteral` (the datatype
  IRI of every typed literal written). All three now route through `SparqlSerializer.SerializeUri`.
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

### Added

- `Resource.IsPartiallyLoaded` — true when loading a mapped property failed partway. Because a large
  collection now loads in batches, a store failure mid-read leaves the earlier batches in the
  collection. The exception still reaches the caller and the load is self-healing on the next read,
  but a caller that catches it can now tell a truncated collection from a complete one.
- `Uri.IsBlankNodeLabel()` — the lexical counterpart to `IsBlankId()`. Ask `IsBlankId()` whether
  something *is* a blank node; ask `IsBlankNodeLabel()` whether it can be written into a query.

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
