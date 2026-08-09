# 0036. Store-integration tests self-provision servers via Testcontainers

Date: 2026-07-14

## Status
Accepted (2.0)

## Context
The three store-integration test projects — `tests/Trinity.Tests.{Fuseki,GraphDB,Virtuoso}` —
exercise the real store backends against a running server. They previously required a manually
provisioned server on that store's **default port** (Fuseki 3030, GraphDB 7200, Virtuoso 1111),
with credentials/repositories set up by hand (documented only as "How to run" comments in each
setup class). Consequences:

- They could not run unattended, so they were excluded from CI and rarely exercised.
- As shipped they were not even runnable via the CLI: each project is an `Exe` whose `Program.Main`
  is a no-op (the runner calls are commented out) and it referenced neither `Microsoft.NET.Test.Sdk`
  nor `NUnit3TestAdapter`, so `dotnet test` discovered nothing.
- Fixed default ports collide with any store a developer already runs locally.

## Decision
Each integration project provisions its own server in Docker via **Testcontainers for .NET**
(`Testcontainers`, centrally versioned in `Directory.Packages.props`):

- A NUnit `[SetUpFixture]` per project (`FusekiContainer`, `GraphDBContainer`, `VirtuosoContainer`)
  starts the container once for the whole assembly (`[OneTimeSetUp]`) and disposes it afterwards
  (`[OneTimeTearDown]`).
- **Ports:** the container's service port is bound to a **random free host port**
  (`WithPortBinding(servicePort, assignRandomHostPort: true)`); the connection string is built from
  `container.Hostname` + `container.GetMappedPublicPort(servicePort)`. A random ephemeral port is the
  strongest guarantee against colliding with a server already running locally — stronger than any
  fixed "custom" port, which could itself be taken.
- **Readiness:** Fuseki waits on `GET /$/ping`, GraphDB on `GET /rest/repositories`, Virtuoso on the
  `"Server online at 1111"` log line.
- **Provisioning:** GraphDB has no repository out of the box, so `GraphDBContainer` POSTs a Turtle
  repository config to `/rest/repositories` to create `trinity-rdf` after start. Fuseki creates its
  dataset via `FUSEKI_DATASET_1=ds`; Virtuoso needs only `DBA_PASSWORD=dba`.
- The published connection string is consumed by the existing setups (`SetupClass.ConnectionString`,
  `VirtuosoTestSetup`/`GraphDBTestSetup` `ConnectionString` properties). Model/base URIs remain fixed
  identifiers — they need not track the mapped port.
- `Microsoft.NET.Test.Sdk` + `NUnit3TestAdapter` were added (with `GenerateProgramFile=false` to keep
  the existing no-op `Main`) so the suites run under `dotnet test`.

Images are pinned: `stain/jena-fuseki:4.0.0`, `ontotext/graphdb:10.8.0`,
`openlink/virtuoso-opensource-7:latest`. (`stain/jena-fuseki` is used rather than
`secoresearch/fuseki` because it serves the dataset query endpoint at `/ds/query`, which is what
dotNetRDF's `FusekiConnector` targets.) The long-standing copy-paste bug in the Fuseki test csproj
(`AssemblyName`/`RootNamespace` said *Virtuoso*) was fixed at the same time.

## Consequences
- `dotnet test tests/Trinity.Tests.{Virtuoso,GraphDB,Fuseki}` now works on any machine with a Docker
  daemon, with no manual server setup and no port conflicts.
- Results at the time of this change: **Virtuoso 90/97** and **GraphDB 97/101** pass; the remaining
  handful are pre-existing store-specific edge cases. **Fuseki is 4/86**: the container starts and the
  store connects (`IsReady`), but nearly every query fails with HTTP 404 because dotNetRDF's
  `FusekiConnector` POSTs to `/ds/query` where this server rejects POST (GET works), and graph
  deletes 405. This is a pre-existing provider/dependency bug — the store's own code carries a TODO
  about the query URL — exposed, not caused, by making the suite runnable. Tracked with the other
  Fuseki provider issues in [0009](0009-supported-store-backends.md).
- These suites stay out of the **default** CI job (Docker availability + large image pulls). They
  could be enabled in a separate CI job/matrix later (GitHub Actions ubuntu runners provide Docker).
- New test dependency on a Docker daemon; if absent, the `[SetUpFixture]` fails fast with a clear
  Testcontainers error.

## Related
- [0009](0009-supported-store-backends.md) (store backends + the Fuseki provider issues),
  [0011](0011-configuration-model.md) (the setups seed graphs with `LoadGraphs`, not config),
  [0015](0015-modernize-target-frameworks-and-ci.md) (CI).
