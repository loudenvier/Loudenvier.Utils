using Loudenvier.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Loudenvier.FastFileEnumerator.Benchmarking;

/// <summary>
/// Separate, opt-in investigations. Scheduler instrumentation is never included
/// in the normal benchmark or in the sequential-vs-P1 equivalence test.
/// </summary>
internal static class BenchmarkInvestigations
{
    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    internal static void Execute(BenchmarkOptions options, ReferenceSnapshot reference, List<BackendVariant> variants)
    {
        var bases = variants.Where(v => v.ParallelWorkers == 0).ToList();
#if NETFRAMEWORK
        if (reference.RequiresExtendedWindowsPaths)
            bases.RemoveAll(v => v.Options.Backend == FastEnumerationBackend.Portable);
#endif
        if (bases.Count == 0) throw new ArgumentException("No usable backend selected.");
        Console.WriteLine("Investigation modes use the same independently built reference as normal benchmarks.");
        foreach (BackendVariant variant in bases)
        {
            ReferenceScanner.Validate(options.Root, variant, reference);
            Console.Write(".");
        }
        Console.WriteLine(" Reference validation OK.");
        if (options.TreeProfile) PrintTree(options, reference);
        if (options.P1Check) CompareP1(options, reference, bases);
        if (options.SchedulerProfile) ProfileScheduler(options, reference, bases);
    }

    private static void CompareP1(BenchmarkOptions options, ReferenceSnapshot reference, List<BackendVariant> bases)
    {
        string path = Sibling(options.CsvPath, "p1");
        using var writer = OpenCsv(path, "Backend,Workload,Pair,Position,Mode,ElapsedMs,ManagedBytes,GC0,GC1,GC2");
        Console.WriteLine();
        Console.WriteLine("=== Sequential vs P1: alternating AB/BA pairs; " + options.Samples + " pairs per workload ===");
        foreach (Workload workload in options.Workloads)
        foreach (BackendVariant baseline in bases)
        {
            var p1 = new BackendVariant(baseline.Name + "/P1", baseline.Options.Backend,
                baseline.Options.BufferSize, 1);
            ReferenceScanner.Validate(options.Root, p1, reference);
            for (int warm = 0; warm < options.Warmup; warm++)
            {
                BenchmarkRunner.RunOne(options.Root, baseline, workload, reference, -1);
                BenchmarkRunner.RunOne(options.Root, p1, workload, reference, -1);
            }
            var differences = new List<double>(options.Samples);
            var ratios = new List<double>(options.Samples);
            for (int pair = 0; pair < options.Samples; pair++)
            {
                bool forward = (pair & 1) == 0;
                BackendVariant first = forward ? baseline : p1;
                BackendVariant second = forward ? p1 : baseline;
                BenchmarkSample a = BenchmarkRunner.RunOne(options.Root, first, workload, reference, pair + 1);
                BenchmarkSample b = BenchmarkRunner.RunOne(options.Root, second, workload, reference, pair + 1);
                WriteP1Row(writer, baseline.Name, workload, pair + 1, 1, a);
                WriteP1Row(writer, baseline.Name, workload, pair + 1, 2, b);
                double seq = forward ? a.Milliseconds : b.Milliseconds;
                double par = forward ? b.Milliseconds : a.Milliseconds;
                differences.Add(100.0 * (par / seq - 1.0));
                ratios.Add(par / seq);
            }
            differences.Sort();
            ratios.Sort();
            Console.WriteLine(" {0,-20} {1,-12} P1/seq median={2:F4}x, delta={3:+0.00;-0.00;0.00}% " +
                "(positive means P1 slower), pairs={4}",
                baseline.Name, workload, Median(ratios), Median(differences), options.Samples);
        }
        Console.WriteLine("P1 CSV: " + Path.GetFullPath(path));
    }

    private static void WriteP1Row(StreamWriter writer, string backend, Workload workload,
        int pair, int position, BenchmarkSample sample)
    {
        writer.WriteLine(string.Join(",", new[] {
            Csv(backend), workload.ToString(), pair.ToString(CI), position.ToString(CI),
            sample.ParallelWorkers == 0 ? "Sequential" : "P1",
            sample.Milliseconds.ToString("F6", CI),
            sample.AllocatedBytes >= 0 ? sample.AllocatedBytes.ToString(CI) : "",
            sample.Gc0.ToString(CI), sample.Gc1.ToString(CI), sample.Gc2.ToString(CI)
        }));
    }

    private static void ProfileScheduler(BenchmarkOptions options, ReferenceSnapshot reference,
        List<BackendVariant> bases)
    {
        int[] degrees = options.ParallelDegrees.Length == 0 ? [2, 4, 8, 16] :
            options.ParallelDegrees.Where(n => n >= 2).ToArray();
        if (degrees.Length == 0)
            throw new ArgumentException("--scheduler-profile requires --parallel with at least one degree >= 2.");
        string path = Sibling(options.CsvPath, "workers");
        string summary = Sibling(options.CsvPath, "scheduler");
        using var csv = OpenCsv(path,
            "Backend,Workload,Degree,Sample,Worker,ManagedThreadId,Files,DirectoryEntries,DirectoriesScanned," +
            "ScanMs,WaitMs,StartDelayMs,WorkerLifetimeMs,WorkerAllocatedBytes,ReservedNativeBytes,TotalMs,PeakQueued,PeakActive");
        using var overview = OpenCsv(summary,
            "Backend,Workload,Degree,Sample,ElapsedMs,SumWorkerBytes,ProcessAllocatedBytes,NativeBufferBytes," +
            "TotalDirectoriesScanned,TotalFiles,PeakQueued,PeakActive,DirectoriesMin,DirectoriesMax,FilesMin,FilesMax");
        Console.WriteLine();
        Console.WriteLine("=== Scheduler profile: opt-in instrumentation, NOT normal benchmark timing ===");
        Console.WriteLine("Worker allocated bytes exclude setup on the caller and per-worker visitor factories; " +
            "native bytes reflect reserved NT buffers, not measured working set.");
        foreach (Workload workload in options.Workloads)
        foreach (BackendVariant backend in bases)
        foreach (int degree in degrees)
        {
            for (int i = 0; i < options.ProfileWarmup; i++)
                RunMeasured(options.Root, backend, workload, degree, reference);
            double[] times = new double[options.ProfileSamples];
            long maxNative = 0;
            for (int iteration = 0; iteration < options.ProfileSamples; iteration++)
            {
                MeasuredResult result = RunMeasured(options.Root, backend, workload, degree, reference);
                times[iteration] = result.Milliseconds;
                ParallelDirectoryMetrics m = result.Metrics;
                long workerBytes = m.Workers.Any(w => w.ThreadAllocatedBytes < 0) ? -1 :
                    m.Workers.Sum(w => w.ThreadAllocatedBytes);
                long native = m.Workers.Sum(w => w.ReservedNativeBytes);
                maxNative = Math.Max(maxNative, native);
                foreach (ParallelWorkerMetrics w in m.Workers)
                {
                    csv.WriteLine(string.Join(",", new[] {
                        Csv(backend.Name), workload.ToString(), degree.ToString(CI), (iteration + 1).ToString(CI),
                        w.WorkerIndex.ToString(CI), w.ManagedThreadId.ToString(CI),
                        w.Files.ToString(CI), w.DirectoryEntries.ToString(CI), w.DirectoriesScanned.ToString(CI),
                        Ms(w.ScanTicks), Ms(w.WaitTicks), Ms(w.StartDelayTicks), Ms(w.LifetimeTicks),
                        w.ThreadAllocatedBytes < 0 ? "" : w.ThreadAllocatedBytes.ToString(CI),
                        w.ReservedNativeBytes.ToString(CI), result.Milliseconds.ToString("F6", CI),
                        m.PeakQueuedDirectories.ToString(CI), m.PeakActiveWorkers.ToString(CI)
                    }));
                }
                overview.WriteLine(string.Join(",", new[] {
                    Csv(backend.Name), workload.ToString(), degree.ToString(CI), (iteration + 1).ToString(CI),
                    result.Milliseconds.ToString("F6", CI), workerBytes < 0 ? "" : workerBytes.ToString(CI),
                    result.ProcessAllocatedBytes < 0 ? "" : result.ProcessAllocatedBytes.ToString(CI),
                    native.ToString(CI), m.Workers.Sum(w => w.DirectoriesScanned).ToString(CI),
                    m.Workers.Sum(w => w.Files).ToString(CI), m.PeakQueuedDirectories.ToString(CI),
                    m.PeakActiveWorkers.ToString(CI), m.Workers.Min(w => w.DirectoriesScanned).ToString(CI),
                    m.Workers.Max(w => w.DirectoriesScanned).ToString(CI),
                    m.Workers.Min(w => w.Files).ToString(CI), m.Workers.Max(w => w.Files).ToString(CI)
                }));
            }
            Array.Sort(times);
            Console.WriteLine(" {0,-14} {1,-12} P{2,-3} median={3:F3} ms, native peak <= {4:N0} B",
                backend.Name, workload, degree, Median(times), maxNative);
        }
        Console.WriteLine("Per-worker CSV: " + Path.GetFullPath(path));
        Console.WriteLine("Scheduler CSV:  " + Path.GetFullPath(summary));
    }

    private sealed class MeasuredResult
    {
        internal readonly ParallelDirectoryMetrics Metrics;
        internal readonly double Milliseconds;
        internal readonly long ProcessAllocatedBytes;
        internal MeasuredResult(ParallelDirectoryMetrics metrics, double milliseconds, long processAllocatedBytes)
        {
            Metrics = metrics; Milliseconds = milliseconds; ProcessAllocatedBytes = processAllocatedBytes;
        }
    }

    private static MeasuredResult RunMeasured(string root, BackendVariant backend, Workload workload,
        int degree, ReferenceSnapshot expected)
    {
        var metrics = new ParallelDirectoryMetrics(degree);
        var accumulators = new ScanAccumulator[degree];
#if NET6_0_OR_GREATER
        long allocationStart = GC.GetTotalAllocatedBytes(true);
#else
        long allocationStart = -1;
#endif
        long start = Stopwatch.GetTimestamp();
        IO.FastFileEnumerator.EnumerateParallelMeasured(root, index =>
        {
            var scan = new ScanAccumulator(root, workload);
            accumulators[index] = scan;
            return scan.Callback;
        }, backend.Options, metrics);
        long end = Stopwatch.GetTimestamp();
#if NET6_0_OR_GREATER
        long allocationEnd = GC.GetTotalAllocatedBytes(true);
#else
        long allocationEnd = -1;
#endif
        long files = 0, dirs = 0, bytes = 0;
        ulong hash = 0;
        long records = 0;
        foreach (ScanAccumulator scan in accumulators)
        {
            files += scan.Files; dirs += scan.Directories;
            bytes = unchecked(bytes + scan.Bytes);
            hash = unchecked(hash + scan.Hash);
            records += scan.Records?.Count ?? 0;
        }
        if (files != expected.Files || dirs != expected.Directories || bytes != expected.Bytes ||
            (workload == Workload.HashFull && hash != expected.Hash) ||
            (workload == Workload.Materialize && records != expected.Entries.Count))
            throw new InvalidOperationException("Measured scan did not match independent reference.");
        if (metrics.Workers.Sum(w => w.Files) != expected.Files ||
            metrics.Workers.Sum(w => w.DirectoryEntries) != expected.Directories)
            throw new InvalidOperationException("Measured callback counters differ from expected totals.");
        long traversable = expected.Entries.Values.LongCount(e => e.IsDirectory &&
            (e.Attributes & FileAttributes.ReparsePoint) == 0);
        if (metrics.Workers.Sum(w => w.DirectoriesScanned) != traversable + 1)
            throw new InvalidOperationException("Directory scheduler did not process every traversable directory.");
        GC.KeepAlive(accumulators);
        return new MeasuredResult(metrics, (end - start) * 1000.0 / Stopwatch.Frequency,
            allocationStart < 0 || allocationEnd < 0 ? -1 : allocationEnd - allocationStart);
    }

    private static void PrintTree(BenchmarkOptions options, ReferenceSnapshot reference)
    {
        var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var counts = new Dictionary<string, int>(comparison) { [options.Root] = 0 };
        foreach (EntryRecord entry in reference.Entries.Values)
        {
            if (entry.IsDirectory) counts[entry.FullPath] = counts.TryGetValue(entry.FullPath, out int existing) ? existing : 0;
            else
            {
                string? parent = Path.GetDirectoryName(entry.FullPath);
                if (parent != null) counts[parent] = counts.TryGetValue(parent, out int current) ? current + 1 : 1;
            }
        }
        int[] values = counts.Values.OrderBy(x => x).ToArray();
        long top10 = counts.Values.OrderByDescending(x => x).Take(10).Sum(x => (long)x);
        Console.WriteLine();
        Console.WriteLine("=== File concentration by directory (from independent reference, no scan timed) ===");
        Console.WriteLine(" Directories={0}, p50={1}, p90={2}, p95={3}, p99={4}, max={5}, " +
            "top-10 share={6:F1}%", values.Length,
            Percentile(values, .50), Percentile(values, .90), Percentile(values, .95),
            Percentile(values, .99), values[values.Length - 1],
            reference.Files == 0 ? 0 : 100.0 * top10 / reference.Files);
        foreach (var item in counts.OrderByDescending(kv => kv.Value).Take(10))
            Console.WriteLine(" {0,7} files  {1}", item.Value, item.Key);
        string path = Sibling(options.CsvPath, "tree");
        using var csv = OpenCsv(path, "Directory,DirectFiles");
        foreach (var item in counts.OrderByDescending(kv => kv.Value))
            csv.WriteLine(Csv(item.Key) + "," + item.Value.ToString(CI));
        Console.WriteLine("Tree CSV: " + Path.GetFullPath(path));
    }

    private static int Percentile(int[] sorted, double fraction) =>
        sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(sorted.Length * fraction) - 1)];

    private static string Ms(long ticks) => (ticks * 1000.0 / Stopwatch.Frequency).ToString("F6", CI);
    // Accept both List<double> (paired P1 samples) and double[] (scheduler samples)
    // without copying the samples solely to calculate the median.
    private static double Median(IReadOnlyList<double> sorted) => sorted.Count % 2 == 0
        ? (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2
        : sorted[sorted.Count / 2];
    private static string Sibling(string file, string label)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(file)) ?? ".";
        string extension = Path.GetExtension(file);
        string name = Path.GetFileNameWithoutExtension(file);
        return Path.Combine(directory, name + "-" + label + (extension.Length == 0 ? ".csv" : extension));
    }
    private static StreamWriter OpenCsv(string path, string header)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        var stream = new StreamWriter(path, false, new UTF8Encoding(false));
        stream.WriteLine(header);
        return stream;
    }
    private static string Csv(string value) => value.IndexOfAny([',', '"', '\n', '\r']) >= 0
        ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
}
