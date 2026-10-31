using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Loudenvier.IO;

namespace Loudenvier.FastFileEnumerator.Benchmarking;

// Diagnostic-only code. The production FastFileEnumerator assembly is unchanged.
internal static class DiagnosticRunner
{
    private enum ScanTest
    {
        Count, SpanWalk, HashName, HashFull,
        DirectoryString, NameString, PartsString, FullPath,
        RetainPaths, Materialize, MaterializePreSized
    }

    private enum MemoryTest
    {
        Loop, SpanWalk, FnvChars, HashName, HashFullSpan, HashFullString,
        DirectoryString, NameString, PartsString, FullPath,
        RetainDirectories, RetainNames, RetainParts, RetainPaths,
        RetainRecords, RetainRecordsPreSized, RetainRecordsConcat
    }

    private sealed class PreparedEntry
    {
        internal string Relative = "";
        internal string RelativeDirectory = "";
        internal string Name = "";
        internal string Directory = "";
        internal string DirectoryWithSeparator = "";
        internal long Length;
        internal FileAttributes Attributes;
        internal bool IsDirectory;
    }

    private sealed class Outcome
    {
        internal long Files;
        internal long Directories;
        internal long Bytes;
        internal long Entries;
        internal long Chars;
        internal ulong Digest;
        internal object? Retained;
    }

    private sealed class Sample
    {
        internal string Suite = "";
        internal string Scenario = "";
        internal int Iteration;
        internal double Ms;
        internal long Allocated;
        internal int Gc0, Gc1, Gc2;
        internal long Entries, Chars;
        internal ulong Digest;
    }

    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;
    private static long _sink;

    internal static void Execute(BenchmarkOptions options, ReferenceSnapshot reference)
    {
        BackendVariant variant = CreateVariant(options);
        if (variant.Options.Backend == FastEnumerationBackend.Portable &&
            reference.RequiresExtendedWindowsPaths &&
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
#if NETFRAMEWORK
            throw new NotSupportedException("Portable cannot scan long Windows paths on net472. Choose --diag-backend nt.");
#endif
        }
        Console.WriteLine();
        Console.WriteLine("=== Managed diagnostic suites (library source unchanged) ===");
        Console.WriteLine("Diagnostic backend: " + variant.Name + "; samples=" + options.Samples +
            "; warmup=" + options.Warmup + "; process bits=" + (IntPtr.Size * 8));
        Console.WriteLine("FNV tests consume exactly the same path and metadata encoding as the original benchmark.");
        Console.WriteLine("The memory suite uses PRELOADED immutable strings, excluding filesystem I/O.");
        Console.WriteLine("All allocations include scenario setup; retained objects remain live through measurement.");
        Console.WriteLine("GC counts are process-wide; managed bytes are per current thread.");

        Console.Write("Validating diagnostic backend against independent reference...");
        ReferenceScanner.Validate(options.Root, variant, reference);
        Console.WriteLine(" OK");

        PreparedEntry[] entries = Prepare(reference);
        CheckPrepared(entries, reference);
        Console.WriteLine("Prepared " + entries.Length.ToString("N0", CI) +
            " identical reference entries in memory (outside measurement).");

        ScanTest[] scanTests = (ScanTest[])Enum.GetValues(typeof(ScanTest));
        MemoryTest[] memoryTests = (MemoryTest[])Enum.GetValues(typeof(MemoryTest));
        string enumCsv = DiagnosticPath(options.CsvPath, "-diag-enumeration.csv");
        string memCsv = DiagnosticPath(options.CsvPath, "-diag-memory.csv");

        List<Sample> scanSamples = BenchmarkScan(options, reference, variant, scanTests);
        WriteCsv(enumCsv, scanSamples, options.Root, variant.Name);
        Console.WriteLine("Diagnostic enumeration CSV: " + enumCsv);

        // Preloaded data is intentionally reused. The collection is outside the timings.
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        List<Sample> memorySamples = BenchmarkMemory(options, reference, entries, memoryTests);
        WriteCsv(memCsv, memorySamples, options.Root, "PreloadedMemory");
        Console.WriteLine("Diagnostic in-memory CSV: " + memCsv);
        GC.KeepAlive(entries);
        Console.WriteLine("Diagnostics completed. Blackhole=" + _sink.ToString("X16", CI));
    }

    private static BackendVariant CreateVariant(BenchmarkOptions options)
    {
        string backend = options.DiagnosticBackend;
        bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        switch (backend)
        {
            case "auto":
                return new BackendVariant("Auto(" + options.DiagnosticBufferBytes + "B)", FastEnumerationBackend.Auto, options.DiagnosticBufferBytes);
            case "nt":
                if (!windows) throw new NotSupportedException("NT diagnostics require Windows; try --diag-backend bcl.");
                return new BackendVariant("Nt(" + options.DiagnosticBufferBytes + "B)", FastEnumerationBackend.WindowsNt, options.DiagnosticBufferBytes);
            case "bcl":
#if NET6_0_OR_GREATER
                return new BackendVariant("Bcl(" + options.DiagnosticBufferBytes + "B)", FastEnumerationBackend.Bcl, options.DiagnosticBufferBytes);
#else
                throw new NotSupportedException("BCL diagnostics require .NET 6+.");
#endif
            case "portable":
                return new BackendVariant("Portable", FastEnumerationBackend.Portable, options.DiagnosticBufferBytes);
            default:
                throw new ArgumentException("Invalid diagnostic backend: " + backend);
        }
    }

    private static PreparedEntry[] Prepare(ReferenceSnapshot reference)
    {
        var list = new List<PreparedEntry>(reference.Entries.Count);
        foreach (KeyValuePair<string, EntryRecord> pair in reference.Entries)
        {
            string relative = pair.Key;
            int relativeSeparator = relative.LastIndexOf('/');
            string relativeDir = relativeSeparator < 0 ? "" : relative.Substring(0, relativeSeparator);
            string name = relative.Substring(relativeSeparator + 1);
            string full = pair.Value.FullPath;
            int fullSeparator = Math.Max(full.LastIndexOf('/'), full.LastIndexOf('\\'));
            if (fullSeparator < 0) throw new InvalidOperationException("Invalid reference path: " + full);
            string directory = full.Substring(0, fullSeparator);
            if (fullSeparator == 0 || (fullSeparator == 2 && full.Length >= 3 && full[1] == ':'))
                directory = full.Substring(0, fullSeparator + 1);
            string directoryPrefix = directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? directory : directory + Path.DirectorySeparatorChar;
            if (!string.Equals(string.Concat(directoryPrefix, name), full, StringComparison.Ordinal))
                throw new InvalidOperationException("Preloaded path parts differ from reference: " + full);
            list.Add(new PreparedEntry
            {
                Relative = relative,
                RelativeDirectory = relativeDir,
                Name = name,
                Directory = directory,
                DirectoryWithSeparator = directoryPrefix,
                Length = pair.Value.Length,
                Attributes = pair.Value.Attributes,
                IsDirectory = pair.Value.IsDirectory
            });
        }
        list.Sort((a, b) => StringComparer.Ordinal.Compare(a.Relative, b.Relative));
        return list.ToArray();
    }

    private static void CheckPrepared(PreparedEntry[] items, ReferenceSnapshot reference)
    {
        ulong spanHash = 0;
        ulong stringHash = 0;
        foreach (PreparedEntry e in items)
        {
            spanHash = unchecked(spanHash + PathHasher.EntryHash(e.RelativeDirectory.AsSpan(),
                e.Name.AsSpan(), e.Length, e.Attributes, e.IsDirectory));
            stringHash = unchecked(stringHash + PathHasher.EntryHash(e.Relative, e.Length, e.Attributes, e.IsDirectory));
        }
        if (spanHash != reference.Hash || stringHash != reference.Hash)
            throw new InvalidOperationException("Prepared in-memory FNV differs from independent baseline.");
    }

    private static List<Sample> BenchmarkScan(BenchmarkOptions options, ReferenceSnapshot reference,
        BackendVariant variant, ScanTest[] tests)
    {
        Console.WriteLine(); Console.WriteLine("Suite: enumeration + callback (" + tests.Length + " scenarios)");
        foreach (ScanTest test in tests)
            for (int w = 0; w < options.Warmup; w++) RunScan(options.Root, variant, test, reference, -1);

        var byTest = tests.ToDictionary(t => t.ToString(), t => new List<Sample>(options.Samples));
        var samples = new List<Sample>(options.Samples * tests.Length);
        for (int round = 0; round < options.Samples; round++)
        {
            foreach (ScanTest test in Rotated(tests, round))
            {
                Sample sample = RunScan(options.Root, variant, test, reference, round + 1);
                samples.Add(sample); byTest[test.ToString()].Add(sample);
            }
            Progress(round + 1, options.Samples);
        }
        Console.WriteLine();
        PrintSummary(tests.Select(t => t.ToString()), byTest);
        return samples;
    }

    private static Sample RunScan(string root, BackendVariant variant, ScanTest test,
        ReferenceSnapshot expected, int iteration)
    {
        long beforeAlloc = AllocationCounter.Read();
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
        long start = Stopwatch.GetTimestamp();
        var consumer = new ScanConsumer(root, test, expected.Entries.Count);
        IO.FastFileEnumerator.Enumerate(root, consumer.Callback, variant.Options);
        long stop = Stopwatch.GetTimestamp();
        long afterAlloc = AllocationCounter.Read();
        int d0 = GC.CollectionCount(0) - gc0, d1 = GC.CollectionCount(1) - gc1, d2 = GC.CollectionCount(2) - gc2;
        Outcome r = consumer.Result;
        if (r.Files != expected.Files || r.Directories != expected.Directories || r.Bytes != expected.Bytes ||
            r.Entries != expected.Entries.Count)
            throw new InvalidOperationException("Diagnostic enumeration totals mismatch: " + test);
        if (test == ScanTest.HashFull && r.Digest != expected.Hash)
            throw new InvalidOperationException("Diagnostic HashFull mismatch.");
        if (r.Retained is ICollection<string> paths && paths.Count != expected.Entries.Count)
            throw new InvalidOperationException("Retained path count mismatch.");
        if (r.Retained is ICollection<EntryRecord> records && records.Count != expected.Entries.Count)
            throw new InvalidOperationException("Retained record count mismatch.");
        Consume(r);
        return MakeSample("Enumeration", test.ToString(), iteration, start, stop,
            beforeAlloc, afterAlloc, d0, d1, d2, r);
    }

    private sealed class ScanConsumer
    {
        private readonly string _root;
        private readonly ScanTest _test;
        private readonly FastFileVisitor _callback;
        private readonly Outcome _result = new Outcome();
        private readonly List<string>? _paths;
        private readonly List<EntryRecord>? _records;
        internal FastFileVisitor Callback => _callback;
        internal Outcome Result => _result;

        internal ScanConsumer(string root, ScanTest test, int expectedCount)
        {
            _root = NormalizeRoot(root);
            _test = test;
            _paths = test == ScanTest.RetainPaths ? new List<string>() : null;
            _records = test == ScanTest.Materialize ? new List<EntryRecord>() :
                test == ScanTest.MaterializePreSized ? new List<EntryRecord>(expectedCount) : null;
            _callback = OnEntry;
            _result.Retained = (object?)_paths ?? _records;
        }

        private void OnEntry(in FastFileEntry entry)
        {
            Outcome r = _result;
            r.Entries++;
            if (entry.IsDirectory) r.Directories++;
            else { r.Files++; r.Bytes = unchecked(r.Bytes + entry.Length); }
            ReadOnlySpan<char> relative = RelativeDirectory(entry.Directory, _root.Length);
            switch (_test)
            {
                case ScanTest.Count: break;
                case ScanTest.SpanWalk:
                    WalkChars(relative, ref r.Digest, ref r.Chars);
                    WalkChars(entry.FileName, ref r.Digest, ref r.Chars);
                    break;
                case ScanTest.HashName:
                    r.Digest = unchecked(r.Digest + PathHasher.EntryHash(ReadOnlySpan<char>.Empty,
                        entry.FileName, entry.Length, entry.Attributes, entry.IsDirectory));
                    break;
                case ScanTest.HashFull:
                    r.Digest = unchecked(r.Digest + PathHasher.EntryHash(relative,
                        entry.FileName, entry.Length, entry.Attributes, entry.IsDirectory));
                    break;
                case ScanTest.DirectoryString:
                    string directory = entry.Directory.ToString();
                    r.Chars += directory.Length;
                    r.Digest = unchecked(r.Digest + (ulong)directory.Length + (directory.Length > 0 ? (ulong)directory[0] : 0UL));
                    break;
                case ScanTest.NameString:
                    string name = entry.FileName.ToString();
                    r.Chars += name.Length;
                    r.Digest = unchecked(r.Digest + (ulong)name.Length + (name.Length > 0 ? (ulong)name[0] : 0UL));
                    break;
                case ScanTest.PartsString:
                    string dirPart = entry.Directory.ToString();
                    string namePart = entry.FileName.ToString();
                    r.Chars += dirPart.Length + namePart.Length;
                    r.Digest = unchecked(r.Digest + (ulong)(dirPart.Length ^ namePart.Length) +
                        (dirPart.Length > 0 ? (ulong)dirPart[0] : 0UL) +
                        (namePart.Length > 0 ? (ulong)namePart[0] : 0UL));
                    break;
                case ScanTest.FullPath:
                    string full = entry.GetFullPath();
                    r.Chars += full.Length;
                    r.Digest = unchecked(r.Digest + (ulong)full.Length + (full.Length > 0 ? (ulong)full[0] : 0UL));
                    break;
                case ScanTest.RetainPaths:
                    string retained = entry.GetFullPath();
                    _paths!.Add(retained);
                    r.Chars += retained.Length;
                    break;
                case ScanTest.Materialize:
                case ScanTest.MaterializePreSized:
                    string path = entry.GetFullPath();
                    _records!.Add(new EntryRecord(path, entry.Length, entry.Attributes, entry.IsDirectory));
                    r.Chars += path.Length;
                    break;
            }
        }
    }

    private static List<Sample> BenchmarkMemory(BenchmarkOptions options, ReferenceSnapshot reference,
        PreparedEntry[] entries, MemoryTest[] tests)
    {
        Console.WriteLine(); Console.WriteLine("Suite: IN-MEMORY data only (" + tests.Length + " scenarios)");
        foreach (MemoryTest test in tests)
            for (int w = 0; w < options.Warmup; w++) RunMemory(entries, reference, test, -1);
        var byTest = tests.ToDictionary(t => t.ToString(), t => new List<Sample>(options.Samples));
        var samples = new List<Sample>(options.Samples * tests.Length);
        for (int round = 0; round < options.Samples; round++)
        {
            foreach (MemoryTest test in Rotated(tests, round))
            {
                Sample sample = RunMemory(entries, reference, test, round + 1);
                samples.Add(sample); byTest[test.ToString()].Add(sample);
            }
            Progress(round + 1, options.Samples);
        }
        Console.WriteLine(); PrintSummary(tests.Select(t => t.ToString()), byTest);
        return samples;
    }

    private static Sample RunMemory(PreparedEntry[] entries, ReferenceSnapshot reference,
        MemoryTest test, int iteration)
    {
        long beforeAlloc = AllocationCounter.Read();
        int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
        long start = Stopwatch.GetTimestamp();
        Outcome result = ConsumeMemory(entries, test);
        long stop = Stopwatch.GetTimestamp();
        long afterAlloc = AllocationCounter.Read();
        int d0 = GC.CollectionCount(0) - gc0, d1 = GC.CollectionCount(1) - gc1, d2 = GC.CollectionCount(2) - gc2;
        if (result.Entries != entries.Length || result.Files != reference.Files ||
            result.Directories != reference.Directories || result.Bytes != reference.Bytes)
            throw new InvalidOperationException("Memory diagnostic totals mismatch: " + test);
        if ((test == MemoryTest.HashFullSpan || test == MemoryTest.HashFullString) &&
            result.Digest != reference.Hash)
            throw new InvalidOperationException("Memory diagnostic FNV mismatch: " + test);
        if (result.Retained is ICollection<EntryRecord> records && records.Count != entries.Length)
            throw new InvalidOperationException("Memory record count mismatch: " + test);
        if (result.Retained is ICollection<string> paths &&
            paths.Count != (test == MemoryTest.RetainParts ? entries.Length * 2 : entries.Length))
            throw new InvalidOperationException("Memory path count mismatch: " + test);
        Consume(result);
        return MakeSample("Memory", test.ToString(), iteration, start, stop,
            beforeAlloc, afterAlloc, d0, d1, d2, result);
    }

    private static Outcome ConsumeMemory(PreparedEntry[] entries, MemoryTest test)
    {
        var r = new Outcome();
        List<string>? paths = test == MemoryTest.RetainPaths ||
            test == MemoryTest.RetainDirectories || test == MemoryTest.RetainNames ||
            test == MemoryTest.RetainParts ? new List<string>() : null;
        List<EntryRecord>? records = test == MemoryTest.RetainRecords ? new List<EntryRecord>() :
            test == MemoryTest.RetainRecordsPreSized || test == MemoryTest.RetainRecordsConcat
                ? new List<EntryRecord>(entries.Length) : null;
        r.Retained = (object?)paths ?? records;

        foreach (PreparedEntry e in entries)
        {
            r.Entries++;
            if (e.IsDirectory) r.Directories++;
            else { r.Files++; r.Bytes = unchecked(r.Bytes + e.Length); }
            switch (test)
            {
                case MemoryTest.Loop: break;
                case MemoryTest.SpanWalk:
                    WalkChars(e.RelativeDirectory.AsSpan(), ref r.Digest, ref r.Chars);
                    WalkChars(e.Name.AsSpan(), ref r.Digest, ref r.Chars);
                    break;
                case MemoryTest.FnvChars:
                    r.Digest = unchecked(r.Digest + PathHasher.CharsOnly(e.Relative.AsSpan()));
                    break;
                case MemoryTest.HashName:
                    r.Digest = unchecked(r.Digest + PathHasher.EntryHash(ReadOnlySpan<char>.Empty,
                        e.Name.AsSpan(), e.Length, e.Attributes, e.IsDirectory));
                    break;
                case MemoryTest.HashFullSpan:
                    r.Digest = unchecked(r.Digest + PathHasher.EntryHash(e.RelativeDirectory.AsSpan(),
                        e.Name.AsSpan(), e.Length, e.Attributes, e.IsDirectory));
                    break;
                case MemoryTest.HashFullString:
                    r.Digest = unchecked(r.Digest + PathHasher.EntryHash(e.Relative,
                        e.Length, e.Attributes, e.IsDirectory));
                    break;
                case MemoryTest.DirectoryString:
                    string dir = e.Directory.AsSpan().ToString();
                    r.Chars += dir.Length;
                    r.Digest = unchecked(r.Digest + (ulong)dir.Length);
                    break;
                case MemoryTest.NameString:
                    string name = e.Name.AsSpan().ToString();
                    r.Chars += name.Length;
                    r.Digest = unchecked(r.Digest + (ulong)name.Length + (name.Length > 0 ? (ulong)name[0] : 0UL));
                    break;
                case MemoryTest.PartsString:
                    string partDir = e.Directory.AsSpan().ToString();
                    string partName = e.Name.AsSpan().ToString();
                    r.Chars += partDir.Length + partName.Length;
                    r.Digest = unchecked(r.Digest + (ulong)(partDir.Length ^ partName.Length));
                    break;
                case MemoryTest.FullPath:
                    string full = Path.Combine(e.Directory.AsSpan().ToString(), e.Name.AsSpan().ToString());
                    r.Chars += full.Length;
                    r.Digest = unchecked(r.Digest + (ulong)full.Length + (full.Length > 0 ? (ulong)full[0] : 0UL));
                    break;
                case MemoryTest.RetainDirectories:
                    string retainedDir = e.Directory.AsSpan().ToString();
                    paths!.Add(retainedDir);
                    r.Chars += retainedDir.Length;
                    break;
                case MemoryTest.RetainNames:
                    string retainedName = e.Name.AsSpan().ToString();
                    paths!.Add(retainedName);
                    r.Chars += retainedName.Length;
                    break;
                case MemoryTest.RetainParts:
                    string retainedPartDir = e.Directory.AsSpan().ToString();
                    string retainedPartName = e.Name.AsSpan().ToString();
                    paths!.Add(retainedPartDir);
                    paths.Add(retainedPartName);
                    r.Chars += retainedPartDir.Length + retainedPartName.Length;
                    break;
                case MemoryTest.RetainPaths:
                    string retained = Path.Combine(e.Directory.AsSpan().ToString(), e.Name.AsSpan().ToString());
                    paths!.Add(retained);
                    r.Chars += retained.Length;
                    break;
                case MemoryTest.RetainRecords:
                case MemoryTest.RetainRecordsPreSized:
                    string path = Path.Combine(e.Directory.AsSpan().ToString(), e.Name.AsSpan().ToString());
                    records!.Add(new EntryRecord(path, e.Length, e.Attributes, e.IsDirectory));
                    r.Chars += path.Length;
                    break;
                case MemoryTest.RetainRecordsConcat:
                    // Proposed lower-allocation construction, BENCHMARK ONLY;
                    // e.DirectoryWithSeparator is prebuilt outside all timed loops.
                    string concat = string.Concat(e.DirectoryWithSeparator, e.Name);
                    records!.Add(new EntryRecord(concat, e.Length, e.Attributes, e.IsDirectory));
                    r.Chars += concat.Length;
                    break;
            }
        }
        return r;
    }

    private static void WalkChars(ReadOnlySpan<char> text, ref ulong checksum, ref long charCount)
    {
        for (int i = 0; i < text.Length; ++i)
            checksum = unchecked(checksum + (uint)text[i]);
        charCount += text.Length;
    }

    private static string NormalizeRoot(string root)
    {
        string value = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return value.Length == 0 ? Path.GetPathRoot(root) ?? root : value;
    }

    private static ReadOnlySpan<char> RelativeDirectory(ReadOnlySpan<char> directory, int rootLength)
    {
        ReadOnlySpan<char> relative = directory[rootLength..];
        while (relative.Length > 0 && (relative[0] == '/' || relative[0] == '\\')) relative = relative[1..];
        while (relative.Length > 0 && (relative[relative.Length - 1] == '/' || relative[relative.Length - 1] == '\\'))
            relative = relative[..^1];
        return relative;
    }

    private static Sample MakeSample(string suite, string scenario, int iteration,
        long start, long stop, long beforeAlloc, long afterAlloc, int gc0, int gc1, int gc2, Outcome result)
    {
        return new Sample
        {
            Suite = suite, Scenario = scenario, Iteration = iteration,
            Ms = (stop - start) * 1000.0 / Stopwatch.Frequency,
            Allocated = beforeAlloc < 0 || afterAlloc < 0 ? -1 : afterAlloc - beforeAlloc,
            Gc0 = gc0, Gc1 = gc1, Gc2 = gc2,
            Entries = result.Entries, Chars = result.Chars, Digest = result.Digest
        };
    }

    private static void Consume(Outcome value)
    {
        unchecked { _sink ^= (long)(value.Digest ^ (ulong)value.Bytes ^ (ulong)value.Chars ^ (ulong)value.Entries); }
        GC.KeepAlive(value.Retained);
    }

    private static IEnumerable<T> Rotated<T>(IReadOnlyList<T> tests, int iteration)
    {
        int n = tests.Count;
        int shift = iteration % n;
        bool reversed = (iteration / n & 1) != 0;
        for (int i = 0; i < n; i++)
        {
            int index = reversed ? (shift - i + n) % n : (shift + i) % n;
            yield return tests[index];
        }
    }

    private static void Progress(int current, int total)
    {
        if (current % 10 == 0 || current == total)
            Console.Write("\rSamples: " + current + "/" + total + "   ");
    }

    private static void PrintSummary(IEnumerable<string> tests, Dictionary<string, List<Sample>> byTest)
    {
        Console.WriteLine(" {0,-24} {1,10} {2,10} {3,10} {4,10} {5,16} {6,10}",
            "Test", "Mean ms", "Median ms", "P95 ms", "Min ms", "Managed B", "GC 0/1/2");
        foreach (string test in tests)
        {
            List<Sample> data = byTest[test];
            double[] times = data.Select(s => s.Ms).OrderBy(x => x).ToArray();
            long[] bytes = data.Where(s => s.Allocated >= 0).Select(s => s.Allocated).OrderBy(x => x).ToArray();
            double median = Median(times);
            double p95 = times[(int)Math.Ceiling(times.Length * .95) - 1];
            string alloc = bytes.Length == 0 ? "N/A" : Median(bytes).ToString("N0", CI);
            Console.WriteLine(" {0,-24} {1,10:F4} {2,10:F4} {3,10:F4} {4,10:F4} {5,16} {6,10}",
                test, times.Average(), median, p95, times[0], alloc,
                data.Sum(s => s.Gc0) + "/" + data.Sum(s => s.Gc1) + "/" + data.Sum(s => s.Gc2));
        }
    }

    private static double Median(double[] values) => values.Length % 2 != 0 ? values[values.Length / 2] :
        (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2;
    private static double Median(long[] values) => values.Length % 2 != 0 ? values[values.Length / 2] :
        (values[values.Length / 2 - 1] + (double)values[values.Length / 2]) / 2;

    private static string DiagnosticPath(string mainCsv, string suffix)
    {
        string fullPath = Path.GetFullPath(mainCsv);
        string directory = Path.GetDirectoryName(fullPath) ?? ".";
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, Path.GetFileNameWithoutExtension(fullPath) + suffix);
    }

    private static void WriteCsv(string path, List<Sample> samples, string root, string backend)
    {
        using var csv = new StreamWriter(path, false, new UTF8Encoding(false));
        csv.WriteLine("Suite,Scenario,Iteration,ElapsedMs,AllocatedBytes,GC0,GC1,GC2,Entries,Chars,DigestHex,Runtime,ProcessBits,Backend,Root");
        string runtime = RuntimeInformation.FrameworkDescription;
        foreach (Sample s in samples)
        {
            string[] fields =
            [
                s.Suite, s.Scenario, s.Iteration.ToString(CI), s.Ms.ToString("F6", CI),
                s.Allocated < 0 ? "" : s.Allocated.ToString(CI),
                s.Gc0.ToString(CI), s.Gc1.ToString(CI), s.Gc2.ToString(CI),
                s.Entries.ToString(CI), s.Chars.ToString(CI), s.Digest.ToString("X16", CI),
                runtime, (IntPtr.Size * 8).ToString(CI), backend, root
            ];
            csv.WriteLine(string.Join(",", fields.Select(EscapeCsv)));
        }
    }

    private static string EscapeCsv(string s)
    {
        return s.IndexOfAny([',', '"', '\r', '\n']) < 0 ? s : "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
