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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace Semiodesk.Trinity.Benchmarks
{
    /// <summary>
    /// Runs one benchmark method in a plain loop, without BenchmarkDotNet, for a profiler to attach to.
    /// </summary>
    /// <remarks>
    /// <code>
    /// dotnet run -c Release --project benchmarks/Trinity.Benchmarks -- profile ReadBenchmarks.GetResourcesMapped --backend InMemory --iterations 200 --param Count=1000
    /// dotnet-trace collect -- dotnet benchmarks/Trinity.Benchmarks/bin/Release/net8.0/Semiodesk.Trinity.Benchmarks.dll profile ...
    /// </code>
    ///
    /// Exists because BenchmarkDotNet's own profilers do not fit this harness: <c>EventPipeProfiler</c>
    /// needs an out-of-process toolchain, and the in-process one is what keeps the containers alive
    /// across cases (see <see cref="Program"/>). A trace of a whole BenchmarkDotNet run would also be
    /// mostly BenchmarkDotNet. Here the process does one thing, N times, so its hot path is the
    /// workload's.
    ///
    /// The lifecycle is the benchmark's own, found by its attributes: <c>[GlobalSetup]</c> once, then
    /// <c>[IterationSetup]</c>, the method and <c>[IterationCleanup]</c> per iteration, then
    /// <c>[GlobalCleanup]</c>. So the guards (<c>AssertWrote</c> and friends) run exactly as they do
    /// under BenchmarkDotNet. A parameter that is not given takes the first value it would take there.
    ///
    /// The timings it prints are a sanity check, not a measurement: there is no warmup, no statistics,
    /// and the setup and cleanup around each iteration are excluded only approximately.
    /// </remarks>
    public static class ProfileRunner
    {
        private const string Usage =
            "usage: profile <Type.Method> [--backend <name>] [--iterations <n>] [--param <Name>=<value>]...";

        public static int Run(string[] args)
        {
            if (args.Length == 0 || args[0].StartsWith("-", StringComparison.Ordinal))
            {
                Console.Error.WriteLine(Usage);
                return 2;
            }

            var target = args[0];
            var iterations = 100;
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 1; i < args.Length; i++)
            {
                var value = i + 1 < args.Length ? args[i + 1] : null;

                switch (args[i])
                {
                    case "--backend":
                        parameters[nameof(StoreBenchmarkBase.Backend)] = value;
                        i++;
                        break;

                    case "--iterations":
                        iterations = int.Parse(value ?? "", CultureInfo.InvariantCulture);
                        i++;
                        break;

                    case "--param":
                        var pair = (value ?? "").Split(new[] { '=' }, 2);

                        if (pair.Length != 2)
                        {
                            Console.Error.WriteLine($"--param expects Name=Value, got '{value}'.\n{Usage}");
                            return 2;
                        }

                        parameters[pair[0]] = pair[1];
                        i++;
                        break;

                    default:
                        Console.Error.WriteLine($"Unknown argument '{args[i]}'.\n{Usage}");
                        return 2;
                }
            }

            var dot = target.LastIndexOf('.');

            if (dot <= 0)
            {
                Console.Error.WriteLine($"Expected Type.Method, got '{target}'.\n{Usage}");
                return 2;
            }

            var type = typeof(ProfileRunner).Assembly.GetTypes()
                .SingleOrDefault(t => !t.IsAbstract && t.Name == target.Substring(0, dot));

            var method = type?.GetMethod(target.Substring(dot + 1), BindingFlags.Public | BindingFlags.Instance);

            if (method == null || method.GetCustomAttribute<BenchmarkAttribute>() == null)
            {
                Console.Error.WriteLine($"No benchmark method '{target}'. List them with: -- --list flat");
                return 2;
            }

            var instance = Activator.CreateInstance(type);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = ParameterValue(instance, property, parameters);

                if (value != null)
                {
                    property.SetValue(instance, value);
                    Console.WriteLine($"{property.Name} = {value}");
                }
            }

            var unused = parameters.Keys.Where(k => type.GetProperty(k) == null).ToList();

            if (unused.Count > 0)
            {
                Console.Error.WriteLine($"{type.Name} has no parameter {string.Join(", ", unused)}.");
                return 2;
            }

            var globalSetup = Lifecycle<GlobalSetupAttribute>(type, method.Name);
            var iterationSetup = Lifecycle<IterationSetupAttribute>(type, method.Name);
            var iterationCleanup = Lifecycle<IterationCleanupAttribute>(type, method.Name);
            var globalCleanup = Lifecycle<GlobalCleanupAttribute>(type, method.Name);

            Invoke(globalSetup, instance);

            var timings = new List<double>(iterations);

            try
            {
                for (var i = 0; i < iterations; i++)
                {
                    Invoke(iterationSetup, instance);

                    var watch = Stopwatch.StartNew();
                    Invoke(method, instance);
                    watch.Stop();

                    timings.Add(watch.Elapsed.TotalMilliseconds);

                    Invoke(iterationCleanup, instance);
                }
            }
            finally
            {
                Invoke(globalCleanup, instance);
            }

            timings.Sort();

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: {1} iterations, min {2:F3} ms, median {3:F3} ms, max {4:F3} ms",
                target, iterations, timings[0], timings[timings.Count / 2], timings[timings.Count - 1]));

            return 0;
        }

        /// <summary>
        /// The value a parameter property takes: the one given, else the first BenchmarkDotNet would use.
        /// </summary>
        private static object ParameterValue(object instance, PropertyInfo property, IDictionary<string, string> given)
        {
            var values = property.GetCustomAttribute<ParamsAttribute>()?.Values;
            var source = property.GetCustomAttribute<ParamsSourceAttribute>();

            if (source != null)
            {
                var member = (object)instance.GetType().GetProperty(source.Name)
                    ?? instance.GetType().GetMethod(source.Name);

                var items = member is PropertyInfo p ? p.GetValue(instance) : ((MethodInfo)member).Invoke(instance, null);

                values = ((IEnumerable)items).Cast<object>().ToArray();
            }

            if (values == null)
            {
                return null;
            }

            if (given.TryGetValue(property.Name, out var text))
            {
                var type = property.PropertyType;

                // Parsed against the property's type rather than looked up among the declared values,
                // so a profile can run at a size the table does not have -- the usual reason to profile.
                return type.IsEnum
                    ? Enum.Parse(type, text, ignoreCase: true)
                    : Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            }

            // An explicit --backend bypasses TRINITY_BENCH_BACKENDS; without one, the first selected
            // backend is used, which for an unset variable is InMemory -- the one that needs no Docker.
            return values.FirstOrDefault();
        }

        /// <summary>
        /// The lifecycle method BenchmarkDotNet would run around <paramref name="benchmark"/>.
        /// </summary>
        /// <remarks>
        /// One that names its benchmark with <c>Target</c> wins over an untargeted one, as it does in
        /// BenchmarkDotNet; a method targeted at a different benchmark is not a candidate at all.
        /// <c>GetMethods</c> returns an override rather than the base declaration, so a virtual setup is
        /// invoked once, through virtual dispatch.
        /// </remarks>
        private static MethodInfo Lifecycle<T>(Type type, string benchmark) where T : TargetedAttribute
        {
            var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => (method: m, attribute: m.GetCustomAttribute<T>(inherit: true)))
                .Where(c => c.attribute != null)
                .ToList();

            return candidates.FirstOrDefault(c => c.attribute.Targets?.Contains(benchmark) == true).method
                ?? candidates.FirstOrDefault(c => c.attribute.Targets == null || c.attribute.Targets.Length == 0).method;
        }

        private static void Invoke(MethodInfo method, object instance)
        {
            if (method == null)
            {
                return;
            }

            try
            {
                method.Invoke(instance, null);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }
    }
}
