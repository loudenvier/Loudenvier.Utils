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

// Focused, controlled follow-up to managed diagnostics.
// No modifications to the production library, and no new NuGet dependencies.
internal static class FocusedDiagnosticRunner
{
    private sealed class Prepared
    {
        internal string Relative = "";
        internal string RelativeDirectory = "";
        internal string Name = "";
        internal string Directory = "";
        internal string FullPath = "";
        internal string DirectoryPrefix = "";
        internal long Length;
        internal FileAttributes Attributes;
        internal bool IsDirectory;
    }

    private enum HashCase
    {
        BaselineString, StringNoInline, FullSpan, SpanInline, SpanNoInline,
        SplitSpans, CharsOnly
    }

    private enum PathCase
    {
        Loop, DirectoryToString, NameToString, PartsToString,
        CombineOriginal, CombineOneString, CombineCachedPrefix,
        RetainOriginal, RetainOneString, RetainCachedPrefix,
        RecordsOriginal, RecordsPreSized, RecordsOneString, RecordsOneStringPreSized,
        RecordsCachedPrefix
    }

    private enum LiveCase
    {
        OriginalCountAccumulator, OriginalHashAccumulator, DiagnosticHashAccumulator, MinimalHashAccumulator,
        PathOriginal, PathOneString, RetainOriginal, RetainOneString,
        RecordsOriginal, RecordsOneString
    }

    private sealed class Observation
    {
        internal string Suite = "";
        internal string Scenario = "";
        internal int Round;
        internal double Ms;
        internal long AllocBytes;
        internal int Gc0, Gc1, Gc2;
        internal long Entries, Files, Directories, Bytes;
        internal ulong Digest;
        internal object? Retained;
    }

    private readonly struct Sample
    {
        internal readonly string Suite, Name;
        internal readonly int Round, Gc0, Gc1, Gc2;
        internal readonly double Ms;
        internal readonly long Bytes;
        internal readonly ulong Digest;
        internal Sample(Observation o)
        {
            Suite = o.Suite; Name = o.Scenario; Round = o.Round;
            Ms = o.Ms; Bytes = o.AllocBytes;
            Gc0 = o.Gc0; Gc1 = o.Gc1; Gc2 = o.Gc2; Digest = o.Digest;
        }
    }

    private static readonly CultureInfo CI = CultureInfo.InvariantCulture;
    private static long _sink;

    internal static void Execute(BenchmarkOptions options, ReferenceSnapshot reference)
    {
        BackendVariant backend = SelectBackend(options);
        Console.WriteLine();
        Console.WriteLine("=== FOCUSED diagnostics: FNV/JIT, string identity, single-allocation paths ===");
        Console.WriteLine("Runtime: " + RuntimeInformation.FrameworkDescription + "; bits=" + IntPtr.Size * 8 +
                          "; root=" + options.Root);
        Console.WriteLine("Backend: " + backend.Name + "; samples=" + options.Samples +
                          "; warmup=" + options.Warmup);
        Console.WriteLine("Data verification and string-identity checks are outside stopwatch.");
        Console.WriteLine("GC counts are process-wide; allocation bytes are current-thread only.");
        Console.WriteLine("Path results are validated against independent full paths before measuring.");
        Console.WriteLine("Original/hash variants and path variants use identical source records.");
#if NETFRAMEWORK
        if (backend.Options.Backend == FastEnumerationBackend.Portable &&
            reference.RequiresExtendedWindowsPaths && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new NotSupportedException("Portable cannot reliably enumerate this long-path tree on .NET Framework; use --diag-backend nt.");
#endif
        ReferenceScanner.Validate(options.Root, backend, reference);

        Prepared[] data = Prepare(reference);
        AssertAlgorithms(data, reference);
        AssertPathIdentity();
        ValidateLivePaths(options.Root, backend, reference);

        var samples = new List<Sample>();
        samples.AddRange(RunGroup("FNV-in-memory", (HashCase[])Enum.GetValues(typeof(HashCase)),
            options, test => RunHashMemory(data, reference, test)));
        samples.AddRange(RunGroup("Paths-in-memory", (PathCase[])Enum.GetValues(typeof(PathCase)),
            options, test => RunPathsMemory(data, reference, test)));
        samples.AddRange(RunGroup("Live-enumeration", (LiveCase[])Enum.GetValues(typeof(LiveCase)),
            options, test => RunLive(options.Root, backend, reference, test)));

        string csvPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(options.CsvPath)) ?? ".",
            Path.GetFileNameWithoutExtension(options.CsvPath) + "-diag-focused.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(csvPath)!);
        WriteCsv(csvPath, samples, options.Root, backend.Name);
        Console.WriteLine("Focused CSV: " + csvPath);
        Console.WriteLine("Hash & allocation diagnostics completed. Blackhole=" + _sink.ToString("X16", CI));
        GC.KeepAlive(data);
    }

    private static BackendVariant SelectBackend(BenchmarkOptions options)
    {
        string backend = options.DiagnosticBackend;
        FastEnumerationBackend choice;
        switch (backend)
        {
            case "auto": choice = FastEnumerationBackend.Auto; break;
            case "nt":
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    throw new NotSupportedException("Windows NT is available only on Windows.");
                choice = FastEnumerationBackend.WindowsNt; break;
            case "bcl":
#if NET6_0_OR_GREATER
                choice = FastEnumerationBackend.Bcl; break;
#else
                throw new NotSupportedException("BCL backend requires .NET 6+.");
#endif
            case "portable": choice = FastEnumerationBackend.Portable; break;
            default: throw new ArgumentException("Invalid diagnostic backend: " + backend);
        }
        return new BackendVariant(backend + "(" + options.DiagnosticBufferBytes + "B)",
            choice, options.DiagnosticBufferBytes);
    }


    private static Prepared[] Prepare(ReferenceSnapshot reference)
    {
        var items = new List<Prepared>(reference.Entries.Count);
        foreach (KeyValuePair<string, EntryRecord> item in reference.Entries)
        {
            string full = item.Value.FullPath;
            int separator = Math.Max(full.LastIndexOf('\\'), full.LastIndexOf('/'));
            if (separator < 0) throw new InvalidOperationException("Expected a full path: " + full);
            string dir = full.Substring(0, separator);
            if (separator == 0 || (separator == 2 && full.Length > 2 && full[1] == ':'))
                dir = full.Substring(0, separator + 1);
            string name = full.Substring(separator + 1);
            int relativeSeparator = item.Key.LastIndexOf('/');
            string relativeDirectory = relativeSeparator < 0 ? "" : item.Key.Substring(0, relativeSeparator);
            string relativeName = item.Key.Substring(relativeSeparator + 1);
            if (!string.Equals(name, relativeName, StringComparison.Ordinal))
                throw new InvalidOperationException("Prepared relative/absolute names differ: " + full);
            string prefix = dir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                            dir.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? dir : dir + Path.DirectorySeparatorChar;
            items.Add(new Prepared
            {
                Relative = item.Key,
                RelativeDirectory = relativeDirectory,
                Name = name, Directory = dir, DirectoryPrefix = prefix, FullPath = full,
                Length = item.Value.Length, Attributes = item.Value.Attributes,
                IsDirectory = item.Value.IsDirectory
            });
        }
        items.Sort((x, y) => StringComparer.Ordinal.Compare(x.Relative, y.Relative));
        return items.ToArray();
    }

    private static void AssertAlgorithms(Prepared[] data, ReferenceSnapshot reference)
    {
        ulong[] hashes = new ulong[6];
        foreach (Prepared e in data)
        {
            hashes[0] = unchecked(hashes[0] + PathHasher.EntryHash(e.Relative, e.Length, e.Attributes, e.IsDirectory));
            hashes[1] = unchecked(hashes[1] + FocusedHash.ContiguousStringNoInline(e.Relative, e.Length, e.Attributes, e.IsDirectory));
            hashes[2] = unchecked(hashes[2] + FocusedHash.ContiguousCore(e.Relative.AsSpan(), e.Length, e.Attributes, e.IsDirectory));
            hashes[3] = unchecked(hashes[3] + FocusedHash.ContiguousInline(e.Relative.AsSpan(), e.Length, e.Attributes, e.IsDirectory));
            hashes[4] = unchecked(hashes[4] + FocusedHash.ContiguousNoInline(e.Relative.AsSpan(), e.Length, e.Attributes, e.IsDirectory));
            hashes[5] = unchecked(hashes[5] + PathHasher.EntryHash(e.RelativeDirectory.AsSpan(), e.Name.AsSpan(), e.Length, e.Attributes, e.IsDirectory));
            string expected = e.FullPath;
            AssertPath(FocusedPaths.OneString(e.Directory.AsSpan(), e.Name.AsSpan()), expected);
            AssertPath(Path.Combine(e.Directory.AsSpan().ToString(), e.Name.AsSpan().ToString()), expected);
            AssertPath(string.Concat(e.DirectoryPrefix, e.Name), expected);
        }
        foreach (ulong h in hashes)
            if (h != reference.Hash)
                throw new InvalidOperationException("One focused hash implementation differs from the independent reference: " + h.ToString("X16"));
        Console.WriteLine("Preflight OK: " + data.Length.ToString("N0", CI) +
                          " entries; 6 hashes and 3 path builders match exactly.");
    }

    private static void AssertPath(string actual, string expected)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidOperationException("Path construction mismatch: " + actual + " != " + expected);
    }

    private static void AssertPathIdentity()
    {
        string original = new string('Q', 127);
        ReadOnlySpan<char> entire = original.AsSpan();
        ReadOnlySpan<char> slice = original.AsSpan(1, 125);
        string full = entire.ToString();
        string partial = slice.ToString();
        bool fullSame = ReferenceEquals(original, full);
        bool partialSame = ReferenceEquals(original, partial);
        Console.WriteLine("Span.ToString identity: full-string=" + fullSame +
                          "; substring=" + partialSame + "; original length=127, slice length=125.");
        if (partialSame) throw new InvalidOperationException("A sliced span unexpectedly returned its source string unchanged.");
        if (partial.Length != 125 || partial[0] != 'Q') throw new InvalidOperationException("Span identity probe failed.");
        GC.KeepAlive(original); GC.KeepAlive(full); GC.KeepAlive(partial);
    }

    private static void ValidateLivePaths(string root, BackendVariant backend, ReferenceSnapshot reference)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        IO.FastFileEnumerator.Enumerate(root, (in FastFileEntry e) =>
        {
            string path = FocusedPaths.OneString(e.Directory, e.FileName);
            string relative = ReferenceScanner.ToRelative(root, path);
            if (!seen.Add(relative) || !reference.Entries.TryGetValue(relative, out EntryRecord expected) ||
                !string.Equals(expected.FullPath, path, StringComparison.Ordinal))
                throw new InvalidOperationException("Live single-string path validation mismatch: " + path);
        }, backend.Options);
        if (seen.Count != reference.Entries.Count)
            throw new InvalidOperationException("Live single-string paths missed records.");
        Console.WriteLine("Live single-string path construction: exact matches on all " + seen.Count + " entries.");
    }

    private static List<Sample> RunGroup<T>(string title, T[] tests, BenchmarkOptions options,
        Func<T, Observation> run) where T : Enum
    {
        Console.WriteLine(); Console.WriteLine("Suite: " + title + " (" + tests.Length + " cases)");
        var output = new List<Sample>(tests.Length * options.Samples);
        var groups = tests.ToDictionary(t => t.ToString(), _ => new List<Sample>());
        foreach (T t in tests)
            for (int i = 0; i < options.Warmup; ++i) Consume(run(t));
        for (int round = 0; round < options.Samples; ++round)
        {
            foreach (T t in Rotated(tests, round))
            {
                Observation o = run(t);
                o.Suite = title; o.Scenario = t.ToString(); o.Round = round + 1;
                Consume(o);
                var sample = new Sample(o);
                output.Add(sample);
                groups[t.ToString()].Add(sample);
            }
            if ((round + 1) % 10 == 0 || round + 1 == options.Samples)
                Console.Write("\rSamples " + (round + 1) + "/" + options.Samples + "   ");
        }
        Console.WriteLine();
        Console.WriteLine(" {0,-28} {1,10} {2,10} {3,10} {4,14}  {5}",
            "Case", "Median ms", "Mean ms", "P95 ms", "Allocated B", "GC 0/1/2");
        foreach (T t in tests)
        {
            List<Sample> group = groups[t.ToString()];
            double[] times = group.Select(x => x.Ms).OrderBy(x => x).ToArray();
            long[] allocation = group.Where(x => x.Bytes >= 0).Select(x => x.Bytes).OrderBy(x => x).ToArray();
            string allocated = allocation.Length == 0 ? "N/A" : Median(allocation).ToString("N0", CI);
            Console.WriteLine(" {0,-28} {1,10:F4} {2,10:F4} {3,10:F4} {4,14}  {5}/{6}/{7}",
                t, Median(times), times.Average(), times[(int)Math.Ceiling(times.Length * 0.95) - 1],
                allocated, group.Sum(x => x.Gc0), group.Sum(x => x.Gc1), group.Sum(x => x.Gc2));
        }
        return output;
    }

    private static Observation RunHashMemory(Prepared[] data, ReferenceSnapshot reference, HashCase test)
    {
        long before = AllocationCounter.Read();
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        long start = Stopwatch.GetTimestamp();
        ulong hash = 0;
        long bytes = 0, dirs = 0, files = 0;
        foreach (Prepared e in data)
        {
            if (e.IsDirectory) dirs++;
            else { files++; bytes = unchecked(bytes + e.Length); }
            ulong h;
            switch (test)
            {
                case HashCase.BaselineString: h = PathHasher.EntryHash(e.Relative, e.Length, e.Attributes, e.IsDirectory); break;
                case HashCase.StringNoInline: h = FocusedHash.ContiguousStringNoInline(e.Relative, e.Length, e.Attributes, e.IsDirectory); break;
                case HashCase.FullSpan: h = FocusedHash.ContiguousCore(e.Relative.AsSpan(), e.Length, e.Attributes, e.IsDirectory); break;
                case HashCase.SpanInline: h = FocusedHash.ContiguousInline(e.Relative.AsSpan(), e.Length, e.Attributes, e.IsDirectory); break;
                case HashCase.SpanNoInline: h = FocusedHash.ContiguousNoInline(e.Relative.AsSpan(), e.Length, e.Attributes, e.IsDirectory); break;
                case HashCase.SplitSpans: h = PathHasher.EntryHash(e.RelativeDirectory.AsSpan(), e.Name.AsSpan(), e.Length, e.Attributes, e.IsDirectory); break;
                case HashCase.CharsOnly: h = PathHasher.CharsOnly(e.Relative.AsSpan()); break;
                default: throw new ArgumentOutOfRangeException(nameof(test));
            }
            hash = unchecked(hash + h);
        }
        long stop = Stopwatch.GetTimestamp();
        var o = Build(start, stop, before, g0, g1, g2, data.Length, files, dirs, bytes, hash);
        CheckTotals(o, reference);
        if (test != HashCase.CharsOnly && hash != reference.Hash)
            throw new InvalidOperationException("Hash memory mismatch for " + test);
        return o;
    }

    private static Observation RunPathsMemory(Prepared[] data, ReferenceSnapshot reference, PathCase test)
    {
        long before = AllocationCounter.Read();
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        long start = Stopwatch.GetTimestamp();
        List<string>? paths = IsRetainPaths(test) ? new List<string>(data.Length) : null;
        List<EntryRecord>? records = IsRecords(test)
            ? (test == PathCase.RecordsOriginal || test == PathCase.RecordsOneString
                ? new List<EntryRecord>() : new List<EntryRecord>(data.Length))
            : null;
        Dictionary<string, string>? prefixes = IsCached(test)
            ? new Dictionary<string, string>(StringComparer.Ordinal) : null;
        ulong checksum = 0;
        long bytes = 0, dirs = 0, files = 0;
        foreach (Prepared e in data)
        {
            if (e.IsDirectory) dirs++; else { files++; bytes = unchecked(bytes + e.Length); }
            string? text = null;
            switch (test)
            {
                case PathCase.Loop: break;
                case PathCase.DirectoryToString: text = e.Directory.AsSpan().ToString(); break;
                case PathCase.NameToString: text = e.Name.AsSpan().ToString(); break;
                case PathCase.PartsToString:
                    string dir = e.Directory.AsSpan().ToString();
                    string name = e.Name.AsSpan().ToString();
                    checksum = unchecked(checksum + (ulong)dir.Length);
                    text = name;
                    break;
                case PathCase.CombineOriginal:
                case PathCase.RetainOriginal:
                case PathCase.RecordsOriginal:
                case PathCase.RecordsPreSized:
                    text = Path.Combine(e.Directory.AsSpan().ToString(), e.Name.AsSpan().ToString()); break;
                case PathCase.CombineOneString:
                case PathCase.RetainOneString:
                case PathCase.RecordsOneString:
                case PathCase.RecordsOneStringPreSized:
                    text = FocusedPaths.OneString(e.Directory.AsSpan(), e.Name.AsSpan()); break;
                case PathCase.CombineCachedPrefix:
                case PathCase.RetainCachedPrefix:
                case PathCase.RecordsCachedPrefix:
                    if (!prefixes!.TryGetValue(e.Directory, out string? prefix))
                    {
                        prefix = e.Directory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                                 e.Directory.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                            ? e.Directory : e.Directory + Path.DirectorySeparatorChar;
                        prefixes.Add(e.Directory, prefix);
                    }
                    text = string.Concat(prefix, e.Name); break;
                default: throw new ArgumentOutOfRangeException(nameof(test));
            }
            if (text != null)
            {
                checksum = unchecked(checksum + (ulong)text.Length + (ulong)(text.Length > 0 ? text[0] : 0));
                paths?.Add(text);
                records?.Add(new EntryRecord(text, e.Length, e.Attributes, e.IsDirectory));
            }
        }
        long stop = Stopwatch.GetTimestamp();
        var o = Build(start, stop, before, g0, g1, g2, data.Length, files, dirs, bytes, checksum);
        o.Retained = (object?)records ?? paths;
        CheckTotals(o, reference);
        if (paths != null && paths.Count != data.Length) throw new InvalidOperationException("Retained path count mismatch.");
        if (records != null && records.Count != data.Length) throw new InvalidOperationException("Retained records count mismatch.");
        return o;
    }

    private static bool IsRetainPaths(PathCase t) =>
        t == PathCase.RetainOriginal || t == PathCase.RetainOneString || t == PathCase.RetainCachedPrefix;
    private static bool IsRecords(PathCase t) =>
        t == PathCase.RecordsOriginal || t == PathCase.RecordsPreSized ||
        t == PathCase.RecordsOneString || t == PathCase.RecordsOneStringPreSized ||
        t == PathCase.RecordsCachedPrefix;
    private static bool IsCached(PathCase t) =>
        t == PathCase.CombineCachedPrefix || t == PathCase.RetainCachedPrefix || t == PathCase.RecordsCachedPrefix;

    private static Observation RunLive(string root, BackendVariant backend, ReferenceSnapshot reference, LiveCase test)
    {
        long before = AllocationCounter.Read();
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        long start = Stopwatch.GetTimestamp();
        long files, dirs, bytes, entries;
        ulong hash;
        object? retained;
        if (test == LiveCase.OriginalCountAccumulator || test == LiveCase.OriginalHashAccumulator)
        {
            var scanner = new ScanAccumulator(root,
                test == LiveCase.OriginalHashAccumulator ? Workload.HashFull : Workload.Count);
            IO.FastFileEnumerator.Enumerate(root, scanner.Callback, backend.Options);
            files = scanner.Files; dirs = scanner.Directories; bytes = scanner.Bytes;
            entries = files + dirs; hash = scanner.Hash; retained = scanner;
        }
        else if (test == LiveCase.DiagnosticHashAccumulator)
        {
            // Mirrors the original diagnostic consumer: heap Outcome, mutable
            // fields, an extra Entries increment, and the same split FNV.
            var scanner = new DiagnosticHashScanner(root);
            IO.FastFileEnumerator.Enumerate(root, scanner.Callback, backend.Options);
            files = scanner.Files; dirs = scanner.Directories;
            bytes = scanner.Bytes; entries = scanner.Entries;
            hash = scanner.Hash; retained = scanner;
        }
        else
        {
            var scanner = new LiveScanner(root, test, reference.Entries.Count);
            IO.FastFileEnumerator.Enumerate(root, scanner.Callback, backend.Options);
            files = scanner.Files; dirs = scanner.Directories; bytes = scanner.Bytes;
            entries = scanner.Entries; hash = scanner.Hash; retained = scanner.Retained;
        }
        long stop = Stopwatch.GetTimestamp();
        var o = Build(start, stop, before, g0, g1, g2, entries, files, dirs, bytes, hash);
        o.Retained = retained;
        CheckTotals(o, reference);
        if (test == LiveCase.OriginalHashAccumulator || test == LiveCase.DiagnosticHashAccumulator ||
            test == LiveCase.MinimalHashAccumulator)
            if (hash != reference.Hash) throw new InvalidOperationException("Live FNV mismatch on " + test);
        if (retained is ICollection<EntryRecord> records && records.Count != reference.Entries.Count)
            throw new InvalidOperationException("Live record retention mismatch.");
        if (retained is ICollection<string> paths && paths.Count != reference.Entries.Count)
            throw new InvalidOperationException("Live path retention mismatch.");
        return o;
    }

    private sealed class DiagnosticHashScanner
    {
        private sealed class MutableResult
        {
            internal long Files, Directories, Entries, Bytes;
            internal ulong Hash;
        }
        private readonly MutableResult _result = new MutableResult();
        private readonly int _rootLength;
        internal long Files => _result.Files;
        internal long Directories => _result.Directories;
        internal long Entries => _result.Entries;
        internal long Bytes => _result.Bytes;
        internal ulong Hash => _result.Hash;
        internal FastFileVisitor Callback { get; }
        internal DiagnosticHashScanner(string root)
        {
            string normalized = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            _rootLength = normalized.Length == 0 ? (Path.GetPathRoot(root) ?? root).Length : normalized.Length;
            Callback = OnEntry;
        }
        private void OnEntry(in FastFileEntry e)
        {
            MutableResult r = _result;
            r.Entries++;
            if (e.IsDirectory) r.Directories++;
            else { r.Files++; r.Bytes = unchecked(r.Bytes + e.Length); }
            ReadOnlySpan<char> relative = e.Directory.Slice(_rootLength);
            while (relative.Length != 0 && (relative[0] == '/' || relative[0] == '\\'))
                relative = relative.Slice(1);
            while (relative.Length != 0 && (relative[relative.Length - 1] == '/' || relative[relative.Length - 1] == '\\'))
                relative = relative.Slice(0, relative.Length - 1);
            r.Hash = unchecked(r.Hash + PathHasher.EntryHash(relative,
                e.FileName, e.Length, e.Attributes, e.IsDirectory));
        }
    }

    private sealed class LiveScanner
    {
        private readonly string _root;
        private readonly LiveCase _test;
        private readonly List<string>? _paths;
        private readonly List<EntryRecord>? _records;
        internal long Files, Directories, Bytes, Entries;
        internal ulong Hash;
        internal object? Retained => (object?)_records ?? _paths;
        internal FastFileVisitor Callback { get; }
        internal LiveScanner(string root, LiveCase test, int capacity)
        {
            _root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (_root.Length == 0) _root = Path.GetPathRoot(root) ?? root;
            _test = test;
            _paths = test == LiveCase.RetainOriginal || test == LiveCase.RetainOneString ? new List<string>(capacity) : null;
            _records = test == LiveCase.RecordsOriginal || test == LiveCase.RecordsOneString ? new List<EntryRecord>(capacity) : null;
            Callback = OnEntry;
        }
        private void OnEntry(in FastFileEntry e)
        {
            bool directory = e.IsDirectory;
            Directories += directory ? 1 : 0;
            if (!directory) { Files++; Bytes = unchecked(Bytes + e.Length); }
            Entries++;
            if (_test == LiveCase.MinimalHashAccumulator)
            {
                ReadOnlySpan<char> part = e.Directory.Slice(_root.Length);
                while (part.Length != 0 && (part[0] == '\\' || part[0] == '/')) part = part.Slice(1);
                while (part.Length != 0 && (part[part.Length - 1] == '\\' || part[part.Length - 1] == '/'))
                    part = part.Slice(0, part.Length - 1);
                Hash = unchecked(Hash + PathHasher.EntryHash(part, e.FileName, e.Length, e.Attributes, directory));
            }
            else if (_test == LiveCase.PathOriginal || _test == LiveCase.PathOneString ||
                _test == LiveCase.RetainOriginal || _test == LiveCase.RetainOneString ||
                _test == LiveCase.RecordsOriginal || _test == LiveCase.RecordsOneString)
            {
                string full = _test == LiveCase.PathOneString || _test == LiveCase.RetainOneString ||
                              _test == LiveCase.RecordsOneString
                    ? FocusedPaths.OneString(e.Directory, e.FileName)
                    : e.GetFullPath();
                Hash = unchecked(Hash + (ulong)full.Length + (ulong)(full.Length > 0 ? full[0] : 0));
                _paths?.Add(full);
                _records?.Add(new EntryRecord(full, e.Length, e.Attributes, directory));
            }
        }
    }

    private static Observation Build(long start, long stop, long beforeAlloc,
        int beforeGc0, int beforeGc1, int beforeGc2,
        long entries, long files, long dirs, long bytes, ulong digest)
    {
        long afterAlloc = AllocationCounter.Read();
        return new Observation
        {
            Ms = (stop - start) * 1000.0 / Stopwatch.Frequency,
            AllocBytes = beforeAlloc < 0 || afterAlloc < 0 ? -1 : afterAlloc - beforeAlloc,
            Gc0 = GC.CollectionCount(0) - beforeGc0,
            Gc1 = GC.CollectionCount(1) - beforeGc1,
            Gc2 = GC.CollectionCount(2) - beforeGc2,
            Entries = entries, Files = files, Directories = dirs, Bytes = bytes, Digest = digest
        };
    }

    private static void CheckTotals(Observation o, ReferenceSnapshot expected)
    {
        if (o.Entries != expected.Entries.Count || o.Files != expected.Files ||
            o.Directories != expected.Directories || o.Bytes != expected.Bytes)
            throw new InvalidOperationException("Totals mismatch during focused benchmark. Files may have changed.");
    }
    private static void Consume(Observation o)
    {
        unchecked { _sink ^= (long)(o.Digest ^ (ulong)o.Bytes ^ (ulong)o.Entries); }
        GC.KeepAlive(o.Retained);
    }

    private static IEnumerable<T> Rotated<T>(IReadOnlyList<T> items, int round)
    {
        int n = items.Count;
        int offset = round % n;
        bool reverse = (round / n & 1) != 0;
        for (int i = 0; i < n; ++i)
        {
            int index = reverse ? (offset - i + n) % n : (offset + i) % n;
            yield return items[index];
        }
    }

    private static double Median(double[] values) => values.Length % 2 == 0
        ? (values[values.Length / 2 - 1] + values[values.Length / 2]) / 2 : values[values.Length / 2];
    private static double Median(long[] values) => values.Length % 2 == 0
        ? (values[values.Length / 2 - 1] + (double)values[values.Length / 2]) / 2 : values[values.Length / 2];

    private static void WriteCsv(string path, List<Sample> samples, string root, string backend)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine("Suite,Scenario,Iteration,ElapsedMs,AllocatedBytes,GC0,GC1,GC2,DigestHex,Runtime,ProcessBits,Backend,Root");
        string runtime = RuntimeInformation.FrameworkDescription;
        foreach (Sample s in samples)
        {
            string[] columns =
            [
                s.Suite, s.Name, s.Round.ToString(CI), s.Ms.ToString("F6", CI),
                s.Bytes < 0 ? "" : s.Bytes.ToString(CI),
                s.Gc0.ToString(CI), s.Gc1.ToString(CI), s.Gc2.ToString(CI),
                s.Digest.ToString("X16", CI), runtime,
                (IntPtr.Size * 8).ToString(CI), backend, root
            ];
            writer.WriteLine(string.Join(",", columns.Select(Csv)));
        }
    }
    private static string Csv(string text) => text.IndexOfAny([',', '"', '\r', '\n']) < 0
        ? text : "\"" + text.Replace("\"", "\"\"") + "\"";
}
