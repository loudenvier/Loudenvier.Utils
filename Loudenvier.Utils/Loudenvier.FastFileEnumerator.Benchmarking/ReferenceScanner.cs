using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Loudenvier.FastFileEnumerator.Benchmarking;

internal sealed class ReferenceSnapshot
{
    public readonly Dictionary<string, EntryRecord> Entries;
    public readonly long Files;
    public readonly long Directories;
    public readonly long Bytes;
    public readonly ulong Hash;
    // DirectoryInfo on .NET Framework may fail when an enumeration pattern
    // pushes a directory path beyond legacy MAX_PATH (260 including NUL).
    public readonly bool RequiresExtendedWindowsPaths;

    public ReferenceSnapshot(Dictionary<string, EntryRecord> entries, long files, long dirs, long bytes, ulong hash)
    {
        Entries = entries; Files = files; Directories = dirs; Bytes = bytes; Hash = hash;
        RequiresExtendedWindowsPaths = entries.Values.Any(e =>
            e.IsDirectory ? e.FullPath.Length + 2 >= 260 : e.FullPath.Length + 1 >= 260);
    }
}

internal static class ReferenceScanner
{
    internal static ReferenceSnapshot Build(string root)
    {
#if NETFRAMEWORK
        // Independent long-path-aware reference on .NET Framework/Windows.
        // DirectoryInfo's legacy enumerator can fail with DirectoryNotFoundException
        // when a nested directory plus "\\*" reaches MAX_PATH.
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
            return Win32ReferenceScanner.Build(root);
#endif
        return BuildManaged(root);
    }

    private static ReferenceSnapshot BuildManaged(string root)
    {
        string normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (normalizedRoot.Length == 0) normalizedRoot = Path.GetPathRoot(root) ?? root;
        var records = new Dictionary<string, EntryRecord>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(root);
        long files = 0, dirs = 0, bytes = 0;
        ulong hash = 0;
        while (pending.Count != 0)
        {
            string directory = pending.Pop();
            foreach (FileSystemInfo info in new DirectoryInfo(directory).EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
            {
                FileAttributes attrs = info.Attributes;
                bool isDir = (attrs & FileAttributes.Directory) != 0;
                if (isDir && (attrs & FileAttributes.ReparsePoint) == 0)
                    pending.Push(info.FullName);
                long size = isDir ? 0 : ((FileInfo)info).Length;
                if (isDir) dirs++;
                else { files++; bytes = unchecked(bytes + size); }
                string relative = ToRelative(normalizedRoot, info.FullName);
                if (records.ContainsKey(relative))
                    throw new InvalidOperationException("Duplicate reference path: " + relative);
                records.Add(relative, new EntryRecord(info.FullName, size, attrs, isDir));
                hash = unchecked(hash + PathHasher.EntryHash(relative, size, attrs, isDir));
            }
        }
        return new ReferenceSnapshot(records, files, dirs, bytes, hash);
    }

    internal static string ToRelative(string root, string fullPath)
    {
        string normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (normalizedRoot.Length == 0) normalizedRoot = Path.GetPathRoot(root) ?? root;
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (fullPath.Length <= normalizedRoot.Length ||
            !fullPath.StartsWith(normalizedRoot, comparison))
            throw new InvalidOperationException("Entry is outside root: " + fullPath);
        string remainder = fullPath.Substring(normalizedRoot.Length);
        if (remainder.Length > 0 && (remainder[0] == '/' || remainder[0] == '\\'))
            remainder = remainder.Substring(1);
        else if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            throw new InvalidOperationException("Not a descendant of root: " + fullPath);
        return Path.DirectorySeparatorChar == '\\' ? remainder.Replace('\\', '/') : remainder;
    }

    internal static void Validate(string root, BackendVariant variant, ReferenceSnapshot expected)
    {
        var accumulator = new ScanAccumulator(root, Workload.Materialize);
        IO.FastFileEnumerator.Enumerate(root, accumulator.Callback, variant.Options);
        AssertTotals(variant.Name, accumulator, expected);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (EntryRecord entry in accumulator.Records!)
        {
            string key = ToRelative(root, entry.FullPath);
            if (!seen.Add(key)) throw new InvalidOperationException(variant.Name + " returned duplicate entry: " + key);
            if (!expected.Entries.TryGetValue(key, out EntryRecord actual))
                throw new InvalidOperationException(variant.Name + " returned unexpected entry: " + key);
            if (entry.IsDirectory != actual.IsDirectory || entry.Length != actual.Length || entry.Attributes != actual.Attributes)
                throw new InvalidOperationException(variant.Name + " metadata mismatch on " + key +
                    ": expected attrs=" + actual.Attributes + " length=" + actual.Length +
                    ", actual attrs=" + entry.Attributes + " length=" + entry.Length);
        }
        if (seen.Count != expected.Entries.Count)
        {
            foreach (string key in expected.Entries.Keys)
                if (!seen.Contains(key)) throw new InvalidOperationException(variant.Name + " missed entry: " + key);
        }

        var hashed = new ScanAccumulator(root, Workload.HashFull);
        IO.FastFileEnumerator.Enumerate(root, hashed.Callback, variant.Options);
        AssertTotals(variant.Name + " (hash)", hashed, expected);
        if (hashed.Hash != expected.Hash)
            throw new InvalidOperationException(variant.Name + " hash differs from independent reference: " +
                hashed.Hash.ToString("X16") + " != " + expected.Hash.ToString("X16"));
    }

    private static void AssertTotals(string name, ScanAccumulator actual, ReferenceSnapshot expected)
    {
        if (actual.Files != expected.Files || actual.Directories != expected.Directories || actual.Bytes != expected.Bytes)
            throw new InvalidOperationException(name + " totals mismatch. Expected " + expected.Directories +
                " dirs, " + expected.Files + " files, " + expected.Bytes + " bytes; got " + actual.Directories +
                " dirs, " + actual.Files + " files, " + actual.Bytes + " bytes.");
    }
}
