using Loudenvier.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Loudenvier.FastFileEnumerator.Benchmarking;

internal sealed class BenchmarkSample
{
    public Workload Workload;
    public string Method = "";
    public int Iteration;
    public double Milliseconds;
    public long AllocatedBytes;
    public int Gc0;
    public int Gc1;
    public int Gc2;
    public long Files;
    public long Directories;
    public long Bytes;
    public ulong Hash;
    public int RecordCount;
    public int ParallelWorkers;
    public string AllocationScope = "CurrentThread";
}

internal static class BenchmarkRunner
{
    private static long _sink;
    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;
    private static readonly string[] CsvHeader = [
        "Workload", "Backend", "BufferBytes", "Iteration", "ElapsedMs", "ManagedBytes", "GC0", "GC1", "GC2",
        "Files", "Directories", "LogicalBytes", "HashHex", "MaterializedRecords", "Runtime", "Root",
        "Workers", "AllocationScope"
    ];

    internal static void Execute(BenchmarkOptions options)
    {
        List<BackendVariant> variants = options.GetVariants();
        Console.WriteLine("Runtime: " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription +
            "; OS: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        Console.WriteLine("Root: " + options.Root);
        Console.WriteLine("Variants: " + string.Join(", ", variants.Select(v => v.Name)));
        Console.WriteLine("Workloads: " + string.Join(", ", options.Workloads.Select(w => w.ToString())));
        Console.WriteLine("Samples: " + options.Samples + "; warmup: " + options.Warmup +
            "; initially selected methods: " + variants.Count);
        Console.WriteLine("Hash: built-in benchmark FNV-1a 64-bit over relative path + size + attributes + kind (not XXH3).");
        Console.WriteLine("Managed allocations: current thread for sequential/P1; " +
            "multi-thread scan is process-wide on .NET 10 and N/A on Framework.");
        Console.WriteLine("GC counters: process-wide; parallel workers use independent accumulators.");
        Console.WriteLine("All backends traverse directories but never follow directory reparse points.");

        Console.WriteLine();
#if NETFRAMEWORK
        Console.WriteLine("Building independent Win32 FindFirstFileW reference (extended paths; outside measurement)...");
#else
        Console.WriteLine("Building independent DirectoryInfo reference (outside measurement)...");
#endif
        ReferenceSnapshot baseline = ReferenceScanner.Build(options.Root);
#if NETFRAMEWORK
        // The production Portable backend intentionally uses DirectoryInfo.
        // On legacy Framework, DirectoryInfo may not enumerate directories
        // whose search pattern crosses MAX_PATH. Skip this incapable backend
        // rather than silently omitting files or weakening validation.
        if (baseline.RequiresExtendedWindowsPaths)
        {
            int removed = variants.RemoveAll(v =>
                v.Options.Backend == FastEnumerationBackend.Portable);
            if (removed != 0)
                Console.WriteLine("Skipping Portable on net472: this tree requires extended-length Windows paths; " +
                    "the production Portable backend uses legacy DirectoryInfo and cannot reliably traverse it. " +
                    "The NT and Auto backends remain fully validated.");
            if (variants.Count == 0)
                throw new NotSupportedException("All selected backends require legacy DirectoryInfo, " +
                    "which cannot reliably enumerate this long-path tree on .NET Framework. " +
                    "Select --backends auto,nt or use a shallower directory tree for Portable.");
        }
#endif
        Console.WriteLine("Reference: " + baseline.Directories + " directories; " + baseline.Files +
            " files; " + baseline.Bytes + " logical bytes; " + baseline.Entries.Count + " entries.");
        if (options.DiagnosticsFocusedOnly)
        {
            Console.WriteLine("Running FOCUSED JIT / string allocation diagnostics without normal benchmark.");
            FocusedDiagnosticRunner.Execute(options, baseline);
            return;
        }
        if (options.DiagnosticsOnly)
        {
            Console.WriteLine("Running managed/JIT/allocation diagnostics without the normal benchmark.");
            DiagnosticRunner.Execute(options, baseline);
            return;
        }
        Console.WriteLine("Active variants (" + variants.Count + " methods per round): " +
            string.Join(", ", variants.Select(v => v.Name)));
        Console.WriteLine("Validating all methods: full paths, attributes, sizes, totals and hash...");
        foreach (BackendVariant variant in variants)
        {
            ReferenceScanner.Validate(options.Root, variant, baseline);
            Console.Write(".");
        }
        Console.WriteLine(" All " + variants.Count + " methods validated.");

        var allSamples = new List<BenchmarkSample>(options.Samples * variants.Count * options.Workloads.Length);
        foreach (Workload workload in options.Workloads)
        {
            if (options.ForceGcBeforeWorkload)
            {
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            }
            Console.WriteLine();
            Console.WriteLine("Scenario: " + workload + " (" + variants.Count + " methods)");
            Console.WriteLine("Warmup...");
            for (int w = 0; w < options.Warmup; ++w)
                foreach (BackendVariant variant in Rotated(variants, w))
                    RunOne(options.Root, variant, workload, baseline, -1);

            // Each complete group of N rounds puts each backend in each position.
            // Every second group reverses direction to reduce positional bias.
            var byBackend = variants.ToDictionary(v => v.Name,
                v => new List<BenchmarkSample>(options.Samples), StringComparer.Ordinal);
            for (int i = 0; i < options.Samples; ++i)
            {
                foreach (BackendVariant variant in Rotated(variants, i))
                {
                    BenchmarkSample sample = RunOne(options.Root, variant, workload, baseline, i + 1);
                    byBackend[variant.Name].Add(sample);
                    allSamples.Add(sample);
                }
                if ((i + 1) % 10 == 0 || i + 1 == options.Samples)
                    Console.Write("\rSamples: " + (i + 1) + "/" + options.Samples + "    ");
            }
            Console.WriteLine();
            PrintSummary(variants, byBackend);
        }
        WriteCsv(options.CsvPath, options.Root, variants, allSamples);
        Console.WriteLine("CSV: " + Path.GetFullPath(options.CsvPath));
        if (options.Diagnostics)
            DiagnosticRunner.Execute(options, baseline);
        Console.WriteLine("Done. Data blackhole: " + Interlocked.Read(ref _sink).ToString("X16", CI));
    }

    private static IEnumerable<BackendVariant> Rotated(IReadOnlyList<BackendVariant> variants, int iteration)
    {
        int n = variants.Count;
        int offset = iteration % n;
        bool backwards = (iteration / n & 1) != 0;
        for (int pos = 0; pos < n; ++pos)
        {
            int index = backwards ? (offset - pos + n) % n : (offset + pos) % n;
            yield return variants[index];
        }
    }

    private static BenchmarkSample RunOne(string root, BackendVariant variant, Workload workload,
        ReferenceSnapshot expected, int iteration)
    {
        // Per-worker callback factories eliminate hot-path contention; only the
        // post-scan reduction requires visiting each worker's accumulator.
        bool parallel = variant.ParallelWorkers > 1;
        string allocationScope;
        long allocationStart;
#if NET6_0_OR_GREATER
        allocationScope = parallel ? "Process" : "CurrentThread";
        allocationStart = parallel ? GC.GetTotalAllocatedBytes(true) : AllocationCounter.Read();
#else
        allocationScope = parallel ? "N/A" : "CurrentThread";
        allocationStart = parallel ? -1 : AllocationCounter.Read();
#endif
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
        long start = Stopwatch.GetTimestamp();
        ScanAccumulator[] accumulators;
        if (variant.ParallelWorkers == 0)
        {
            var scanner = new ScanAccumulator(root, workload);
            IO.FastFileEnumerator.Enumerate(root, scanner.Callback, variant.Options);
            accumulators = [scanner];
        }
        else
        {
            int count = variant.ParallelWorkers;
            accumulators = new ScanAccumulator[count];
            IO.FastFileEnumerator.EnumerateParallel(root, index =>
            {
                var scanner = new ScanAccumulator(root, workload);
                accumulators[index] = scanner;
                return scanner.Callback;
            }, variant.Options, count);
        }
        long stop = Stopwatch.GetTimestamp();
#if NET6_0_OR_GREATER
        long allocationEnd = parallel ? GC.GetTotalAllocatedBytes(true) : AllocationCounter.Read();
#else
        long allocationEnd = parallel ? -1 : AllocationCounter.Read();
#endif
        int afterGc0 = GC.CollectionCount(0), afterGc1 = GC.CollectionCount(1), afterGc2 = GC.CollectionCount(2);

        // Reduce only AFTER the stopwatch stops. No per-file atomics, locks,
        // shared List<T>, or shared hash accumulator are used by the benchmark.
        long files = 0, directories = 0, bytes = 0;
        ulong hash = 0;
        int recordCount = 0;
        foreach (ScanAccumulator scanner in accumulators)
        {
            files += scanner.Files;
            directories += scanner.Directories;
            bytes = unchecked(bytes + scanner.Bytes);
            hash = unchecked(hash + scanner.Hash);
            recordCount += scanner.Records?.Count ?? 0;
            GC.KeepAlive(scanner);
        }
        if (files != expected.Files || directories != expected.Directories || bytes != expected.Bytes)
            throw new InvalidOperationException(variant.Name + " totals changed during benchmark; filesystem may have changed.");
        if (workload == Workload.HashFull && hash != expected.Hash)
            throw new InvalidOperationException(variant.Name + " hash changed during benchmark; filesystem may have changed.");
        if (workload == Workload.Materialize && recordCount != expected.Entries.Count)
            throw new InvalidOperationException(variant.Name + " materialization count changed.");

        Interlocked.Exchange(ref _sink, unchecked((long)(hash ^ (ulong)bytes ^
            (ulong)files ^ ((ulong)directories << 32) ^ (ulong)recordCount)));
        GC.KeepAlive(accumulators);
        return new BenchmarkSample
        {
            Workload = workload, Method = variant.Name, Iteration = iteration,
            ParallelWorkers = variant.ParallelWorkers, AllocationScope = allocationScope,
            Milliseconds = (stop - start) * 1000.0 / Stopwatch.Frequency,
            AllocatedBytes = allocationStart < 0 || allocationEnd < 0 ? -1 : allocationEnd - allocationStart,
            Gc0 = afterGc0 - gc0, Gc1 = afterGc1 - gc1, Gc2 = afterGc2 - gc2,
            Files = files, Directories = directories, Bytes = bytes,
            Hash = hash, RecordCount = recordCount
        };
    }

    private static void PrintSummary(List<BackendVariant> methods, Dictionary<string, List<BenchmarkSample>> byMethod)
    {
        Console.WriteLine(" {0,-23} {1,10} {2,10} {3,10} {4,10} {5,14} {6,12}",
            "Method", "Mean ms", "Median ms", "P95 ms", "Min ms", "Managed B", "GC 0/1/2");
        foreach (BackendVariant variant in methods)
        {
            List<BenchmarkSample> samples = byMethod[variant.Name];
            double[] times = [.. samples.Select(s => s.Milliseconds).OrderBy(x => x)];
            long[] allocations = [.. samples.Where(s => s.AllocatedBytes >= 0).Select(s => s.AllocatedBytes).OrderBy(x => x)];
            double mean = times.Average();
            double median = Median(times);
            double p95 = times[(int)Math.Ceiling(times.Length * 0.95) - 1];
            string gc = samples.Sum(s => s.Gc0) + "/" + samples.Sum(s => s.Gc1) + "/" + samples.Sum(s => s.Gc2);
            Console.WriteLine(" {0,-23} {1,10:F4} {2,10:F4} {3,10:F4} {4,10:F4} {5,14} {6,12}",
                variant.Name, mean, median, p95, times[0],
                allocations.Length == 0 ? "N/A" : Median(allocations).ToString("N0", CI), gc);
        }
    }

    private static double Median(double[] values) => values.Length % 2 != 0
        ? values[values.Length / 2] : (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2;
    private static double Median(long[] values) => values.Length % 2 != 0
        ? values[values.Length / 2] : (values[values.Length / 2 - 1] + (double)values[values.Length / 2]) / 2;

    private static void WriteCsv(string filename, string root, List<BackendVariant> variants, List<BenchmarkSample> samples)
    {
        string? parent = Path.GetDirectoryName(Path.GetFullPath(filename));
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        var buffer = variants.ToDictionary(v => v.Name, v => v.Options.BufferSize, StringComparer.Ordinal);
        using var csv = new StreamWriter(filename, false, new UTF8Encoding(false));
        csv.WriteLine(string.Join(",", CsvHeader));
        foreach (BenchmarkSample s in samples) {
            string[] fields = [
                    s.Workload.ToString(), s.Method, buffer[s.Method].ToString(CI), s.Iteration.ToString(CI),
                    s.Milliseconds.ToString("F6", CI), s.AllocatedBytes >= 0 ? s.AllocatedBytes.ToString(CI) : "",
                    s.Gc0.ToString(CI), s.Gc1.ToString(CI), s.Gc2.ToString(CI),
                    s.Files.ToString(CI), s.Directories.ToString(CI), s.Bytes.ToString(CI),
                    s.Hash.ToString("X16", CI), s.RecordCount.ToString(CI),
                    System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, root,
                    s.ParallelWorkers.ToString(CI), s.AllocationScope
                ];
            csv.WriteLine(string.Join(",", fields.Select(EscapeCsv)));
        }
    }

    private static string EscapeCsv(string input)
    {
        if (input.IndexOfAny([',', '"', '\r', '\n']) < 0) return input;
        return "\"" + input.Replace("\"", "\"\"") + "\"";
    }
}
