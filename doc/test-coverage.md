# Test coverage

How to measure it, and what the gaps are. Numbers below are a **union across four
suites** measured on `develop` at the commit that added this file.

## Running it

```bash
rm -rf .coverage
for s in Fuseki GraphDB Virtuoso; do
  dotnet test tests/Trinity.Tests.$s/Trinity.Tests.$s.csproj -c Release \
    --collect:"XPlat Code Coverage" --results-directory .coverage/$s
done
dotnet test Trinity.Tests/Trinity.Tests.csproj -c Release \
  --collect:"XPlat Code Coverage" --results-directory .coverage/InMemory

python3 scripts/coverage-summary.py .coverage              # per assembly
python3 scripts/coverage-summary.py .coverage --classes    # per class
python3 scripts/coverage-summary.py .coverage --uncovered StoreBase
```

The store suites need Docker (ADR-0036). `coverlet.collector` is referenced by the
test projects; nothing else depends on it.

## Why the union, and not one run

No single suite exercises everything. The store suites drive the *same* shared
fixtures through different backends, and the backends override different members —
so a per-run report understates every shared class, sometimes badly. `StoreBase` is
21% under Virtuoso alone, because Virtuoso overrides most of what it inherits, and
48–52% under each of the others. Only the union says anything useful.

`scripts/coverage-summary.py` merges on `(assembly, class, method, line)` and
deliberately **excludes the filename from that key**: runs report the same file both
relatively and absolutely depending on invocation, and keying on it counts every
line twice, halving the reported figure.

## Baseline

| assembly | lines | |
|---|---:|---:|
| `Semiodesk.Trinity` | 4051/4938 | 82% |
| `Semiodesk.Trinity.Fuseki` | 123/164 | 75% |
| `Semiodesk.Trinity.GraphDB` | 169/237 | 71% |
| `Semiodesk.Trinity.Virtuoso` | 571/1153 | 49% |

## The gaps worth knowing

**Line coverage says a line ran, not that anything asserted on it.** Read these as
"nothing has ever executed this", which is a floor on the problem, not a measure of
it.

- **`StoreBase.UpdateResources`, the wholesale branch — 0 hits on every backend**
  (`StoreBase.cs:424-427,454,456`). `UpdateResources` has exactly one test in the
  whole repository, and it passes resources that were committed and reloaded, so it
  takes the *delta* branch. The branch new resources take has never run. This is not
  hypothetical: that branch silently writes nothing on Fuseki, because
  `WITH <g> … WHERE …` is a no-op there when `<g>` does not yet exist — which is why
  `FusekiStore` already overrides `UpdateResource` (singular) to use
  `INSERT DATA { GRAPH … }`. Coverage found the hole independently of the bug.
- **`SparqlEndpointStore` — 0/54, on all four runs.** Nothing anywhere constructs
  one. Its two tests are `Assert.Inconclusive("Endpoint doesn't seem to exist
  anymore.")`. Anything changed here is unverifiable by the suite.
- **`ModelGroup` — 74/160.** `ModelGroupTest<T>` is a handful of tests against a
  class with ~50 members; most of the `ISet` surface (`ExceptWith`, `IntersectWith`,
  `UnionWith`, `SetEquals`, `Overlaps`, …) has never run.
- **`VirtuosoManager` — 125/521.** Vendored provider code; large, and mostly reached
  only by paths the suite does not take.
- Assorted exception types (`InvalidQueryException`, `ResourceLockedException`,
  `StoreProviderMissingException`, …) are never constructed, so nothing pins the
  messages or the conditions that should raise them.

## A caution about the store suites

Every write test inherits a graph that an earlier test created. That makes the
"graph does not exist yet" precondition nearly unreachable — and it is exactly the
precondition under which `UpdateResources` fails on Fuseki. **A regression test for
a write path should clear the model first**, or it is testing the easy case.
