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
second copy of setup logic is how GraphDB lost its TriG case (ADR-0047). Each fixture is started
once per process and stopped at exit. A backend that fails to start is **not retried**: every case
runs its own `[GlobalSetup]`, so a retry would start a fresh multi-GB container for each of up to 232
cases. The half-started fixture is stopped, and the first failure is rethrown for the rest.

**The harness registers its own mappings only.** The test assembly is registered for its ontology
prefixes, not its mappings: its `Linq.Person` also maps `foaf:Person`, and an untyped read would build
that wider test class instead of `BenchmarkPerson`. Editing a test would then move a benchmark number.

**One in-process job, built in code.** In-process, because the default toolchain spawns a process
per case and would restart every container for every cell. Monitoring with one invocation and ten
iterations, because these operations take milliseconds to seconds and the default pilot hunts for a
nanosecond-stable figure that does not exist. The job is built in `Program.Config()`: stacking
`[InProcess]` on `[SimpleJob]` declares two jobs, not one, and still prints a plausible table. Its
per-case timeout is raised to two hours: the in-process default is five minutes, and a case that
exceeds it does not fail — it ends the whole run, which a 1M layered case does in memory before its
first iteration.

**Every measurement proves it measured something.** A write verifies what landed, outside the timed
region, and throws if it did not; a read verifies its fixture before it is timed, and checks its own
answer inside the timed region, which costs one comparison. This is not caution: Virtuoso *silently
writes zero* when one statement exceeds 10,000 entries (#50, #70), and a benchmark that crossed that
would report an excellent time for doing nothing. A read that resolves none of its members is faster than one that
resolves all of them.

The guards check **content, not just counts**, wherever a count can come out right by accident:
- The layered changeset is deliberately unbalanced, 120 triples added against 100 removed. With a
  balanced one, a view that ignored its layers entirely had exactly the effective size.
- The materialized graph is checked for the added names and against the removed ones.
- Staging must put the replaced value in removals, not only the new one in additions. Otherwise a
  single-valued property would read two values, the ADR-0042 hazard.
- A staged delete must cover the victim's object side as well as its subject side. The object side is
  what ADR-0042's two-bound-patterns fix changed.

Each of these was checked by breaking the path in a throwaway worktree and confirming the guard fails:
a view delete that skips the object side, staging that never writes removals, a bulk load that
resolves nothing, and the test mappings registered again.

**Backends are chosen with `TRINITY_BENCH_BACKENDS`**, read by a `ParamsSource`. BenchmarkDotNet has
no parameter filter (`-p` is its profiler switch), and an unselected backend is then never started.

**Fixtures are bulk-loaded** as N-Triples, read as Turtle, through `IStore.Read`, unless seeding is
what the workload measures. Seeding through the mapper costs one or two requests per resource and
would dominate the run. The load is chunked at 5,000 triples, half of the 10,000 entries Virtuoso
accepts in one statement (#70). It was 250,000, half of what ADR-0042 measured Virtuoso accepting, but
past 10,000 Virtuoso's `Read` fails outright, so no Virtuoso case above the smallest size had run.

**The 1M tier is opt-in.** The `Layered*` classes run at 10k and 100k; a `…Large` subclass in the
`Large` category runs at 1M and is filtered out unless `--large` is passed. Layered baselines are
named after their size and kept between cases, since `[GlobalSetup]` runs once per case.

The tier is 1M **triples** (200,000 resources), and Trinity never turns that baseline into objects.
Each timed operation touches a handful of resources — a point read, ten staged changes, an `Accept()`
of a hundred — or is a store-side `INSERT … WHERE`. The question it answers is whether a small change
costs more because the data under it is large, which is what ADR-0042's O(changes) design claims. It
is not a "load everything" test. On the in-memory backend, though, the *store* holds those triples in
the process (6.8 GB observed), and nobody should run a dataset that size on dotNetRDF's in-memory
store. So an in-memory 1M cell stress-tests dotNetRDF, not a usage pattern. The tier's realistic
targets are the server backends.

**CI runs a smoke pass and gates nothing on time.** `--smoke` runs every case once, at the smallest
value of each numeric parameter, against InMemory. It exists so a workload that throws — including a
guard that fires — fails the build rather than the next person to run it. Absolute numbers depend on
the host, disk and Docker runtime; a threshold would be noise. A smoke case may run for 15 minutes,
not the measuring job's two hours, so a hung case fails the fast job instead of holding it.

**Results are exported, not committed.** The config adds the full JSON exporter, and the README
documents a before/after comparison on one machine with `dotnet/performance`'s ResultsComparer.
Results belong to the machine that produced them.

**Profiling bypasses BenchmarkDotNet.** `profile <Type.Method>` runs one benchmark method in a plain
loop with its own setup, cleanup and guards, for `dotnet-trace`, `dotnet-counters` or an IDE
profiler. BenchmarkDotNet's `EventPipeProfiler` needs an out-of-process toolchain, which the
containers rule out.

## What building it found
Measured with the full job (one warmup, ten iterations) on 2026-09-30. The default tier ran completely
on the in-memory store, Oxigraph and Fuseki, and for 187 of its 232 cases on GraphDB. Virtuoso and the
1M tier were not run, by decision: the 1M tier because the realistic question at that size is already
answered at 100k, Virtuoso for time. Absolute figures belong to that machine; the ratios and the growth
with size are what carry over.

The test that sorts these findings is whether a cost follows Trinity across backends. Allocation is
the clean signal, because the store's own work happens in another process.

**Trinity's own costs, the same on every backend**
- **`GetResource<T>` is O(n²) in the described resource's triple count** (#63). A resource with 2000
  values takes 1.5–2.1 s and allocates 2.8–3.4 GB (in-memory, Oxigraph, Fuseki). On GraphDB it takes
  26.5 s and 16 GB, because its reasoner adds inferred triples to each `DESCRIBE`. `GraphTripleProvider`
  reads triple k with `Triples.ElementAt(k)`, walking the collection each time, and the in-memory store's
  `DESCRIBE` override describes the subject once per triple. Fixing both, measured in a throwaway
  worktree, makes it linear: 16 ms and 9.9 MB at 2000. The lazy load underneath, which the fan-out
  benchmark was built to measure, turned out linear (32 MB for 2000 members). It had looked guilty only
  because every traversal starts with a `GetResource<T>` of the hub.
- **Materializing allocates ~25 KB per two-triple resource** (#67): 24.6–25.6 MB for 1000 resources on
  all four backends, 35× the raw query's time in memory and 4–5× on Fuseki and GraphDB. Not yet traced.

**Query shapes Trinity emits that some engines cannot index.** The fix is in Trinity either way.
- **A LINQ equality lookup filters instead of binding** (#64). At 100k resources it takes 725 ms in
  memory and 749 ms on Oxigraph, against a flat 0.2–0.6 ms by hand. Fuseki and GraphDB optimize the
  shape and stay flat. #52 made the comparison lexical (`STR(?v) = …`), which is no more indexable, so a
  fix must keep that semantics.
- **A layered view's `GetResource<T>` grows with the baseline on Oxigraph and Fuseki** (#65): 314 ms
  (446×) and 46 ms (12.8×) at 100k. GraphDB (0.9×) and the in-memory store stay flat, and so do
  materialized views everywhere. `VALUES` precedes the overlay, as ADR-0041 requires, but the binding has
  to be pushed through `MINUS` and a nested `FILTER NOT EXISTS`, and those engines don't do it.
- **`Model.GetResource(uri)` and `ModelGroup.GetResource` bind the subject with `FILTER (?s = …)`** (#66),
  the shape ADR-0042 replaced in the staging paths. On Oxigraph that is 46.7 ms at 100k (about 130×),
  and 47× through a 16-member group. It is flat elsewhere. The first figure, 129 ms, was inflated: the
  untyped read was building the test assembly's `Linq.Person` (see the review below).

**Costs of dotNetRDF's in-memory engine.** They matter for tests and development, not production.
- **`Accept()` is O(baseline) in memory only:** 2.4 s at 10k and 30 s at 100k for the same ~100 changes,
  allocating 12 GB. The servers take 8–86 ms.
- **`Refresh()` is 11× a plain copy in memory only.** The 5.1 minutes measured at 1M is why ADR-0042's
  31.5 s did not reproduce there. On the servers the overlay costs the same as the copy (0.8–1.2×):
  1.0–5.0 s at 100k.
- **Any query with more than one `FROM` rebuilds the merged default graph**, so an in-memory `ModelGroup`
  is O(data) per query (60 ms per lookup across 16 members, 7× one model). The same reads through a group
  on Fuseki and GraphDB are 0.6–1.1× one model. Oxigraph's 47× is #66's `FILTER`, not the group.
- **`GetResource<T>` is also O(model) in memory**, independently of #63: the override's `VALUES` follows
  the pattern it constrains. That is 441 ms per lookup at 100k resources, against 36 µs by hand.

**Defects the backends surfaced**, each left failing in its cell so it stays visible:
- **A mapped `long` above `int.MaxValue` cannot be read back from Oxigraph** (#54). More generally,
  any `xsd:integer` outside the `Int32` range throws on every store. Oxigraph turns every integer
  datatype into `xsd:integer`, and `XsdTypeMapper` deserializes that as `Int32` before ADR-0040's
  conversion runs. `WideResourceBenchmarks` fails its setup there, so all eight of its Oxigraph cells
  fail.
- **Virtuoso cannot read N-Triples, JSON-LD, TriG or N-Quads from a string** (#55). Its string path has
  its own parser switch with no N-Triples case, and it passes quad-format content to a dotNetRDF overload
  that takes a file name. The harness seeds as Turtle for that reason.
- **Virtuoso refuses any single statement over 10,000 entries** (#70). `Read` of more than about 10k
  triples throws. Everything else writes nothing, because the error is swallowed (#50):
  - materializing a view past 10,000 effective triples, where `Refresh()`'s own check then throws;
  - `UpdateResources`, which chunks by 1000 subjects, not by triples, so 1000 wide resources are
    21,000 triples in one statement;
  - any caller update of that size through `ExecuteNonQuery`. So a materialized view cannot be used on
  Virtuoso past 10k. Found only after the review below unbalanced the layered changeset: the old one
  produced exactly 10,000 effective triples. The timed rows that cross the limit are left failing:
  `SerializationBenchmarks`' reads at 10k people and `WideResourceBenchmarks`' writes at 1000
  resources. Their fixtures are seeded in chunks, so the rest of each class still measures.
- **`CreateModelGroup(params IModel[])` returns an empty group** on `SparqlEndpointStore`, and on
  `dotNetRDFStore` when called through the concrete type rather than `IStore` (#68).
- **`ModelGroup.GetResources<T>()` throws `NotImplementedException`** (#56). The group workload uses the
  query overload instead.
- **`GetResources<T>(ISparqlQuery)` does not follow the `;` shorthand.** It needs the preprocessor to see
  exactly `?s ?p ?o` in scope.

**What reproduced.** Staging through a view stays O(changes) on every backend: 0.25–22 ms per change,
1.8–4.3× the minimal writes by hand, flat from 10k to 100k. So does a materialized `DeleteResource`, at
0.8–23 ms. The view's `ContainsResource` and selective caller query come out at 1.0–3.4× the baseline,
in line with ADR-0041. Writes, deletes, literal updates, paging, `Count`, `Contains` and serialization
are all within 0.9–1.5× of hand-written SPARQL on the servers. Mapping is not where those spend their
time.

## The review, and what it changed
A review of #57 found that several guards could not notice when the work they check didn't happen, so
a regression would have read as a speedup. All of it was fixed before merge:
- The layered changeset was balanced, so a view that ignored its layers had the right size. It is now
  unbalanced, and the guards check content.
- Staging never checked that the old value reached removals, and a staged delete was checked on its
  subject side only.
- Read rows returned counts nobody compared. They now check their answers.
- The test assembly's mappings were registered, so the untyped read built `Linq.Person`. This skewed
  #66's first figure.
- A failed container start was retried for every case, and the half-started container leaked.
- Bad `--param` and `--iterations` values failed late or with the wrong message.
- The CI smoke run inherited the two-hour per-case timeout.
- Duplicate cells: the serialization raw rows, and the baseline and group rows of the layered read,
  ran once per value of a parameter they don't depend on. Both are now one method per variant.

A second review pass found three more gaps, fixed the same way:
- The serialization export guard looked for one IRI, while an export's order is up to the store. It
  now parses the document back, locally, and counts triples.
- `profile` treated every `ArgumentException` from setup as a usage error. It now catches only the
  fixtures' own `BenchmarkParameterException`, and cleans up and rethrows anything else.
- Two fixture seeds exceeded Virtuoso's limit.

Each new guard was checked by breaking its path in a throwaway worktree and watching it fail. The
stronger guards immediately found #70 on Virtuoso. Deferred to #69: routing the raw rows' IRIs through
`SerializeUri`, and loading the test ontologies once instead of per case.

## Consequences
- An optimization starts from a named workload and ends with a before/after pair from one machine,
  rather than from a harness written for the occasion.
- An ADR that makes a performance claim can point at the class that reproduces it; the README's
  workload table maps each class to the ADR it reproduces.
- The benchmarks cost the fast CI job about two minutes. They are not a correctness suite: a
  guard that fires is a harness failure, and the defect it points at still needs a test in the right
  project.
- Virtuoso's materialized cells fail in setup at every size: past 10,000 effective triples, the
  materializing update is refused, the error is swallowed (#50), and `Refresh()` finds nothing written
  (#70). The old balanced changeset produced exactly 10,000, right at the limit, which is why those
  cells used to pass. They are left failing so the defect stays visible.
- GraphDB's container runs with reasoning on (`rdfsplus-optimized`); its write numbers include
  inference and are not comparable to the other backends' without saying so.
- The harness holds a mapped model of its own (`BenchmarkPerson`, `BenchmarkWideResource`) rather
  than reusing a test fixture, so an unrelated test change cannot move a benchmark number.

## Related
- ADR-0019, 0023, 0030, 0034, 0037, 0039, 0040, 0041, 0042, 0046 — the behaviour the workloads measure
- ADR-0036, 0044 — the Testcontainers fixtures reused here
- ADR-0047 — the duplicated-setup failure the fixture reuse avoids
- `benchmarks/README.md` — how to run, read, compare and profile
