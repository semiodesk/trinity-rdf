// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2026

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Entry point. Run with <c>dotnet run -c Release --project benchmarks/Trinity.Benchmarks</c>;
    /// add <c>--filter</c> to narrow, e.g. <c>--filter "*Write*"</c>, or <c>--list flat</c> as usual
    /// for BenchmarkDotNet.
    /// </summary>
    /// <remarks>
    /// Every backend except the in-memory one is provisioned in Docker. To run a subset, set
    /// <c>TRINITY_BENCH_BACKENDS</c> (see <see cref="BenchmarkBackends"/>); BenchmarkDotNet itself has
    /// no parameter filter, and its <c>-p</c> is the profiler switch.
    ///
    /// Three switches are this harness's own and are removed before BenchmarkDotNet sees the rest:
    /// <list type="bullet">
    /// <item><c>--large</c> includes the <c>Large</c> category (the million-triple tier), which is
    /// otherwise filtered out.</item>
    /// <item><c>--smoke</c> runs every case once with no warmup, and defaults the backends to
    /// <see cref="StoreBackend.InMemory"/>. It proves the workloads run and their guards hold; its
    /// timings mean nothing. This is what CI runs.</item>
    /// <item><c>profile</c>, as the first argument, bypasses BenchmarkDotNet entirely -- see
    /// <see cref="ProfileRunner"/>.</item>
    /// </list>
    /// </remarks>
    public static class Program
    {
        /// <summary>
        /// The category that marks a benchmark as part of the opt-in million-triple tier.
        /// </summary>
        public const string LargeCategory = "Large";

        /// <summary>
        /// How long one benchmark case may run, setup included, before BenchmarkDotNet gives up on it.
        /// </summary>
        /// <remarks>
        /// The in-process toolchain's default is five minutes, and exceeding it does not fail the case:
        /// it throws out of <c>BenchmarkSwitcher.Run</c> and ends the whole run. A 1M layered case --
        /// seeding, then materializing, then ten iterations of a multi-second rebuild -- is past five
        /// minutes in memory before its first iteration.
        /// </remarks>
        private static readonly TimeSpan CaseTimeout = TimeSpan.FromHours(2);

        /// <summary>
        /// The same limit for <c>--smoke</c>, which CI runs.
        /// </summary>
        /// <remarks>
        /// A smoke case is one iteration at the smallest size, and even a container start fits well
        /// inside this. A hung case should fail the fast CI job in minutes, not hold it for two hours.
        /// </remarks>
        private static readonly TimeSpan SmokeCaseTimeout = TimeSpan.FromMinutes(15);

        public static int Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "profile", StringComparison.OrdinalIgnoreCase))
            {
                return ProfileRunner.Run(args.Skip(1).ToArray());
            }

            var rest = new List<string>(args);
            var large = rest.Remove("--large");
            var smoke = rest.Remove("--smoke");

            if (smoke && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BenchmarkBackends.Variable)))
            {
                // In-process, so the ParamsSource reads this. A smoke run exists to be cheap and
                // Docker-free; a caller who wants it against a container says so explicitly.
                Environment.SetEnvironmentVariable(BenchmarkBackends.Variable, nameof(StoreBackend.InMemory));
            }

            var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
                .Run(rest.ToArray(), Config(large, smoke));

            // A guard that throws (AssertWrote, AssertSeeded) fails its case, not the process, so the
            // exit code is the only thing a smoke run in CI can go by.
            return summaries.Any(s => s.HasCriticalValidationErrors || s.Reports.Any(r => !r.Success)) ? 1 : 0;
        }

        /// <summary>
        /// The single job every benchmark runs under.
        /// </summary>
        /// <remarks>
        /// Built here rather than with attributes because <c>[InProcess]</c> and <c>[SimpleJob]</c>
        /// do not compose -- each declares its own job, so the pair gives two, and only one of them
        /// has the settings that make these measurements mean anything.
        ///
        /// <b>In-process</b> because containers are cached per process: the default toolchain runs a
        /// separate process per benchmark case and would restart all four servers for every cell.
        ///
        /// <b>Monitoring, one invocation</b> because these operations are network- and disk-bound and
        /// take milliseconds to seconds. BenchmarkDotNet's default pilot looks for a stable nanosecond
        /// figure that does not exist here, and finds it by running a multi-second operation a hundred
        /// times. Ten iterations rather than five: at five, the noisiest cells came back with a
        /// confidence interval wider than the mean, which is not a measurement.
        ///
        /// <b>MemoryDiagnoser</b> because allocation is the half of the cost that is unambiguously
        /// ours; the store's work happens in another process.
        ///
        /// <b>Grouped by category</b> so a class can pair several operations each with its own
        /// baseline: BenchmarkDotNet allows one baseline per logical group, and a category is one.
        ///
        /// <b>JSON exporter</b> because comparing a run before a change with one after it is what the
        /// harness is for, and the full JSON is what a results comparer reads.
        /// </remarks>
        private static IConfig Config(bool large, bool smoke)
        {
            var job = Job.Default
                .WithToolchain(new InProcessEmitToolchain(smoke ? SmokeCaseTimeout : CaseTimeout, logOutput: true))
                .WithStrategy(RunStrategy.Monitoring)
                .WithWarmupCount(smoke ? 0 : 1)
                .WithIterationCount(smoke ? 1 : 10)
                .WithInvocationCount(1)
                .WithUnrollFactor(1);

            var config = DefaultConfig.Instance
                .AddJob(job)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddExporter(JsonExporter.Full)
                .AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);

            if (!large)
            {
                config = config.AddFilter(new SimpleFilter(b => !b.Descriptor.Categories.Contains(LargeCategory)));
            }

            if (smoke)
            {
                config = config.AddFilter(new SimpleFilter(AtSmallestSizes));
            }

            return config;
        }

        /// <summary>
        /// Keeps a case only if every numeric parameter takes its smallest declared value.
        /// </summary>
        /// <remarks>
        /// A smoke run is there to prove every workload runs and its guards hold, which the smallest
        /// size proves as well as the largest, at a fraction of the cost: an in-memory model group at
        /// 100,000 triples answers each query in about a second. Non-numeric parameters -- the backend,
        /// whether a view is materialized -- keep all their values, because each one is a different
        /// code path rather than a bigger dose of the same one.
        /// </remarks>
        private static bool AtSmallestSizes(BenchmarkCase benchmark)
        {
            return benchmark.Parameters.Items.All(p =>
            {
                if (!IsNumeric(p.Value))
                {
                    return true;
                }

                var smallest = p.Definition.Values.Where(IsNumeric).Min(v => Convert.ToDecimal(v));

                return Convert.ToDecimal(p.Value) == smallest;
            });
        }

        private static bool IsNumeric(object value)
        {
            return value is int || value is long || value is short || value is byte
                || value is double || value is float || value is decimal;
        }
    }
}
