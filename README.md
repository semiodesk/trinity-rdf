# Semiodesk.Trinity

[![CI](https://github.com/semiodesk/trinity-rdf/actions/workflows/ci.yml/badge.svg)](https://github.com/semiodesk/trinity-rdf/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Semiodesk.Trinity.svg)](https://www.nuget.org/packages/Semiodesk.Trinity)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

An object mapper for RDF knowledge graphs in .NET.

Trinity maps RDFS/OWL vocabulary onto plain C# classes, so you work with objects and LINQ
instead of hand-writing SPARQL and marshalling triples. It runs on top of
[dotNetRDF](https://dotnetrdf.org) and talks to in-memory graphs, SPARQL endpoints, Virtuoso,
GraphDB and Fuseki through one API.

## Quick start

```bash
dotnet add package Semiodesk.Trinity --prerelease
```

Describe your domain as `partial` classes. The RDF terms are the mapping — there is no separate
schema file, and no configuration:

```csharp
using System;
using System.Collections.Generic;
using Semiodesk.Trinity;

[RdfClass("http://xmlns.com/foaf/0.1/Person")]
public partial class Person : Resource
{
    public Person(Uri uri) : base(uri) { }

    [RdfProperty("http://xmlns.com/foaf/0.1/name")]
    public partial string Name { get; set; }

    [RdfProperty("http://xmlns.com/foaf/0.1/age")]
    public partial int Age { get; set; }

    [RdfProperty("http://xmlns.com/foaf/0.1/knows")]
    public partial List<Person> Knows { get; set; }
}
```

Then read and write them like any other objects:

```csharp
MappingDiscovery.RegisterAssembly(Assembly.GetExecutingAssembly());

var store = StoreFactory.CreateStore("provider=dotnetrdf");
var model = store.CreateModel(new Uri("http://example.org/contacts"));

var alice = model.CreateResource<Person>(new Uri("http://example.org/alice"));
alice.Name = "Alice";
alice.Age = 34;
alice.Commit();

var adults = from p in model.AsQueryable<Person>()
             where p.Age >= 18 && p.Name.StartsWith("A")
             orderby p.Name
             select p;
```

## How the mapping works

A Roslyn source generator ships inside the package as an analyzer. For every `partial` member
carrying `[RdfProperty]` it supplies the implementing half at compile time — the property mapping
field, the getter and setter, and a `GetTypes()` override derived from `[RdfClass]`. Nothing runs
after the build, so `dotnet build` is all you need on Windows, Linux and macOS.

It handles scalars, collections, language-invariant strings, resource references, multiple
`[RdfClass]` attributes and inheritance. Only `partial` members are generated — and if you forget
the keyword, or nest a mapped type, or leave out the `Uri` constructor, the generator says so at
build time (`TRIN001`–`TRIN005`) rather than leaving you with a mapping that quietly does nothing.

Mapped resources stay open: a `Person` can still carry predicates your class never declared, and
`ListValues()` returns both the mapped and the unmapped ones. RDF is not forced into a closed
schema.

## Querying

LINQ queries are translated to SPARQL by Trinity's own query provider and executed on the store —
nothing is fetched and filtered in memory. `Where`, `OrderBy`, `Skip`/`Take`, `Any`/`All`,
`Count`, `OfType`, `SelectMany`, string and math operations, and navigation across linked
resources are supported.

When you want the query language itself, it is right there — and registered vocabulary prefixes
are already in scope:

```csharp
var result = model.ExecuteQuery(new SparqlQuery(
    "SELECT ?s WHERE { ?s a foaf:Person ; foaf:name ?name . FILTER(CONTAINS(?name, 'A')) }"));

foreach (var person in result.GetResources<Person>()) { /* … */ }
```

## Stores

Models are named graphs. A store hands them out, and a `ModelGroup` spans several while still
behaving like a single model.

| Store | Package | Connection string |
|---|---|---|
| In-memory / files | `Semiodesk.Trinity` | `provider=dotnetrdf` |
| SPARQL endpoint | `Semiodesk.Trinity` | `provider=sparqlendpoint;endpoint=<uri>` |
| Virtuoso | `Semiodesk.Trinity.Virtuoso` | `provider=virtuoso;host=<host>;port=1111;uid=<user>;pw=<password>` |
| GraphDB | `Semiodesk.Trinity.GraphDB` | `provider=graphdb;host=<uri>;uid=<user>;pw=<password>;repository=<id>` |
| Fuseki | `Semiodesk.Trinity.Fuseki` | `provider=fuseki;host=<uri>;uid=<user>;pw=<password>;dataset=<name>` |

Backends are registered explicitly at startup:

```csharp
StoreFactory.LoadProvider<VirtuosoStoreProvider>();
```

Graphs are read and written in the usual serializations — Turtle, TriG, N-Triples, N3, RDF/XML
and JSON-LD — via `store.Read(...)`/`store.Write(...)`, with `store.LoadGraphs(...)` as a thin
helper for seeding schema or background graphs at startup.

> The Fuseki backend is functional but currently affected by an upstream connector issue and
> should be considered experimental in this release.

## Why RDF

- **A standard data model.** RDF, RDFS, OWL and SPARQL are W3C standards with two decades of
  implementations behind them. Data and queries move between vendors.
- **SPARQL instead of a proprietary query language.** Close enough to SQL to be familiar, and
  supported by every serious triple store.
- **Schema that is data.** Ontologies are themselves RDF, stored alongside the data they
  describe, and can change while the application runs.
- **Inferencing where you want it.** Class *and* property hierarchies, equality, transitivity —
  most RDF databases can answer queries over facts that were never explicitly stored.

## Requirements

The libraries target `netstandard2.0`. Because mapped members are partial properties, consuming
projects need a **C# 13 compiler — .NET SDK 9.0 or later** — with `<LangVersion>13</LangVersion>`
or higher. 

## Documentation

Architecture Decision Records under [`doc/adr`](doc/adr/README.md) document the design and the
reasoning behind it, including the mapping generator, the LINQ provider and the store model.

## License

MIT — see [LICENSE](LICENSE). Use it in commercial projects.

Originally created by Moritz Eberl and Sebastian Faubel at Semiodesk GmbH.

## Contributing and support

Issues and pull requests are welcome at
[github.com/semiodesk/trinity-rdf](https://github.com/semiodesk/trinity-rdf). For contributions,
please confirm you hold the rights to publish the code under the MIT license.
