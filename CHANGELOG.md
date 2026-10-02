# Changelog

All notable changes to Semiodesk.Trinity are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Architectural decisions live in [`doc/adr/`](doc/adr/README.md); entries here link to the ADR that
records the reasoning. Release mechanics are in [`RELEASING.md`](RELEASING.md).

## [Unreleased]

Every value Trinity writes into SPARQL or SQL text now goes through one serializer for its kind, and a
value that cannot be written is refused, naming itself, rather than written incorrectly.
([ADR-0052](doc/adr/0052-one-serializer-for-literals-and-iris.md))

### Fixed

- **A string value containing a newline and three apostrophes was not stored as that value.** Any value
  holding a newline was written in the long form `'''…'''`, which escaped no quotes, so `'''` in the
  value ended the literal early and the rest was read as part of the update. Every literal is now the
  short double-quoted form with `\ " LF CR TAB` escaped. It affected every write and every query,
  because the preprocessor re-writes each literal through the same function — the LINQ writer's own,
  correct escaping included.
- **A value with a lone carriage return made `Commit()` throw**, because the CR was written raw into a
  single-quoted literal. **A several-line value ending in an apostrophe was refused by Fuseki and not
  written at all by Virtuoso**, which swallowed the error (#50).
- **`SerializeTypedLiteral` stored a string's quotes as part of its lexical form.** `"abc"^^xsd:anyURI`
  was stored as `"abc"` with the quotes, and an apostrophe ended the literal. The lexical form is now
  escaped like any other literal.
- **A graph IRI holding a `>` was not refused.** Commit's delta (every backend), its insert and replace,
  every model read's dataset clause, graph parameters, LINQ IRI terms, the layered view's extension
  functions and Virtuoso's own statements interpolated the IRI raw, so the text after the `>` was read
  as part of the query. All of them now write it through `SerializeIriRef`. An IRI dotNetRDF decodes
  from a `\u` escape is also checked before the preprocessor writes it back.
- **Virtuoso: replacing a graph whose IRI held an apostrophe deleted the data of other graphs.** The
  manager built `DELETE FROM … RDF_MAKE_IID_OF_QNAME('<iri>')` with the IRI between SQL quotes; it is
  now a command parameter.
- **Virtuoso: `ListModels` returned a graph named with `%3E` holding a raw `>`**, because it built each
  model from `Uri.ToString()`. It keeps the `OriginalString` now.
- **Oxigraph: a stored carriage return came back from a `SELECT` as a line feed.** It was asked for SPARQL
  XML results first, and an XML parser normalizes a raw CR to LF; it now asks for JSON results first. The
  stored value was always exact.
- **Re-binding a `FROM` parameter left the previous graph recorded**, so binding the first graph again
  was refused as already set.

### Changed

- **Every literal in a query's `ToString()` is now double-quoted** (`"Hallo"@de`, not `'Hallo'@de`).
  Code that compares generated SPARQL text must expect the new form.
- **`XsdTypeMapper.SerializeObject` of a string returns the plain value**, not the value between quotes.
- **`SparqlSerializer.SerializeTranslatedString` validates and lower-cases its tag**, as every other tag
  path already did, and refuses one that is not a language tag.
- **`Bind` refuses a `LIMIT` or `OFFSET` value that is not a non-negative integer** — including a numeric
  string — and a `FROM` value that is not a graph identifier.
- **`SparqlSerializer.SerializeIriRef` is public**, so a store adapter can write graph names through the
  same guard. **A blank node label is held to the characters of one** (`_:` plus letters, digits, `_`,
  `-` and inner dots); one that is not is refused.

### Removed

- The uncompiled `Trinity/Stores/Virtuoso/VirtuosoSpecific.cs`, left over from when Virtuoso support
  lived in core.

## [2.0.0-rc.4] - 2026-10-01

Everything merged since `2.0.0-rc.3` (2026-08-11). That is more than a release candidate usually
carries:

- **One more change to the authoring model.** Language-tagged text is now declared by property type
  rather than set as a mode on the resource, and `Resource.Language` is gone. Most of the migration
  shows up as compile errors, but three cases do not. Read
  [Upgrading from rc.3](#upgrading-from-rc3) before anything else.
- **Two features:** Oxigraph as a fourth store backend, and layered views. A layered view reads a
  graph with a pending change applied, stages commits into that change, and accepts or discards it.
- **Fixes, most of them for defects that were already in rc.3.** The ones to know about are silent:
  they reported success while losing a write or returning a wrong answer, so nothing in a log would
  show them.
  - `Read(..., update: true)` replaced the graph instead of adding to it, on Fuseki and GraphDB.
  - On every store, a bulk `UpdateResources` that included a resource the store did not have yet
    turned every other replace in the batch into a merge. On Fuseki, a bulk write into an empty
    model wrote nothing.
  - `ModelGroup.ContainsResource` returned `true` for any blank node on any non-empty group.
  - On .NET 10, every `HashSet<Uri>` and `Dictionary<Uri, …>` ignored the fragment of a `UriRef`.
  - `inferenceEnabled: true` on the in-memory store returned results without inference.
  - Virtuoso's `ContainsModel(Uri)` returned `true` for every URI.
- **Scaling.** Reading a resource with thousands of values was quadratic, and so was reading
  thousands of resources. Both are linear now. Mapped collections on Virtuoso are no longer capped
  at a few hundred members.

### Upgrading from rc.3

- **Localized text is declared by type, and `Resource.Language` is removed.**
  ([ADR-0048](doc/adr/0048-localized-literals-typed-containers.md), superseding ADR-0027.) Setting
  `Resource.Language` moved values between the mapping and the unmapped bag. A resource therefore
  showed one language at a time, and two threads reading it in different locales corrupted each
  other's data. Now the declared type decides what a property sees:

  | Declared type | Sees |
  |---|---|
  | `string`, `List<string>` | untagged literals only |
  | `LocalizedString` | every language, one value each |
  | `LocalizedStringCollection` | every language, several values each |
  | `LangString`, `List<LangString>` | tagged literals, as raw values |

  ```csharp
  [RdfProperty(RDFS.label)]
  public partial LocalizedString Title { get; }   // get-only: mutate it, don't assign it

  article.Title["de"] = "Bericht";
  article.Title.Languages;                        // ["de", "en"]
  article.Title.Best("de-AT", "en");              // RFC 4647 lookup
  ```

  The old flag tells you what to declare. A property with `languageInvariant: true` stays `string`.
  Every other `string` property over tagged data becomes `LocalizedString`.

  The following are removed, so each use is a compile error: `Resource.Language`,
  `IResource.Language`, `Language` and `LanguageInvariant` on `IPropertyMapping` and
  `PropertyMapping<T>`, and the `languageInvariant` constructor parameters.

  **Three cases compile and then behave differently:**
  - A property still declared `string` over data that is all language-tagged now reads `null`.
    Nothing is lost. The values stay on the resource, `ListValues(property)` and the new
    `ListLanguages()` still return them, and `Commit()` does not remove them.
  - Filtering values with `.OfType<Tuple<string,string>>()` now returns nothing, because a tagged
    literal is a `LangString`. Search your code for this one.
  - Casting a value to `Tuple<string,string>` throws `InvalidCastException`. A hand-written
    `PropertyMapping<Tuple<string,string>>` is refused when its assembly is registered.
- **A mapped property typed `System.Uri` is refused when its assembly is registered.** Declare it
  `UriRef` instead. ([ADR-0025](doc/adr/0025-resource-identity-uriref-blanknodes.md)) `Uri`
  equality ignores the fragment, but in RDF `…/x#a` and `…/x#b` are different resources. The
  generator warns with **TRIN007** at build time. `MappingDiscovery.RegisterAssembly` then reports
  the property in its `AggregateException`, in Release builds too. `UriRef` derives from `Uri`, so
  the declaration is the only thing to change.
- **`IModelGroup.DefaultModel` is removed.** Nothing ever read it, so setting it had no effect.
- **`IResource` has two new members**, `ListLanguages()` and `ListLanguages(Property)`. You only
  need to add them if you implement `IResource` without deriving from `Resource`.
- **Fuseki needs version 5.x.** ([ADR-0043](doc/adr/0043-fuseki-store-revival.md)) Jena 4.x answers
  HTTP 500 to any query that names a `urn:uuid:` IRI. `CreateResource()` mints those by default, so
  such a resource could be written but never read back.

### Added

- **Oxigraph as a fourth store backend** (`Semiodesk.Trinity.Oxigraph`, `provider=oxigraph`). It
  passes the same shared store suites as Fuseki, GraphDB and Virtuoso. Oxigraph has no reasoner, so
  `inferenceEnabled: true` **throws** rather than being ignored: an answer computed without
  inference looks exactly like a correct one. Because Oxigraph is strict, it exposed three defects
  in dotNetRDF's output that the other stores tolerate: invalid RDF/XML entity declarations, Turtle
  with a byte-order mark, and a catch-all `Accept` header that lets an `ASK` come back as plain
  text. The adapter works around all three. ([ADR-0047](doc/adr/0047-oxigraph-store.md))
- **Layered read views.** `store.CreateLayeredModel(baseline, additions, removals)` reads
  `(baseline − removals) ∪ additions` and leaves the baseline untouched. Where additions and
  removals overlap, additions win. Mapped reads and LINQ apply the overlay natively, and SPARQL you
  pass in is rewritten. A query the rewriter cannot handle faithfully is refused rather than
  answered from unsubtracted data: property paths, `GRAPH`, `SERVICE`, `CONSTRUCT`/`DESCRIBE`,
  `FILTER EXISTS`, a `FROM` of your own, blank nodes and inferencing. All three graphs must be in the
  same store. ([ADR-0041](doc/adr/0041-layered-read-views.md))
- **Staged writes through a layered view.** `Commit()` on a resource read through a view stages the
  change into the additions and removals graphs. `Accept()` applies the change to the baseline, and
  `Discard()` abandons it. `Accept()` refuses a stale change, meaning one that removes a triple the
  baseline no longer holds, because applying it would merge silently. `Accept(force: true)`
  applies it anyway. ([ADR-0042](doc/adr/0042-staged-writes-and-accept.md))
- **Materialized layered views.** Pass a fourth graph and the view keeps its effective triples
  there. Queries then run natively, which lifts the rewriting refusals except graph selection, and
  makes inferencing possible. Staging keeps the graph current in proportion to the change, not the
  baseline: in memory, 0.7 ms per stage against a 31.5 s rebuild at a million triples. `Refresh()`
  rebuilds the graph and **throws** if the store wrote less than it should have. Virtuoso writes
  nothing, and reports success, when a single `INSERT … WHERE` exceeds its transaction log.
  ([ADR-0042](doc/adr/0042-staged-writes-and-accept.md))
- **RDFS inferencing in the in-memory store.** `inferenceEnabled: true` was accepted and then
  ignored. It now reasons over the RDFS schema in the graphs loaded into the store. Entailments go
  into a side graph, so `inferenceEnabled: false` still answers without them.
  ([ADR-0045](doc/adr/0045-in-memory-rdfs-inferencing.md))
- **Localized text types:** `LangString` (one tagged literal), `LocalizedString`,
  `LocalizedStringCollection`, and `ListLanguages()` on resources. LINQ queries one language at a
  time: `Where(d => d.Title["de"] == "Bericht")`. Three things are refused in a query rather than
  approximated: `Best()`, projecting a single language, and `.Count`/`.Any()` on a container.
  ([ADR-0048](doc/adr/0048-localized-literals-typed-containers.md))
- **Diagnostics:**
  - **TRIN007**: a mapped `System.Uri` property.
  - **TRIN008**: the obsolete `languageInvariant` flag.
  - **TRIN009**: a localized-text container that declares a setter.
  - **TRIN010**: an `ILocalizedText` type other than the two the engine supports.
- `Resource.IsPartiallyLoaded` is `true` when loading a mapped property failed partway. A large
  collection now loads in batches, so a store failure mid-read leaves the earlier batches in the
  collection. The exception still reaches the caller, and the next read reloads the collection.
  This property lets a caller that catches the exception tell a truncated collection from a
  complete one.
- `Uri.CanBeQuerySubject()` answers whether an identifier may be named as a query subject. It is
  named for that decision rather than for a property of the node, so a guard that asks the wrong
  question reads wrong at the call site. `IsBlankId()` remains for "is this a blank node".
- **A cross-store benchmark harness** (`benchmarks/Trinity.Benchmarks`, not shipped). Each
  BenchmarkDotNet workload pairs the mapped path with the hand-written SPARQL it stands in for, on
  all five backends, and checks that the work landed before its time counts. CI runs a
  one-iteration in-memory smoke pass, and no timing gates anything.
  ([ADR-0050](doc/adr/0050-benchmark-harness.md))

### Changed

- **A LINQ `member == "constant"` on a mapped string looks the value up instead of filtering on
  it.** The filter cannot use an index, so a point lookup grew with the model: 28.5 ms at 100k
  resources on Virtuoso, and 746 ms in memory. With the lookup, those are 1.6 ms and 0.7 ms. It is a
  trade, though. Because the lookup is evaluated before it is joined, `Any`, `First` and `Count`
  over a value that **many** resources share get slower. On Oxigraph the lookup only adds cost,
  because Oxigraph pushes the binding into neither a sub-select nor a `UNION`. A non-string literal
  with the same lexical form, such as `"5"^^xsd:int` for `== "5"`, no longer matches; a mapped
  `string` never read one anyway. ([ADR-0051](doc/adr/0051-linq-equality-lookup.md))
- **A mapped `string` in LINQ matches untagged literals only**, as it does on read. Before, this was
  true only for `==`: `StartsWith` matched tagged values, and `Select` returned them unwrapped.
- **Language tags are validated and lower-cased** when a `LangString` is constructed, so a store
  returning `de-DE` round-trips as `de-de`. The validation follows the SPARQL/Turtle `LANGTAG`
  grammar and does not check BCP 47 subtag lengths, so a long tag written by another program never
  makes a read throw. The validation also has a security purpose: a tag is written into the query
  as syntax, so an unvalidated tag taken from request data could inject SPARQL into an update.
- **The generator emits exactly the accessors a property declares**, each with its own modifiers.
  Get-only, `init` and `private set` mapped properties now compile. Before, the generator always
  emitted a bare `get` and `set`, so a get-only property was a CS9253 error.
- **`DeleteResources` on Virtuoso** now uses the same per-resource loop as the other stores. A batch
  spanning several models deletes each resource from its own model rather than throwing
  `NotSupportedException`. An empty batch does nothing rather than throwing. A batch costs one round
  trip per resource, about 1.2 s for 1500 resources.
- `Model.GetResources(IEnumerable<Uri>, …)` and the `ModelGroup` equivalent validate their arguments
  **eagerly**, at the call, rather than on the first `MoveNext()`. A caller that builds the
  enumerable and never enumerates it now gets `ArgumentException` for a type that does not
  implement `IResource`.
- `GetResource(Uri, Type, ITransaction)` refuses a blank node with `ArgumentException`, like its
  siblings. Before, the exception was wrapped in a `TargetInvocationException`.
- Without `dataset=`, the Fuseki provider now uses `ds`, Jena's conventional dataset name. Before,
  it used the literal `dataset`, which produced a store that connected, reported ready, and returned
  404 for every query.
- The SPARQL endpoint store uses dotNetRDF's `SparqlQueryClient` in place of the obsolete
  `SparqlRemoteEndpoint`. It now holds an `HttpClient`, so dispose the store when you are done
  with it.

### Deprecated

- The two-argument `RdfPropertyAttribute(string, bool)` constructor. The `languageInvariant` flag
  has no effect any more, and the generator reports it as **TRIN008**. The constructor will be
  removed in 2.1.

### Removed

- `Resource.Language`, `IResource.Language`, and `Language`/`LanguageInvariant` on
  `IPropertyMapping` and `PropertyMapping<T>`. See [Upgrading from rc.3](#upgrading-from-rc3).
- `IModelGroup.DefaultModel`.

### Fixed

#### Writes that reported success and lost data

- **`Read(..., update: true)` replaced the graph instead of adding to it on Fuseki and GraphDB.**
  Both wrote through a Graph Store `PUT`, which replaces the graph by definition, so everything it
  already held was lost. The shared test checked only the value just added, so it passed. Additions
  now go through `UpdateGraph`, as they always did on Virtuoso, and the test checks the value that
  was already there. ([ADR-0047](doc/adr/0047-oxigraph-store.md))
- **On every store, a bulk `UpdateResources` merged instead of replacing whenever the batch held a
  resource the store did not have yet.** The write put every resource's pattern in one `OPTIONAL`
  block. A subject with no triples gave the whole block no solutions, so every `DELETE` was skipped
  while the `INSERT` still ran. Every other resource then kept its old values next to its new ones.
  A single-valued mapped property shows only one of them, so the damage was invisible through the
  mapping. Subjects are now bound with `VALUES`. The same block also cost the product of the
  resources' triple counts rather than their sum: six resources with six triples each took 1374 ms,
  and twenty now take 6 ms.
- **A bulk write into a model that holds no triples was silently lost on Fuseki.**
  `UpdateResources` scoped its updates with `WITH <g>`. On Jena, a graph-scoped modify against a
  graph with no triples matches nothing, applies nothing, and still answers success. So every
  resource in the batch vanished, at any batch size, with HTTP 204 in reply. Every `DELETE`/`INSERT`
  template in `StoreBase` now names its graph with `GRAPH` instead, and the store creates that graph
  as it inserts. `CREATE SILENT GRAPH` is not an alternative, because Jena has no empty named graphs;
  this was measured. The same scoping was in the singular `UpdateResource` and in the shared delta
  builder, so the defect went beyond bulk writes. All of them are fixed together.
- **A whole-resource write duplicated blank-node-valued links** on the in-memory store, Fuseki and
  GraphDB. A SPARQL modify instantiates its `INSERT` template once per solution and mints a fresh
  blank node each time. A resource with six existing triples therefore gained six links, to six
  empty nodes. The insert is now its own `INSERT DATA` operation, which also makes the cost linear:
  1000 resources went from 1870 ms to 66 ms. Virtuoso was not affected.
- **`VirtuosoStore.UpdateResources` is removed rather than fixed.** It was a copy of the base method
  that differed only in clause order, so it silently missed the fixes above. Virtuoso now inherits
  the base method, and its suite passes without the copy.
- **Virtuoso's `DeleteResources` deleted nothing past about 950 resources.** Its override built one
  `FILTER` for the whole batch. Virtuoso refused the statement, and the adapter swallowed the error
  ([#50](https://github.com/semiodesk/trinity-rdf/issues/50)). The override is gone (see Changed).
- **`Read(stream, ..., leaveOpen: true)` closed the caller's stream on every store**, because
  disposing a plain `StreamReader` also closes the stream beneath it.

#### Wrong answers

- **`ModelGroup` accepted blank nodes as query subjects and answered wrongly.** `ContainsResource`,
  `GetResource` and `GetResource<T>` had no guard. `ContainsResource` interpolates the identifier
  into a triple pattern, where a bare `_:b0` is a fresh variable rather than a reference. It
  therefore matched any subject and returned **`true` for any non-empty group**. The other two
  leaked a raw `RdfParseException` out of the query layer. All three now use the same guard as
  `Model`.
- **`UriRef` ignored its fragment on .NET 10.** .NET 10 added `IEquatable<Uri>` to `System.Uri`, and
  `EqualityComparer<T>.Default` prefers that interface. As a result, every `HashSet<Uri>`,
  `Dictionary<Uri, …>`, `Contains` and `Distinct` compared `UriRef`s without their fragment. The core
  library targets netstandard2.0, so a consumer was affected just by running on .NET 10, without
  recompiling. `UriRef` now implements `IEquatable<Uri>` and declares `==`/`!=`. One case cannot be
  fixed: after `Uri a = someUriRef;`, the expression `a == b` binds to `Uri`'s operator at compile
  time. TRIN007 and the registration check exist for that case.
  ([ADR-0025](doc/adr/0025-resource-identity-uriref-blanknodes.md))

  The same change fixed these:
  - `UriRef` can now be a mapped type. Before, `PropertyMapping<UriRef>` threw in Debug builds, and
    writing a `UriRef` value threw "No serialiser available".
  - The LINQ translator could drop an `rdf:type` constraint, which loosened the query.
  - `Resource.Equals` now compares `OriginalString`. It used to disagree with `GetHashCode` on every
    runtime.
  - LINQ now writes IRIs the same way the write path does. Before, it went through `AbsoluteUri`,
    which threw on a blank node.
  - `select x.Uri` no longer throws `Unsupported projection`.
- **`ContainsModel(Uri)` was wrong on two stores.** Virtuoso returned `true` for every URI. It
  counted the rows of an `ASK`, which always has exactly one row, instead of reading the answer.
  Fuseki returned `false` for every URI, because it discarded the result of its check. The
  in-memory store returned `true` for `null`.
- **Fuseki adapter fixes:**
  - `CreateModelGroup(params IModel[])` returned an empty group.
  - `IsReady` stayed `true` after `Dispose`.
  - A connection string with a password but no user name lost its credentials.
- **`GetResource<T>` on GraphDB could return a different resource.** GraphDB's `DESCRIBE` also
  returns the triples that point *at* the subject. For a subject with no triples of its own, the
  answer held only its referrers, and `GetResource<T>` returned the first resource in the answer
  without checking its IRI. It now returns only the resource asked for, and throws
  `ArgumentException` when that resource is not in the answer.
- **`GetResources` with an empty or `null` subject set no longer reads the whole model.** On `Model`
  the constraint was omitted, leaving `SELECT ?s ?p ?o WHERE { ?s ?p ?o. }`, which loaded every
  triple in the model as resources. `ModelGroup` had no guard at all. An empty set produced the
  invalid `FILTER ( )`, and `null` threw `NullReferenceException`. Both now return an empty result
  without issuing a query.
- **Language-tagged values had several defects caused by `Resource.Language`**, which are fixed by
  removing it:
  - `GetValue(Property)` returned a tuple whose value was `null`.
  - `HasProperty(p, v, "DE")` returned `false` after `AddProperty(p, v, "DE")`.
  - A difference in tag case produced spurious `DELETE`/`INSERT` pairs on `Commit()`.
  - LINQ `Select(p => p.Name)` threw `InvalidCastException` whenever another resource had a tagged
    value on the same predicate.
- **An `ASK` query containing a sub-select was misread.** The preprocessor took the query form from
  the inner `SELECT`, and it inserted the model's `FROM` at a position where dotNetRDF refuses it.

#### Errors

- **A percent-encoded subject IRI no longer breaks the query.** The query builder used
  `Uri.ToString()`, which returns the *display* form and unescapes percent-encoding. Where the
  unescaped character is one that SPARQL forbids in an `IRIREF` (`%20` and `%3E` were measured),
  the query became a parse error: `RdfParseException: Illegal white space in URI`. That error took
  every other subject in the query down with it. Subjects are now serialized through
  `SparqlSerializer.SerializeUri`, which uses `OriginalString`. An audit found the same defect in
  three more places, all now fixed:
  - the `?s a <type>` constraint in `Model.GetResources<T>()`;
  - the `PREFIX` line injected into every query that uses a registered prefix;
  - the datatype IRI of every typed literal written.

  This is unrelated to the .NET 10 `Uri` change above. It is in serialization, not identity, and it
  behaves the same on .NET 8 and .NET 10.
- **An IRI that cannot be written verbatim is refused, and the error names it.** Before, it became an
  `RdfParseException` inside an unrelated batch. `SerializeUri` serializes from `OriginalString` and
  deliberately **not** from `AbsoluteUri`. `AbsoluteUri` would normalize host case, ports, dot
  segments and percent-encoding case. Trinity compares and hashes resources on the exact
  `OriginalString`, so normalizing would break de-duplication in mapped collections and drop LINQ
  rows.
- **A blank-node-valued link no longer breaks the read of its whole collection.** No SPARQL query
  can address a blank node by its label: it is not a legal `VALUES` operand, and in a query it is a
  variable rather than a reference. The old code wrote it as the invalid relative IRI `<_:0>`,
  which failed the query for every other subject in it. Blank ids are now skipped, and
  `ResourceCache` materializes them as unresolved resources, so the link still appears in its
  collection with its identity. Removing such a link still fails; see Known issues.
- **Blank-node handling no longer confuses how a node is spelled with whether it is blank.**
  Virtuoso returns blank nodes as `nodeID://b10000`. These are flagged as blank ids, but they are
  absolute IRIs. Deciding *serialization* on the flag wrote them without brackets and broke writing
  them at all; serialization now decides on the spelling. Deciding *whether an identifier may be
  named as a query subject* on the `IsBlankId` property alone missed a caller-built
  `new UriRef("_:0", UriKind.RelativeOrAbsolute)` and let a bare label into a query. That decision
  now goes through `Uri.CanBeQuerySubject()`, which refuses every blank node on every store. The
  refusal is uniform on purpose. Virtuoso's identifiers look addressable, and `ContainsResource`
  finds them. But `GetResource` binds the subject, and a bound IRI never matches a blank-node
  subject. Allowing them would add a capability that half works on one backend and does not exist
  on the others.
- `PREFIX` declarations and datatype IRIs are always written in brackets. They used the term
  serializer, which writes a blank node label without brackets.
- A `null` URI is reported as `null`. For a while, the guards told a caller who passed `null` that
  the identifier was a blank node.
- `Model.GetResources` no longer throws `NullReferenceException` on the `null` entries that a
  missing `rdf:type` can produce. This matches `ModelGroup`.
- **TriG read from a file returned `null` on Fuseki and GraphDB**, after it had written the data.
  Callers took the `null` as a failure.
- **TriG read from a string or stream was not parsed on GraphDB.** Each backend had its own copy of
  the format switch, and GraphDB's copy had no TriG case, so the document went to the RDF/XML
  parser. The switch is now shared as `StoreBase.TryParse`. The same change makes `https` URLs work
  when reading a graph from a URL. GraphDB, Virtuoso and the in-memory store accepted only `http`
  and returned `null` for `https`.
- **GraphDB parsed an `application/xml` query response twice.** After the fallback parser
  succeeded, a second parser ran over the stream it had already consumed.

#### Scaling

- **Mapped collections are no longer capped at a few hundred members on Virtuoso.**
  `IModel.GetResources(IEnumerable<Uri>, Type, ITransaction)` performs the bulk lazy load under
  *every* mapped-property dereference. It constrained its subjects with an equality chain:
  `FILTER(?s = <a>||?s = <b>||…)`. Virtuoso parses that chain as nested pairs, and its SPARQL
  compiler limits the nesting depth. Past that limit, every read of such a collection failed with
  `SP031: The nesting depth of subexpressions exceed limits of SPARQL compiler`. **So did every
  write**, because `Add`, `Remove` and `Link` read the collection before changing it. Subjects are
  now bound with `VALUES`, in batches of 1000.

  On Virtuoso 7.2.12 and 7.2.14, through the real store path, the chain compiled 1024 subjects and
  failed at 1025, while `VALUES` compiles 4094. The limit is a compile-time constant of the server
  build (one consumer hit it at 157), and `ThreadStackSize` does not change it. That is why the
  chain was removed rather than split into chunks. Reads also got faster at every size: 28 ms
  instead of 132 ms at 300 subjects, and 83 ms instead of 957 ms at 1000. `Model`, `ModelGroup` and
  layered views share one implementation of this read (`BulkResourceReader`).
  ([ADR-0046](doc/adr/0046-bulk-subject-binding-with-values.md))
- **`GetResource<T>` was quadratic in the resource's own number of triples.** There were two causes.
  The graph result was read by position, and dotNetRDF's triple collection has no indexer, so each
  lookup enumerated the collection from the start. Separately, the in-memory store described the
  subject once for each triple it had. A resource with 2000 links took 1.5–1.8 s and about 3 GB on
  the in-memory store, Oxigraph and Fuseki, and 26 s and 15.6 GB on GraphDB. It now takes
  60–120 ms and about 40 MB. Virtuoso has its own result reader and was not affected.
- **Reading N resources was quadratic, on every store.** Both result readers searched a `List`
  before emitting each resource. `GetResources<T>()` of 32,000 resources on Virtuoso went from
  14.3 s to 0.91 s.
- **Virtuoso's `DeleteResource` scanned the whole graph.** Its override filtered `?s ?p ?o` instead
  of binding the resource, so each delete grew with the model, to 36 ms at 64k resources. It now
  binds the resource and stays at about 0.8 ms.

### Known issues

Open at release. Several are on Virtuoso and are **silent**: the adapter's direct-query path
swallows the server's error ([#50](https://github.com/semiodesk/trinity-rdf/issues/50)), so a
refused write reports success.

- Virtuoso: a single statement over 10,000 entries is refused. `Read` of more than about 10k triples
  throws, while `Refresh()` of a large materialized view, a large `UpdateResources` batch, and any
  large update write nothing.
  ([#70](https://github.com/semiodesk/trinity-rdf/issues/70))
- Virtuoso: `Commit()` of a new resource with a few thousand values writes nothing.
  ([#72](https://github.com/semiodesk/trinity-rdf/issues/72))
- Virtuoso: some update paths write graph and resource IRIs without `SerializeUri`, so an IRI with a
  space or `>` in it is not updated. ([#75](https://github.com/semiodesk/trinity-rdf/issues/75))
- Virtuoso: an inferenced, paged `GetResources<T>(offset, limit)` always throws.
  ([#73](https://github.com/semiodesk/trinity-rdf/issues/73))
- Virtuoso: `Read(string, …)` cannot parse N-Triples, and it treats JSON-LD, TriG and N-Quads
  content as a file name. ([#55](https://github.com/semiodesk/trinity-rdf/issues/55))
- Virtuoso: an equality lookup does not find a string with an emoji that was loaded through Turtle.
  ([#76](https://github.com/semiodesk/trinity-rdf/issues/76))
- An `xsd:integer` outside the `Int32` range throws `OverflowException` on read, on every store.
  Oxigraph stores every `long` as such an integer.
  ([#54](https://github.com/semiodesk/trinity-rdf/issues/54))
- An upper-case host in an IRI loses its case. On Fuseki, GraphDB and Virtuoso this includes graph
  names, so a `Read` into `http://Example.org/g` appears to vanish.
  ([#53](https://github.com/semiodesk/trinity-rdf/issues/53))
- A singular `Commit()` of a hand-constructed resource merges it into the stored one instead of
  replacing it. ([#49](https://github.com/semiodesk/trinity-rdf/issues/49))
- `JsonResourceConverter` does not round-trip localized containers, so a JSON edit to one is lost.
  JSON-LD is not affected. ([#51](https://github.com/semiodesk/trinity-rdf/issues/51))
- `ModelGroup.GetResources<T>()` throws `NotImplementedException`.
  ([#56](https://github.com/semiodesk/trinity-rdf/issues/56))
- `CreateModelGroup(params IModel[])` returns an empty group on `SparqlEndpointStore`, and on
  `dotNetRDFStore` unless it is called through `IStore`.
  ([#68](https://github.com/semiodesk/trinity-rdf/issues/68))
- Removing a **blank-node-valued** link still fails, because a blank node is not legal in a SPARQL
  `DELETE` template. The read half is fixed and covered. The write half is still quarantined as
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
- **Breaking: language-tagged text is declared by property type** (`string`, `LocalizedString`,
  `LocalizedStringCollection`, `LangString`), and `Resource.Language` and the `languageInvariant`
  flag are removed. ([ADR-0048](doc/adr/0048-localized-literals-typed-containers.md))
- **Breaking: a mapped `System.Uri` property is refused at registration**; declare it `UriRef`.
  `IModelGroup.DefaultModel` is removed. ([ADR-0025](doc/adr/0025-resource-identity-uriref-blanknodes.md))

### Added

- Layered read views with working-copy semantics — `(baseline − removals) ∪ additions` — including
  staged writes, `Accept`/`Discard`, and optional materialization.
  ([ADR-0041](doc/adr/0041-layered-read-views.md), [ADR-0042](doc/adr/0042-staged-writes-and-accept.md))
- `Commit()` writes a per-value delta instead of rewriting the whole resource, so concurrent writers
  touching different values no longer erase each other.
  ([ADR-0039](doc/adr/0039-resource-write-semantics.md))
- Fuseki is a first-class backend, and Oxigraph a new one. All four store suites run in CI against
  Dockerized servers. ([ADR-0043](doc/adr/0043-fuseki-store-revival.md),
  [ADR-0044](doc/adr/0044-store-suites-green-and-in-ci.md), [ADR-0047](doc/adr/0047-oxigraph-store.md))
- In-memory RDFS inferencing. ([ADR-0045](doc/adr/0045-in-memory-rdfs-inferencing.md))

### Fixed

- `UriRef` identity survives .NET 10, which added `IEquatable<Uri>` to `System.Uri` and thereby made
  every `HashSet<Uri>`/`Dictionary<Uri,…>` fragment-blind without a recompile.
  ([ADR-0025](doc/adr/0025-resource-identity-uriref-blanknodes.md))

[Unreleased]: https://github.com/semiodesk/trinity-rdf/compare/v2.0.0-rc.4...develop
[2.0.0-rc.4]: https://github.com/semiodesk/trinity-rdf/releases/tag/v2.0.0-rc.4
