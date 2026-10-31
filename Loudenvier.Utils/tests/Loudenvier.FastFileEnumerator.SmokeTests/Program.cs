using Loudenvier.IO;
using System.Runtime.InteropServices;

VerifyFullPathConstruction();

string root = Path.Combine(Path.GetTempPath(), "Loudenvier.FFE." + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    Directory.CreateDirectory(Path.Combine(root, "sub"));
    Directory.CreateDirectory(Path.Combine(root, "sub", "nested"));
    File.WriteAllText(Path.Combine(root, "root.txt"), "hello");
    File.WriteAllText(Path.Combine(root, ".hidden"), "dotfile");
    File.WriteAllText(Path.Combine(root, "SUB.TXT"), "case");
    File.WriteAllText(Path.Combine(root, "sub", "ãção.txt"), "unicode");
    File.WriteAllText(Path.Combine(root, "sub", "nested", "level2.bin"), "binary");

    var backends = new List<FastEnumerationBackend>
    {
        FastEnumerationBackend.Auto,
        FastEnumerationBackend.Bcl,
        FastEnumerationBackend.Portable
    };
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        backends.Add(FastEnumerationBackend.WindowsNt);

    var expectedAll = new HashSet<string>(StringComparer.Ordinal);
    foreach (string path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
        expectedAll.Add(Path.GetRelativePath(root, path));

    VerifyParallel(root, expectedAll, backends);

    foreach (FastEnumerationBackend backend in backends)
    {
        var actualAll = new Dictionary<string, (bool isDir, long length)>(StringComparer.Ordinal);
        FastFileEnumerator.Enumerate(root, (in FastFileEntry e) =>
        {
            string relative = Path.GetRelativePath(root, e.GetFullPath());
            if (!actualAll.TryAdd(relative, (e.IsDirectory, e.Length)))
                throw new Exception("Duplicate: " + relative);
            if (e.CreationTimeUtc.Kind != DateTimeKind.Utc ||
                e.LastAccessTimeUtc.Kind != DateTimeKind.Utc ||
                e.LastWriteTimeUtc.Kind != DateTimeKind.Utc)
                throw new Exception("Non-UTC timestamp: " + relative);
        }, new FastEnumerationOptions { Backend = backend });

        Check(expectedAll.SetEquals(actualAll.Keys), backend + ": paths mismatch");
        foreach (var item in actualAll)
        {
            string path = Path.Combine(root, item.Key);
            bool isDirectory = Directory.Exists(path);
            Check(item.Value.isDir == isDirectory, backend + ": wrong kind: " + item.Key);
            Check(isDirectory ? item.Value.length == 0 : item.Value.length == new FileInfo(path).Length,
                backend + ": wrong size: " + item.Key);
        }

        var filtered = new HashSet<string>(StringComparer.Ordinal);
        FastFileEnumerator.Enumerate(root, (in FastFileEntry e) =>
        {
            Check(!e.IsDirectory, backend + ": unexpected directory");
            filtered.Add(e.FileName.ToString());
        }, new FastEnumerationOptions
        {
            Backend = backend, IncludeDirectories = false,
            SearchPattern = "*.TXT", IgnoreCase = true
        });
        Check(filtered.SetEquals(new[] { "root.txt", "SUB.TXT", "ãção.txt" }),
            backend + ": pattern filtering/recursive traversal failed");

        int topLevel = 0;
        FastFileEnumerator.Enumerate(root, (in FastFileEntry e) =>
        {
            topLevel++;
            Check(!e.Directory.ToString().Contains("sub"), backend + ": recursive result");
        }, new FastEnumerationOptions { Backend = backend, RecurseSubdirectories = false });
        Check(topLevel == 4, backend + ": nonrecursive count mismatch");

        // A consumer's IOException must never be mistaken for an inaccessible directory.
        bool propagated = false;
        try
        {
            FastFileEnumerator.Enumerate(root,
                (in FastFileEntry _) => throw new IOException("visitor failure"),
                new FastEnumerationOptions { Backend = backend, IgnoreInaccessible = true });
        }
        catch (IOException ex) when (ex.Message == "visitor failure") { propagated = true; }
        Check(propagated, backend + ": visitor exception swallowed");
        Console.WriteLine("PASS " + backend + " (enumeration, metadata, filters, recursion, visitor exceptions)");
    }

    // Wildcard semantics include literal '*' in names (possible on POSIX).
    Check(FastNameMatcher.Matches("*".AsSpan(), "literal*name".AsSpan(), false),
        "Wildcard '*' must not accidentally match as a literal");
    Check(FastNameMatcher.Matches("a?c".AsSpan(), "abc".AsSpan(), false),
        "Single-character wildcard failed");
    Check(!FastNameMatcher.Matches("a?c".AsSpan(), "ac".AsSpan(), false),
        "Single-character wildcard matched an empty character");

    // Directory links may be unavailable without privileges (especially on Windows).
    // When present, they must be reported once but never followed into a cycle.
    bool createdLink = false;
    try
    {
        Directory.CreateSymbolicLink(Path.Combine(root, "directory-link"), root);
        createdLink = true;
    }
    catch (UnauthorizedAccessException) { }
    catch (IOException) { }
    catch (PlatformNotSupportedException) { }

    if (createdLink)
    {
        foreach (FastEnumerationBackend backend in backends)
        {
            int seenLink = 0, total = 0;
            FastFileEnumerator.Enumerate(root, (in FastFileEntry e) =>
            {
                total++;
                if (e.FileName.SequenceEqual("directory-link"))
                {
                    seenLink++;
                    Check(e.IsReparsePoint, backend + ": link should be marked as reparse point");
                }
                Check(total < 100, backend + ": followed cyclic directory symlink");
            }, new FastEnumerationOptions { Backend = backend });
            Check(seenLink == 1, backend + ": directory link was not reported once");
        }
    }

    // This backend must fail gracefully rather than attempt loading ntdll outside Windows.
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        bool unsupported = false;
        try
        {
            FastFileEnumerator.Enumerate(root, (in FastFileEntry _) => { },
                new FastEnumerationOptions { Backend = FastEnumerationBackend.WindowsNt });
        }
        catch (PlatformNotSupportedException) { unsupported = true; }
        Check(unsupported, "Forced WindowsNt must be unavailable outside Windows");
    }

    Console.WriteLine("ALL SMOKE TESTS PASSED");
}
finally
{
    Directory.Delete(root, recursive: true);
}

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

// Parallel scans use worker-local sinks: none of the benchmark callbacks
// need a lock, and the union must be exactly the sequential reference set.
static void VerifyParallel(string root, HashSet<string> expected, List<FastEnumerationBackend> backends)
{
    foreach (FastEnumerationBackend backend in backends)
    {
        var options = new FastEnumerationOptions { Backend = backend };
        foreach (int degree in new[] { 1, 2, 4, 8 })
        {
            var locals = new Dictionary<string, (bool IsDir, long Size)>[degree];
            FastFileEnumerator.EnumerateParallel(root, worker =>
            {
                var seen = new Dictionary<string, (bool, long)>(StringComparer.Ordinal);
                locals[worker] = seen;
                return (in FastFileEntry e) =>
                {
                    string relative = Path.GetRelativePath(root, e.GetFullPath());
                    if (!seen.TryAdd(relative, (e.IsDirectory, e.Length)))
                        throw new Exception("Duplicate within worker: " + relative);
                    Check(e.LastWriteTimeUtc.Kind == DateTimeKind.Utc, "Parallel timestamp not UTC");
                };
            }, options, degree);

            var combined = new HashSet<string>(StringComparer.Ordinal);
            foreach (var local in locals)
                foreach (string key in local.Keys)
                    Check(combined.Add(key), backend + "/P" + degree + " duplicate: " + key);
            Check(combined.SetEquals(expected), backend + "/P" + degree + " missing/extra entries");
        }

        // Concurrent invocation is opt-in: an ordinary shared visitor must be thread-safe.
        int count = 0;
        FastFileEnumerator.EnumerateParallel(root,
            (in FastFileEntry _) => System.Threading.Interlocked.Increment(ref count),
            options, maxDegreeOfParallelism: 4);
        Check(count == expected.Count, backend + ": shared visitor count incorrect");

        var filtered = new System.Collections.Concurrent.ConcurrentDictionary<string, byte>();
        FastFileEnumerator.EnumerateParallel(root, (in FastFileEntry e) =>
        {
            Check(!e.IsDirectory, "Parallel filter returned directory");
            filtered.TryAdd(e.FileName.ToString(), 0);
        }, new FastEnumerationOptions
        {
            Backend = backend, IncludeDirectories = false, SearchPattern = "*.TXT", IgnoreCase = true
        }, 4);
        Check(filtered.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(new[] { "root.txt", "SUB.TXT", "ãção.txt" }),
            backend + ": filtered traversal failed");

        count = 0;
        FastFileEnumerator.EnumerateParallel(root,
            (in FastFileEntry _) => System.Threading.Interlocked.Increment(ref count),
            new FastEnumerationOptions { Backend = backend, RecurseSubdirectories = false }, 8);
        Check(count == 4, backend + ": nonrecursive parallel scan returned incorrect count");

        bool propagated = false;
        try
        {
            FastFileEnumerator.EnumerateParallel(root,
                (in FastFileEntry _) => throw new IOException("parallel visitor failure"),
                new FastEnumerationOptions { Backend = backend, IgnoreInaccessible = true }, 4);
        }
        catch (IOException ex) when (ex.Message == "parallel visitor failure") { propagated = true; }
        Check(propagated, backend + ": parallel callback exception was swallowed");
        Console.WriteLine("PASS parallel " + backend + " (P1/P2/P4/P8, filters, callbacks, exceptions)");
    }
}

// Regression tests for the single-allocation GetFullPath() implementation.
// Synthetic entries also cover paths that cannot normally be created on disk.
static void VerifyFullPathConstruction()
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
