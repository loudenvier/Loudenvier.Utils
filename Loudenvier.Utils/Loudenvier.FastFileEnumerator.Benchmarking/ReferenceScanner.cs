using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Loudenvier.FastFileEnumerator.Benchmarking;

internal sealed class ReferenceSnapshot(Dictionary<string, EntryRecord> entries, long files, long dirs, long bytes, ulong hash)
{
    public readonly Dictionary<string, EntryRecord> Entries = entries;
    public readonly long Files = files;
    public readonly long Directories = dirs;
    public readonly long Bytes = bytes;
    public readonly ulong Hash = hash;
    // DirectoryInfo on .NET Framework may fail when an enumeration pattern
    // pushes a directory path beyond legacy MAX_PATH (260 including NUL).
    public readonly bool RequiresExtendedWindowsPaths = entries.Values.Any(e =>
            e.IsDirectory ? e.FullPath.Length + 2 >= 260 : e.FullPath.Length + 1 >= 260);
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
        string remainder = fullPath[normalizedRoot.Length..];
        if (remainder.Length > 0 && (remainder[0] == '/' || remainder[0] == '\\'))
            remainder = remainder[1..];
        else if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            throw new InvalidOperationException("Not a descendant of root: " + fullPath);
        return Path.DirectorySeparatorChar == '\\' ? remainder.Replace('\\', '/') : remainder;
    }

    internal static void Validate(string root, BackendVariant variant, ReferenceSnapshot expected)
    {
        ScanAccumulator[] scans = Scan(root, variant, Workload.Materialize);
        long files = 0, dirs = 0, bytes = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (ScanAccumulator scan in scans)
        {
            files += scan.Files; dirs += scan.Directories; bytes += scan.Bytes;
            foreach (EntryRecord entry in scan.Records!)
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
        }
        AssertTotals(variant.Name, files, dirs, bytes, expected);
        if (seen.Count != expected.Entries.Count)
        {
            foreach (string key in expected.Entries.Keys)
                if (!seen.Contains(key)) throw new InvalidOperationException(variant.Name + " missed entry: " + key);
        }

        ScanAccumulator[] hashScans = Scan(root, variant, Workload.HashFull);
        files = dirs = bytes = 0;
        ulong hash = 0;
        foreach (ScanAccumulator scan in hashScans)
        {
            files += scan.Files; dirs += scan.Directories; bytes += scan.Bytes;
            hash = unchecked(hash + scan.Hash);
        }
        AssertTotals(variant.Name + " (hash)", files, dirs, bytes, expected);
        if (hash != expected.Hash)
            throw new InvalidOperationException(variant.Name + " hash differs from independent reference: " +
                hash.ToString("X16") + " != " + expected.Hash.ToString("X16"));
    }

    private static ScanAccumulator[] Scan(string root, BackendVariant variant, Workload workload)
    {
        if (variant.ParallelWorkers == 0)
        {
            var scan = new ScanAccumulator(root, workload);
            IO.FastFileEnumerator.Enumerate(root, scan.Callback, variant.Options);
            return [scan];
        }
        var workers = new ScanAccumulator[variant.ParallelWorkers];
        IO.FastFileEnumerator.EnumerateParallel(root, index =>
        {
            var scan = new ScanAccumulator(root, workload);
            workers[index] = scan;
            return scan.Callback;
        }, variant.Options, variant.ParallelWorkers);
        return workers;
    }

    private static void AssertTotals(string name, long files, long dirs, long bytes, ReferenceSnapshot expected)
    {
        if (files != expected.Files || dirs != expected.Directories || bytes != expected.Bytes)
            throw new InvalidOperationException(name + " totals mismatch. Expected " + expected.Directories +
                " dirs, " + expected.Files + " files, " + expected.Bytes + " bytes; got " + dirs +
                " dirs, " + files + " files, " + bytes + " bytes.");
    }
}
