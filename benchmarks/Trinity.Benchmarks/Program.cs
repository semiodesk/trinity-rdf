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

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Entry point. Run with <c>dotnet run -c Release --project benchmarks/Trinity.Benchmarks</c>;
    /// add <c>--filter</c> to narrow, e.g. <c>--filter "*Write*"</c>, or
    /// <c>--anyCategories</c>/<c>--list</c> as usual for BenchmarkDotNet.
    /// </summary>
    /// <remarks>
    /// Every backend except the in-memory one is provisioned in Docker, so a full run pulls four
    /// images and starts four servers. To compare a subset, filter on the parameter:
    /// <c>--filter "*" --allStats</c> then read the Backend column, or narrow with
    /// <c>-p Backend=Oxigraph</c>.
    /// </remarks>
    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, Config());
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
        /// <b>Monitoring, one invocation, five iterations</b> because these operations are network-
        /// and disk-bound and take milliseconds to seconds. BenchmarkDotNet's default pilot looks
        /// for a stable nanosecond figure that does not exist here, and finds it by running a
        /// multi-second operation a hundred times.
        ///
        /// <b>MemoryDiagnoser</b> because allocation is the half of the cost that is unambiguously
        /// ours; the store's work happens in another process.
        ///
        /// Ten iterations rather than five: at five, the noisiest cells came back with a confidence
        /// interval wider than the mean, which is not a measurement.
        /// </remarks>
        private static IConfig Config()
        {
            var job = Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                .WithStrategy(RunStrategy.Monitoring)
                .WithWarmupCount(1)
                .WithIterationCount(10)
                .WithInvocationCount(1)
                .WithUnrollFactor(1);

            return DefaultConfig.Instance
                .AddJob(job)
                .AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
}
