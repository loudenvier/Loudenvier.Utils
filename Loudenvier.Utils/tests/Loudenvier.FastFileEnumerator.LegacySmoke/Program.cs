using System;
using System.IO;
using System.Linq;
using Loudenvier.IO;

namespace Loudenvier.FastFileEnumerator.LegacySmoke;

internal static class Program
{
    private static void Main()
    {
        VerifyFullPathConstruction();
        string root = Path.Combine(Path.GetTempPath(), "Loudenvier.FFE.Legacy." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "child"));
            File.WriteAllText(Path.Combine(root, "one.txt"), "123");
            File.WriteAllText(Path.Combine(root, "child", "two.txt"), "45678");

            foreach (FastEnumerationBackend backend in new[] {
                FastEnumerationBackend.Auto, FastEnumerationBackend.WindowsNt,
                FastEnumerationBackend.Portable })
            {
                long size = 0;
                int files = 0, dirs = 0;
                IO.FastFileEnumerator.Enumerate(root, (in FastFileEntry e) =>
                {
                    if (e.IsDirectory) dirs++;
                    else { files++; size += e.Length; }
                    if (e.LastWriteTimeUtc.Kind != DateTimeKind.Utc)
                        throw new Exception("Non-UTC date");
                    string fullPath = e.GetFullPath();
                    if (e.IsDirectory ? !Directory.Exists(fullPath) : !File.Exists(fullPath))
                        throw new Exception("Wrong full path: " + fullPath);
                }, new FastEnumerationOptions { Backend = backend });
                if (dirs != 1 || files != 2 || size != 8)
                    throw new Exception("Invalid results for " + backend);
                foreach (int degree in new[] { 1, 2, 4, 8 })
                {
                    var locals = new int[degree];
                    IO.FastFileEnumerator.EnumerateParallel(root, worker =>
                        (in FastFileEntry _) => locals[worker]++,
                        new FastEnumerationOptions { Backend = backend }, degree);
                    if (locals.Sum() != 3)
                        throw new Exception("Parallel counts incorrect for " + backend + "/P" + degree);
                }
                bool propagated = false;
                try
                {
                    IO.FastFileEnumerator.EnumerateParallel(root,
                        (in FastFileEntry _) => throw new IOException("parallel error"),
                        new FastEnumerationOptions { Backend = backend, IgnoreInaccessible = true }, 4);
                }
                catch (IOException ex) when (ex.Message == "parallel error") { propagated = true; }
                if (!propagated) throw new Exception("Parallel callback exception lost");
                Console.WriteLine("PASS .NET Framework 4.7.2: " + backend + " sequential and P1/P2/P4/P8");
            }
        }
        finally { Directory.Delete(root, true); }
    }

// Regression tests for the single-allocation GetFullPath() implementation.
// Synthetic entries also cover paths that cannot normally be created on disk.
private static void VerifyFullPathConstruction()
{
    string sep = Path.DirectorySeparatorChar.ToString();
    string alt = Path.AltDirectorySeparatorChar.ToString();
    string volume = Path.VolumeSeparatorChar.ToString();
    string[][] combinations =
    {
        new[] { "", "" },
        new[] { "", "file.txt" },
        new[] { "folder", "" },
        new[] { "folder", "file.txt" },
        new[] { "folder" + sep, "file.txt" },
        new[] { "folder" + alt, "file.txt" },
        new[] { "folder" + volume, "file.txt" },
        new[] { "folder" + volume, "subfolder" + sep + "file.txt" },
        new[] { "folder" + volume, "漢字.txt" },
        new[] { sep, "file.txt" },
        new[] { "relative" + sep + "sub", "ãção-\uD83D\uDE80.txt" },
        new[] { "relative" + sep + "sub", "naïve_漢字.txt" },
        new[] { "directory", sep + "rooted.txt" },
        new[] { "folder", "file.name.with.dots" },
        new[] { "dir name", "with spaces" }
    };

    foreach (string[] pair in combinations)
    {
        string expected = Path.Combine(pair[0], pair[1]);
        var entry = new FastFileEntry(pair[0].AsSpan(), pair[1].AsSpan(),
            0, FileAttributes.Normal, 0L, 0L, 0L);
        string actual = entry.GetFullPath();
        Check(actual == expected,
            "GetFullPath differs from Path.Combine: directory=[" + pair[0] +
            "], name=[" + pair[1] + "], expected=[" + expected +
            "], actual=[" + actual + "]");
    }

    if (Path.DirectorySeparatorChar == '\\')
    {
        string[][] windowsCases =
        {
            new[] { @"C:", "relative.txt" },
            new[] { "folder:", "file.txt" },
            new[] { "folder:", @"subdir\file.txt" },
            new[] { @"C:\\", "relative.txt" },
            new[] { @"C:\\dir", "relative.txt" },
            new[] { @"C:\\dir", @"D:\\rooted.txt" },
            new[] { "folder", @"C:drive-relative.txt" },
            new[] { @"\\server\share", "file.txt" }
        };
        foreach (string[] pair in windowsCases)
        {
            var entry = new FastFileEntry(pair[0].AsSpan(), pair[1].AsSpan(),
                0, FileAttributes.Normal, 0L, 0L, 0L);
            Check(entry.GetFullPath() == Path.Combine(pair[0], pair[1]),
                "Windows GetFullPath mismatch: " + pair[0] + " / " + pair[1]);
        }
    }

    // Exercise the pooled-buffer fallback on net472/netstandard2.0.
    string longDirectory = new string('d', 4096);
    string longName = "f-\uD83D\uDE80.txt";
    var longEntry = new FastFileEntry(longDirectory.AsSpan(), longName.AsSpan(),
        0, FileAttributes.Normal, 0L, 0L, 0L);
    string longExpected = longDirectory + sep + longName;
    Check(longEntry.GetFullPath() == longExpected, "GetFullPath failed long path");
    Console.WriteLine("PASS GetFullPath (empty, rooted, separators, Unicode, long paths)");
}

private static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
}
