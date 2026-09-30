# Benchmarks

Cross-store performance measurement for Trinity, **through the lens of the library** — not a
SPARQL-engine shootout. The decisions behind the harness are in
[ADR-0049](../doc/adr/0049-benchmark-harness.md).

```bash
# everything, every backend (needs Docker; pulls four images)
dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --filter "*"

# one workload, two backends
TRINITY_BENCH_BACKENDS=InMemory,Oxigraph dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --filter "*PointRead*"

# the million-triple tier that reproduces ADR-0041/0042
TRINITY_BENCH_BACKENDS=InMemory dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --filter "*Layered*" --large

# what CI runs: every case once, smallest sizes, in memory
dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --filter "*" --smoke

dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --list flat
```

Every backend except the in-memory one is provisioned in Docker by the **same Testcontainers
fixtures the store test suites use** — their classes are public and their `StartAsync` works outside
NUnit, so provisioning is not copied here. A backend that is not selected is never started.

## Options

| | |
|---|---|
| `TRINITY_BENCH_BACKENDS` | Comma-separated backends: `InMemory`, `Oxigraph`, `Fuseki`, `GraphDB`, `Virtuoso`. Unset means all. An unknown name throws. An environment variable because BenchmarkDotNet has no parameter filter — its `-p` is the **profiler** switch. |
| `--large` | Include the `Large` category: the 1,000,000-triple layered workloads. Excluded otherwise; expect tens of minutes per backend. In memory, leave out the group rows (`--filter "*LayeredReadLarge*View" "*LayeredReadLarge*Baseline" …`): an in-memory group query costs ~10 s at 1M, so those rows alone run for hours. |
| `--smoke` | One iteration, no warmup, only the smallest value of each numeric parameter, and `InMemory` unless `TRINITY_BENCH_BACKENDS` says otherwise. Proves the workloads run and their guards hold; the timings mean nothing. Exits non-zero if any case failed. A case may take 15 minutes here, not two hours, so a hung one fails fast. |
| `--artifacts <dir>` | BenchmarkDotNet's own: where the reports go. Use one directory per side of a comparison. |
| `profile …` | Bypasses BenchmarkDotNet; see [Profiling](#profiling). |

Everything else is passed to BenchmarkDotNet unchanged.

## Workloads

Each workload pairs the mapped path with **the hand-written SPARQL it stands in for**, and one of the
rows is BenchmarkDotNet's `Baseline`. Classes that measure several operations put each in its own
category with its own baseline, so every `Ratio` is against the right thing.

| Class | Measures | Baseline | Reproduces |
|---|---|---|---|
| `WriteBenchmarks` | creating resources one `Commit()` at a time, with and without `CreateResource`'s existence check | one batched `INSERT DATA` | |
| `BatchWriteBenchmarks` | `UpdateResources` at batch sizes 1–1000 | the same batches by hand | |
| `ReadBenchmarks` | `GetResources<T>()` and a LINQ filter over the whole model | `SELECT` bindings | |
| `PointReadBenchmarks` | `GetResource` (typed and untyped), `ContainsResource`, a LINQ equality lookup, at 1k and 100k resources | subject- or value-bound `SELECT`/`ASK` | a flat row is an index, a growing one is a scan (ADR-0042) |
| `UpdateBenchmarks` | the per-value delta commit: one literal, one added link | `DELETE DATA` + `INSERT DATA` | ADR-0039 |
| `DeleteBenchmarks` | `DeleteResource`, subject and object side, at 1k and 10k | two bound `DELETE WHERE`s | ADR-0030, ADR-0042's two-bound-patterns rule |
| `LazyLoadBenchmarks` | the N+1 cost of always-on lazy loading | one `SELECT` for the whole shape | ADR-0023 |
| `FanOutBenchmarks` | one resource with 10/158/1000/2000 links | the same VALUES queries by hand | ADR-0046's table (13 → 170 ms on Virtuoso) |
| `ModelGroupBenchmarks` | reads through a group of 1/4/16 member graphs | the same read on one model holding the union | ADR-0019 |
| `LayeredReadBenchmarks` | reads through a layered view, rewriting and materialized | the same read on the plain baseline | ADR-0041's read table (1.31x / 3.45x / 2.24x / 4.23x) |
| `LayeredStagingBenchmarks` | staging a change, a delete, `Accept()`, `Discard()` | the minimal writes into the layers | ADR-0042: 0.7 ms stage, 3 ms delete |
| `LayeredMaterializeBenchmarks` | a full `Refresh()` | copying the baseline graph | ADR-0042: 31.5 s at 1M — 5.1 min in memory, where it is 11× a plain copy; on the servers ≈ a copy (ADR-0049) |
| `WideResourceBenchmarks` | twenty mapped values per resource, both directions | typed literals by hand | ADR-0040 |
| `LinqShapeBenchmarks` | paging, `Count()`, `Any()`, `Contains` | the SPARQL each should become | ADR-0037 |
| `SerializationBenchmarks` | `IStore.Read`/`Write` in Turtle, N-Triples, JSON-LD | `INSERT DATA` / a bindings fetch | ADR-0034 |

The `Layered*` classes each have a `…LargeBenchmarks` subclass in the `Large` category at 1M.
That is 1M **triples** (200,000 resources), and the baseline is never turned into objects. Every timed
operation touches a few resources, or runs inside the store. The tier asks whether a small change costs
more because the data under it is large. It is not a "load everything" test. Its realistic targets are
the server backends: the in-memory store holds all 1M triples in the process (6.8 GB observed), so an
in-memory 1M cell stress-tests dotNetRDF rather than modelling real use.

## How to read the tables

The `Ratio` column is the cost of the mapping, not of the store. Comparing backends down a column is
the obvious use; comparing the rows *within* one backend is the more useful one.

`InMemory` is not a peer of the others and is included on purpose: no network, no server, so its row
is the floor. What it costs is Trinity plus dotNetRDF's in-memory engine — and that engine has costs
of its own worth knowing about: a query naming more than one `FROM` rebuilds the merged default graph
every time, so an in-memory `ModelGroup` is O(data) per query.

`MemoryDiagnoser` is on because allocation is the half of the cost that is unambiguously ours — the
store's work happens in another process.

`OperationsPerInvoke` rows (the point reads, updates, deletes, staging) report time **per operation**;
each invocation does a batch of them over distinct subjects, since one sub-millisecond call is below
what the Monitoring strategy resolves.

## Comparing before and after a change

Absolute numbers do not travel, so a comparison is two runs on the same machine, back to back:

```bash
git switch develop
TRINITY_BENCH_BACKENDS=InMemory,Oxigraph dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --filter "*PointRead*" --artifacts ../bench/before
git switch my-optimization
TRINITY_BENCH_BACKENDS=InMemory,Oxigraph dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --filter "*PointRead*" --artifacts ../bench/after
```

Each run writes `results/*-report-full.json` (and GitHub Markdown beside it). The
[ResultsComparer](https://github.com/dotnet/performance/tree/main/src/tools/ResultsComparer) from
`dotnet/performance` reads the full JSON and applies a statistical test:

```bash
dotnet run -c Release --project <dotnet/performance>/src/tools/ResultsComparer -- \
  --base ../bench/before/results --diff ../bench/after/results --threshold 5%
```

Results are not committed: they belong to the machine that produced them.

## Profiling

`profile` runs one benchmark method in a plain loop — its own `[GlobalSetup]`, `[IterationSetup]`,
`[IterationCleanup]` and guards, but no BenchmarkDotNet — so a profiler sees the workload rather than
the harness:

```bash
dotnet build -c Release benchmarks/Trinity.Benchmarks
dotnet-trace collect -- dotnet benchmarks/Trinity.Benchmarks/bin/Release/net8.0/Semiodesk.Trinity.Benchmarks.dll \
  profile PointReadBenchmarks.GetResourceTyped --backend InMemory --iterations 200 --param Size=100000
```

`--backend` defaults to the first selected backend (InMemory when `TRINITY_BENCH_BACKENDS` is unset),
and a parameter that is not given takes its first declared value. `--param` accepts sizes the table
does not have, within what a fixture can build: layered sizes must be a multiple of 5 and at least
1000, and a size that doesn't fit is refused before anything starts. Names are case-insensitive. The same command works under `dotnet-counters`, or with the dll as the start target of
a Visual Studio or Rider profiling session. BenchmarkDotNet's own `EventPipeProfiler` does not help
here: it needs an out-of-process toolchain, and this harness is in-process so the containers survive.

## Things that will mislead you if you forget them

- **Absolute numbers do not travel.** Container throughput swings with host, disk and Docker
  runtime. Compare within a run, never across machines, and never gate CI on a threshold.
- **GraphDB's test container runs with reasoning on** (`rdfsplus-optimized`, set in
  `GraphDBContainer.cs`). Its writes do inference work the others do not. Not apples to apples;
  say so beside any GraphDB number.
- **A write benchmark can measure nothing at all.** Virtuoso refuses any single statement over
  10,000 entries, and its adapter swallows the error (#50, #70), so an update past the limit reports
  success having written nothing. A benchmark that crossed it would report an excellent time for doing
  nothing. Every write benchmark here verifies what landed, outside the timed region and by content
  where a count could come out right by accident. Every read benchmark verifies its fixture and checks
  its own answer. Keep that in anything new.
- **Some cells fail because Trinity has a defect there, and are left failing so it stays visible.**
  ADR-0049 records each one. Fix the defect, not the workload.
  - `WideResourceBenchmarks` on Oxigraph: a mapped `long` above `int.MaxValue` overflows on read (#54).
  - `SerializationBenchmarks`' N-Triples and JSON-LD reads on Virtuoso: its string read path has no
    N-Triples parser and treats a quad-format document as a file name (#55).
  - Every materialized layered cell on Virtuoso: a view past 10,000 effective triples cannot be
    materialized there (#70).
  - On Virtuoso, the timed rows whose single statement crosses 10,000 entries (#70):
    - `SerializationBenchmarks`' four Read rows at `People=10000`: Turtle throws, and the raw
      `INSERT DATA` writes 0.
    - `WideResourceBenchmarks`' two Write rows at `Count=1000`: the mapped `UpdateResources` and the
      raw `INSERT DATA` both write 0.

    Their fixtures are seeded in chunks, so the other rows of both classes still produce numbers.
- **Layered baselines persist between cases.** BenchmarkDotNet runs `[GlobalSetup]` once per case,
  and reseeding a million triples each time would dominate the run, so a layered baseline graph is
  named after its size and reseeded only when its count is wrong. Anything that changes a baseline
  (`Accept`) clears it afterwards.
- **In-process by necessity.** BenchmarkDotNet's default toolchain runs a separate process per
  benchmark case, which would restart every container for every cell of the table. The job in
  `Program.Config()` keeps one set of servers for the run.

## Adding a workload

Derive from `StoreBenchmarkBase` (it supplies the `Backend` parameter, the store, a clean model,
`PersonUri`, `CountWhere`, `AssertWrote`, `AssertSeeded` and `Expect`). Seed fixtures with
`BenchmarkData` rather than through the mapper, unless seeding is what you measure. Pair the mapped
operation with a raw-SPARQL equivalent marked `[Benchmark(Baseline = true)]`, and give each operation
its own `[BenchmarkCategory]` if the class has several. Then prove the work happened:

- A **write** verifies what landed, outside the timed region, by **content** wherever a count can
  come out right by accident.
- A **read** returns its answer through `Expect(actual, expected, …)`, so a read that silently
  returns less fails instead of reporting a speedup.
- Before trusting a new guard, break the path it guards once, in a throwaway worktree, and watch it
  fail.

Don't make a parameter of something the baseline doesn't depend on. BenchmarkDotNet would run the
baseline once per value and report the same cell several times; use one method per variant instead,
as `SerializationBenchmarks` and `LayeredReadBenchmarks` do. Keep `--smoke` cheap: the smallest value
of each numeric parameter is what CI runs.
