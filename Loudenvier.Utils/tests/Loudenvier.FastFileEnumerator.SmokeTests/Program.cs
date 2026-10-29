using Loudenvier.Utils.IO;
using System.Runtime.InteropServices;

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
