using System;

namespace Loudenvier.FastFileEnumerator.Benchmarking
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                BenchmarkOptions options = BenchmarkOptions.Parse(args);
                if (options.ShowHelp)
                {
                    Help();
                    return 0;
                }
                BenchmarkRunner.Execute(options);
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("BENCHMARK FAILED: " + error);
                Console.Error.WriteLine("Run with --help for options.");
                return 1;
            }
        }

        private static void Help()
        {
            Console.WriteLine(@"Loudenvier.FastFileEnumerator.Benchmarking

Usage:
  dotnet run -c Release -f net10.0 -- <root> [options]

Options:
  --backends auto,bcl,nt,portable   Default: all available. Unsupported backends skipped.
  --buffers 32k,52k,65535,65536   Default: 32, 52 KiB, 65535 and 65536 bytes.
                                    Plain numbers are bytes; suffix 'k' or 'KiB' means 1024 bytes.
  --workloads count,hash,materialize
                                    Default: all three.
  --samples 21                      Timed rounds per workload. N or 2*N rounds balances positions
                                    with N methods; order is rotated and reversed per full cycle.
  --warmup 3                        Warmup rounds per workload.
  --csv results.csv                 Raw measurements output (UTF-8 CSV).
  --gc-between-workloads           Full GC between workloads only, never per sample.
  --help                            Show this help.

Windows + net10.0: Auto, BCL, WindowsNt, Portable.
Windows + net472: Auto (NT), WindowsNt; Portable only on trees without legacy MAX_PATH issues.
On net472, the independent reference uses Win32 extended paths. Portable is explicitly
skipped on long-path trees because the production fallback uses legacy DirectoryInfo.
Linux/macOS + net10.0: Auto, BCL, Portable.
Linux/macOS + netstandard2.0 host: Auto (Portable), Portable.

No NuGet packages are added by this benchmark. It references the real Loudenvier.Utils project.
A complete independent reference validation runs before timing.");
        }
    }
}
