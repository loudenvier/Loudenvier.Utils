using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Loudenvier.FastFileEnumerator.Benchmarking
{
    internal enum Workload { Count, HashFull, Materialize }

    internal sealed class BackendVariant
    {
        public string Name { get; }
        public global::Loudenvier.Utils.IO.FastEnumerationOptions Options { get; }
        public BackendVariant(string name, global::Loudenvier.Utils.IO.FastEnumerationBackend backend, int bufferBytes)
        {
            Name = name;
            Options = new global::Loudenvier.Utils.IO.FastEnumerationOptions
            {
                Backend = backend,
                BufferSize = bufferBytes,
                SearchPattern = "*",
                RecurseSubdirectories = true,
                IncludeDirectories = true,
                IncludeFiles = true,
                IgnoreInaccessible = false
            };
        }
    }

    internal sealed class BenchmarkOptions
    {
        public string Root = "";
        public string CsvPath = "benchmark-samples.csv";
        public int Samples = 21;
        public int Warmup = 3;
        public int[] Buffers = { 32 * 1024, 52 * 1024, 65535, 65536 };
        public Workload[] Workloads = { Workload.Count, Workload.HashFull, Workload.Materialize };
        public string[] Backends = { "auto", "bcl", "nt", "portable" };
        public bool ShowHelp;
        public bool ForceGcBeforeWorkload;

        public static BenchmarkOptions Parse(string[] args)
        {
            var result = new BenchmarkOptions();
            var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "--samples", "--warmup", "--buffers", "--backends", "--workloads", "--csv", "--gc-between-workloads"
            };
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (arg == "--help" || arg == "-h" || arg == "/?") { result.ShowHelp = true; return result; }
                if (arg == "--gc-between-workloads") { result.ForceGcBeforeWorkload = true; continue; }
                if (!arg.StartsWith("--", StringComparison.Ordinal))
                {
                    if (result.Root.Length != 0) throw new ArgumentException("Only one root path can be specified.");
                    result.Root = arg;
                    continue;
                }
                if (!valid.Contains(arg)) throw new ArgumentException("Unknown option: " + arg);
                if (++i >= args.Length) throw new ArgumentException("Expected a value after " + arg);
                string value = args[i];
                switch (arg.ToLowerInvariant())
                {
                    case "--samples": result.Samples = Positive(value, arg); break;
                    case "--warmup": result.Warmup = NonNegative(value, arg); break;
                    case "--csv": result.CsvPath = value; break;
                    case "--buffers": result.Buffers = ParseBuffers(value); break;
                    case "--backends": result.Backends = SplitDistinct(value); break;
                    case "--workloads": result.Workloads = ParseWorkloads(value); break;
                }
            }
            if (result.Root.Length == 0) throw new ArgumentException("Specify the root directory to enumerate.");
            result.Root = Path.GetFullPath(result.Root);
            if (!Directory.Exists(result.Root)) throw new DirectoryNotFoundException(result.Root);
            if (string.IsNullOrWhiteSpace(result.CsvPath)) throw new ArgumentException("CSV path cannot be empty.");
            return result;
        }

        public List<BackendVariant> GetVariants()
        {
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#if NET6_0_OR_GREATER
            bool bcl = true;
#else
            bool bcl = false;
#endif
            var variants = new List<BackendVariant>();
            foreach (string backend in Backends)
            {
                switch (backend.ToLowerInvariant())
                {
                    case "auto":
                        variants.Add(new BackendVariant("Auto(52K)", global::Loudenvier.Utils.IO.FastEnumerationBackend.Auto, 52 * 1024));
                        break;
                    case "bcl":
                        if (!bcl) { Console.WriteLine("Skipping BCL: this runtime target does not support it."); break; }
                        foreach (int bytes in Buffers)
                            variants.Add(new BackendVariant("Bcl(" + FormatBuffer(bytes) + ")", global::Loudenvier.Utils.IO.FastEnumerationBackend.Bcl, bytes));
                        break;
                    case "nt":
                    case "windowsnt":
                        if (!windows) { Console.WriteLine("Skipping WindowsNt: not running on Windows."); break; }
                        foreach (int bytes in Buffers)
                            variants.Add(new BackendVariant("Nt(" + FormatBuffer(bytes) + ")", global::Loudenvier.Utils.IO.FastEnumerationBackend.WindowsNt, bytes));
                        break;
                    case "portable":
                        variants.Add(new BackendVariant("Portable", global::Loudenvier.Utils.IO.FastEnumerationBackend.Portable, 52 * 1024));
                        break;
                    default: throw new ArgumentException("Unknown backend '" + backend + "'. Use auto,bcl,nt,portable.");
                }
            }
            if (variants.Count == 0) throw new ArgumentException("No available backends selected.");
            if (variants.Select(v => v.Name).Distinct(StringComparer.Ordinal).Count() != variants.Count)
                throw new ArgumentException("Duplicate backend variants requested.");
            return variants;
        }

        private static string FormatBuffer(int bytes) => bytes % 1024 == 0
            ? (bytes / 1024).ToString(CultureInfo.InvariantCulture) + "K"
            : bytes.ToString(CultureInfo.InvariantCulture) + "B";

        private static int Positive(string text, string name)
        {
            int v = NonNegative(text, name);
            if (v == 0) throw new ArgumentOutOfRangeException(name, "Must be positive.");
            return v;
        }
        private static int NonNegative(string text, string name)
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) || v < 0 || v > 10000)
                throw new ArgumentException("Invalid value for " + name + ": " + text);
            return v;
        }
        private static int[] ParseBuffers(string value)
        {
            var buffers = new List<int>();
            foreach (string raw in value.Split(','))
            {
                string part = raw.Trim();
                if (part.Length == 0) continue;
                bool kiB = part.EndsWith("k", StringComparison.OrdinalIgnoreCase) ||
                    part.EndsWith("kib", StringComparison.OrdinalIgnoreCase);
                string number = kiB ? part.Substring(0, part.Length - (part.EndsWith("kib", StringComparison.OrdinalIgnoreCase) ? 3 : 1)) : part;
                if (!int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
                    throw new ArgumentException("Invalid buffer size: " + part);
                long bytes = (long)count * (kiB ? 1024 : 1);
                if (bytes < 4096 || bytes > 1024 * 1024)
                    throw new ArgumentException("Buffer size must be between 4096 and 1048576: " + part);
                if (!buffers.Contains((int)bytes)) buffers.Add((int)bytes);
            }
            if (buffers.Count == 0) throw new ArgumentException("At least one buffer size is required.");
            return buffers.ToArray();
        }
        private static string[] SplitDistinct(string input)
        {
            var result = input.Split(',').Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (result.Length == 0) throw new ArgumentException("Selection cannot be empty.");
            return result;
        }
        private static Workload[] ParseWorkloads(string value)
        {
            var result = new List<Workload>();
            foreach (string s in SplitDistinct(value))
            {
                Workload w;
                switch (s)
                {
                    case "count": w = Workload.Count; break;
                    case "hash": case "hashfull": w = Workload.HashFull; break;
                    case "materialize": case "materialization": w = Workload.Materialize; break;
                    default: throw new ArgumentException("Unknown workload: " + s);
                }
                if (!result.Contains(w)) result.Add(w);
            }
            return result.ToArray();
        }
    }
}
