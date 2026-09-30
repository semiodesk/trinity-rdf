# 0049. Cross-store benchmark harness: in-process, verified, never a gate

Date: 2026-09-28

## Status
Accepted

## Context
Every performance claim in the ADRs so far was measured with a throwaway harness that was deleted
afterwards: ADR-0041's read ratios, ADR-0042's 31.5 s rebuild and 0.7 ms stage and 12.5 s → 3 ms
delete, ADR-0046's VALUES table. ADR-0042 names the problem itself — *"a claim that is only measured
can rot silently"* — and lists the figures among what is measured but not tested, because *"timings
do not belong in a correctness suite."*

Both halves hold. What is missing is a third place: somewhere the figures an ADR rests on can be
reproduced on demand, and where an optimization can be measured before and after on the same
workload. The 2.0 revival is about to start profiling and optimizing the mapping layer, and without
that place every optimization would begin by writing another throwaway harness.

## Decision
A BenchmarkDotNet project, `benchmarks/Trinity.Benchmarks`, outside `tests/`, in the solution.

**It measures Trinity, not the stores.** Every workload pairs the mapped operation with the
hand-written SPARQL it stands in for, and the raw row is BenchmarkDotNet's `Baseline`, so the `Ratio`
column is the cost of the mapping. A class that measures several operations gives each its own
category and baseline (the config groups by category). `InMemory` is a backend like the others and
is the floor: no network, no server.

**It reuses the store suites' Testcontainers fixtures** rather than copying their provisioning. A
second copy of setup logic is how GraphDB lost its TriG case (ADR-0047).

**One in-process job, built in code.** In-process, because the default toolchain spawns a process
per case and would restart every container for every cell. Monitoring with one invocation and ten
iterations, because these operations take milliseconds to seconds and the default pilot hunts for a
nanosecond-stable figure that does not exist. The job is built in `Program.Config()`: stacking
`[InProcess]` on `[SimpleJob]` declares two jobs, not one, and still prints a plausible table. Its
per-case timeout is raised to two hours: the in-process default is five minutes, and a case that
exceeds it does not fail — it ends the whole run, which a 1M layered case does in memory before its
first iteration.

**Every measurement proves it measured something.** A write verifies what landed, outside the timed
region, and throws if it did not; a read verifies its fixture before it is timed. This is not
caution: Virtuoso *silently writes zero* above its transaction-log limit (ADR-0042), and a benchmark
that crossed it would report an excellent time for doing nothing.

**Backends are chosen with `TRINITY_BENCH_BACKENDS`**, read by a `ParamsSource`. BenchmarkDotNet has
no parameter filter (`-p` is its profiler switch), and an unselected backend is then never started.

**Fixtures are bulk-loaded** as N-Triples through `IStore.Read`, in 250k-triple chunks — half of
what ADR-0042 measured Virtuoso accepting — unless seeding is what the workload measures. Seeding
through the mapper costs one or two requests per resource and would dominate the run.

**The 1M tier is opt-in.** The `Layered*` classes run at 10k and 100k; a `…Large` subclass in the
`Large` category runs at 1M and is filtered out unless `--large` is passed. Layered baselines are
named after their size and kept between cases, since `[GlobalSetup]` runs once per case.

**CI runs a smoke pass and gates nothing on time.** `--smoke` runs every case once, at the smallest
value of each numeric parameter, against InMemory. It exists so a workload that throws — including a
guard that fires — fails the build rather than the next person to run it. Absolute numbers depend on
the host, disk and Docker runtime; a threshold would be noise.

**Results are exported, not committed.** The config adds the full JSON exporter, and the README
documents a before/after comparison on one machine with `dotnet/performance`'s ResultsComparer.
Results belong to the machine that produced them.

**Profiling bypasses BenchmarkDotNet.** `profile <Type.Method>` runs one benchmark method in a plain
loop with its own setup, cleanup and guards, for `dotnet-trace`, `dotnet-counters` or an IDE
profiler. BenchmarkDotNet's `EventPipeProfiler` needs an out-of-process toolchain, which the
containers rule out.

## What building it found
The first smoke passes, before any tuning, and on the in-memory store unless noted. They are leads
for the profiling work this harness exists for, recorded here so they are not rediscovered. The
figures are single-iteration smoke numbers, which is enough for the orders of magnitude.

- **`Model.GetResource<T>(uri)` is O(model) in memory:** 3.2 ms per lookup at 1,000 resources,
  376 ms at 100,000, against a flat ~37 µs for the subject-bound `SELECT`. `dotNetRDFStore.GetDescribeQuery`
  places its `VALUES` *after* the `?s ?p ?o` it constrains, so the engine enumerates the graph before
  binding the subject — the "VALUES before the pattern" rule ADR-0041 states for the overlay. The
  untyped `GetResource(uri)`, with its `FILTER`, stays flat at ~300 µs. A layered view's
  `GetResource<T>` is *faster* than the plain model's for the same reason (276 µs against 160 ms at
  100k), which inverts ADR-0041's in-memory ratio.
- **The LINQ equality lookup is O(model) too:** 3.6 ms at 1k, 596 ms at 100k, against a flat 143 µs.
- **Traversing a large mapped collection is super-linear:** 158 links take 34 ms, 1,000 take 551 ms,
  2,000 take 1.8 s and allocate 3.4 GB — against 2–32 ms for the same VALUES queries issued by hand.
  The query shape ADR-0046 chose is not the cost; what Trinity does with the result is.
- **An in-memory `ModelGroup` is O(data) per query.** Any query with more than one `FROM` costs about
  1 s at 100,000 triples, raw or mapped alike, because dotNetRDF rebuilds the merged default graph
  each time. The group adds little of its own; the engine is the cost. A layered view, which scopes
  with `GRAPH` rather than merging, does not pay it.
- **A mapped `long` above `int.MaxValue` cannot be read back from Oxigraph** (#54; any out-of-range `xsd:integer` throws, on every store) — found by
  `WideResourceBenchmarks`, whose setup fails there with an `OverflowException`. Oxigraph canonicalizes
  every integer-derived datatype to `xsd:integer`, in all four result formats (verified against the
  pinned 0.5.5 image), and `XsdTypeMapper` deserializes `xsd:integer` as `Int32` before ADR-0040's
  conversion to the declared type ever runs. So a value Trinity wrote as `xsd:long` overflows on the way
  back. `NumericRoundTripTest` round-trips a mapped `400L`, which fits in an `Int32`, so it does not catch
  this. The fix belongs in `XsdTypeMapper` (`xsd:integer` is unbounded) with a test in the shared numeric
  round-trip suite; the benchmark is left failing on Oxigraph until then, since shrinking its values to
  fit would hide the defect.
- **Virtuoso cannot read N-Triples from a string** (#55). `VirtuosoStore.ReadTripleFormat` keeps its own
  parser switch, with no N-Triples case, and throws `NotSupportedException` — the duplicated-switch
  pattern ADR-0047 replaced with `StoreBase.TryParse` elsewhere, surviving in Virtuoso's string path.
  The harness seeds as Turtle for that reason; `SerializationBenchmarks`' N-Triples read keeps failing
  there until it is fixed.
- **Virtuoso cannot read JSON-LD, TriG or N-Quads from a string at all** (#55). `ReadQuadFormat(string, …)`
  hands the document to `IStoreReader.Load(ITripleStore, string)`, whose string is a **file name**, so
  the content is opened as a path (`PathTooLongException` for any realistic document). Virtuoso has no
  `MultiGraphTrigTest` subclass, so nothing exercises this path.
- **`ModelGroup.GetResources<T>()` throws `NotImplementedException`** (#56). The group workload goes
  through the query overload instead.
- **`GetResources<T>(ISparqlQuery)` does not accept the `;` shorthand.** It requires the
  preprocessor to see exactly `?s ?p ?o` in scope, and does not follow `?s a <T> ; ?p ?o`.
- **ADR-0042's 31.5 s full build does not reproduce: `Refresh()` takes 5.1 minutes at 1M in memory**
  (`LayeredMaterializeLargeBenchmarks`, one iteration). It scales roughly linearly: 10.5 s at 50k,
  22.7 s at 100k. So the cost is a constant factor, not a complexity class, and it is about 10x a raw
  `INSERT … WHERE` copy of the baseline (0.97 s at 50k, 2.4 s at 100k). Either the overlay became
  more expensive after ADR-0042 was measured — ADR-0041 records the set-semantics fix adding a nested
  `NOT EXISTS` to the additions branch — or the figure was taken on a different path. It is the first
  lead to profile (`profile LayeredMaterializeBenchmarks.Refresh --param BaselineTriples=100000`).
- **A clean `Accept()` of ~100 changes takes ~4.6 s on a 10k in-memory baseline**, where staging the
  same changes takes well under a millisecond each.
- **Mapping is 15–25x the raw query for a full `GetResources<T>()`** in memory, and allocates 14–17x
  as much.

Against the four Docker backends, the smoke pass runs all 108 cases on Fuseki and GraphDB without a
failure. Oxigraph fails the one wide-resource read above, and Virtuoso fails the N-Triples and JSON-LD
string reads above. Each failing cell is a defect in Trinity, not in the harness.

Apart from the full build above, the ADR figures the harness was built to reproduce hold in shape: staging through a materialized view
costs ~0.4 ms per change (ADR-0042: 0.7 ms), a materialized `DeleteResource` stays in the hundreds of
microseconds, and the view's `ContainsResource` and selective caller query come out at 1.4–4.9x the
baseline (ADR-0041: 3.45x and 4.23x in memory).

## Consequences
- An optimization starts from a named workload and ends with a before/after pair from one machine,
  rather than from a harness written for the occasion.
- An ADR that makes a performance claim can point at the class that reproduces it; the README's
  workload table maps each class to the ADR it reproduces.
- The benchmarks cost the fast CI job about a minute and a half. They are not a correctness suite: a
  guard that fires is a harness failure, and the defect it points at still needs a test in the right
  project.
- Virtuoso's materialized 1M cells fail in setup, and its raw 1M copy fails its verification. That
  is ADR-0042's finding reproduced, and the README says so beside the table.
- GraphDB's container runs with reasoning on (`rdfsplus-optimized`); its write numbers include
  inference and are not comparable to the other backends' without saying so.
- The harness holds a mapped model of its own (`BenchmarkPerson`, `BenchmarkWideResource`) rather
  than reusing a test fixture, so an unrelated test change cannot move a benchmark number.

## Related
- ADR-0019, 0023, 0030, 0034, 0037, 0039, 0040, 0041, 0042, 0046 — the behaviour the workloads measure
- ADR-0036, 0044 — the Testcontainers fixtures reused here
- ADR-0047 — the duplicated-setup failure the fixture reuse avoids
- `benchmarks/README.md` — how to run, read, compare and profile
