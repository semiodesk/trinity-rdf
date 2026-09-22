# Benchmarks

Cross-store performance measurement for Trinity, **through the lens of the library** — not a
SPARQL-engine shootout.

```bash
dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --filter "*"
dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --filter "*WriteBenchmarks*"
dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- --list flat
```

Every backend except the in-memory one is provisioned in Docker by the **same Testcontainers
fixtures the store test suites use** — their classes are public and their `StartAsync` works outside
NUnit, so provisioning is not copied here. A full run therefore needs a Docker daemon and pulls four
images.

## Why it exists

ADR-0042 was written from throwaway measurement harnesses, and says so:

> a claim that is only measured can rot silently

and, listing what is measured but not tested:

> **The performance figures** (31.5 s rebuild, 0.7 ms stage, 12.5 s → 3 ms on the delete path).
> Timings do not belong in a correctness suite.

Both halves of that hold. Timings stay out of the test suites — this is a separate project, outside
`tests/`, not run by CI — but the figures an ADR rests on should be reproducible on demand rather
than lost with the harness that produced them.

## How to read the tables

Each workload pairs the mapped path against **the hand-written SPARQL it stands in for**, and the
raw one is BenchmarkDotNet's `Baseline`. So the `Ratio` column is the cost of the mapping, not of
the store. Comparing backends down a column is the obvious use; comparing the two rows *within* one
backend is the more useful one.

`InMemory` is not a peer of the others and is included on purpose: no network, no server, so its row
is the floor. What it costs is Trinity.

`MemoryDiagnoser` is on because allocation is the half of the cost that is unambiguously ours — the
store's work happens in another process.

## Things that will mislead you if you forget them

- **Absolute numbers do not travel.** Container throughput swings with host, disk and Docker
  runtime. Compare within a run, never across machines, and never gate CI on a threshold.
- **GraphDB's test container runs with reasoning on** (`rdfsplus-optimized`, set in
  `GraphDBContainer.cs`). Its writes do inference work the others do not. Not apples to apples;
  say so beside any GraphDB number.
- **A write benchmark can measure nothing at all.** ADR-0042 records that Virtuoso *silently writes
  zero* when an `INSERT … WHERE` exceeds its transaction-log limit — fine at 500k, zero at 1M. A
  benchmark that crosses that would report an excellent time for doing nothing. Every write
  benchmark here verifies the triple count after the timed region, and throws if the work did not
  land. Keep that in anything new.
- **In-process by necessity.** BenchmarkDotNet's default toolchain runs a separate process per
  benchmark case, which would restart every container for every cell of the table. `[InProcess]`
  keeps one set of servers for the run.

## Adding a workload

Derive from `StoreBenchmarkBase` (it supplies the `Backend` parameter, the store, a clean model, and
`AssertWrote`). Pair the mapped operation with a raw-SPARQL equivalent marked
`[Benchmark(Baseline = true)]`. Verify the work happened outside the timed region.
