using System;
using System.Collections.Generic;
using System.IO;
using Loudenvier.IO;

namespace Loudenvier.FastFileEnumerator.Benchmarking;

internal readonly struct EntryRecord(string fullPath, long length, FileAttributes attributes, bool isDirectory)
{
    public readonly string FullPath = fullPath;
    public readonly long Length = length;
    public readonly FileAttributes Attributes = attributes;
    public readonly bool IsDirectory = isDirectory;
}

internal sealed class ScanAccumulator
{
    private readonly string _root;
    private readonly Workload _workload;
    private readonly FastFileVisitor _callback;
    private readonly List<EntryRecord>? _records;

    public long Files { get; private set; }
    public long Directories { get; private set; }
    public long Bytes { get; private set; }
    public ulong Hash { get; private set; }
    public IReadOnlyList<EntryRecord>? Records => _records;
    public FastFileVisitor Callback => _callback;

    public ScanAccumulator(string root, Workload workload)
    {
        _root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (_root.Length == 0) _root = Path.GetPathRoot(root) ?? root;
        _workload = workload;
        _records = workload == Workload.Materialize ? new List<EntryRecord>() : null;
        _callback = OnEntry;
    }

    private void OnEntry(in FastFileEntry entry)
    {
        bool directory = entry.IsDirectory;
        if (directory) Directories++;
        else { Files++; Bytes = unchecked(Bytes + entry.Length); }

        switch (_workload)
        {
            case Workload.HashFull:
                Hash = unchecked(Hash + PathHasher.EntryHash(
                    RelativeDirectory(entry.Directory, _root.Length), entry.FileName,
                    entry.Length, entry.Attributes, directory));
                break;
            case Workload.Materialize:
                _records!.Add(new EntryRecord(entry.GetFullPath(), entry.Length, entry.Attributes, directory));
                break;
        }
    }

    // Prefix consistency is checked by the independent preflight validation.
    // Hashing takes a slice instead of paying a character-by-character prefix
    // comparison for each entry during the timed workload.
    private static ReadOnlySpan<char> RelativeDirectory(ReadOnlySpan<char> directory, int rootLength)
    {
        ReadOnlySpan<char> relative = directory[rootLength..];
        while (relative.Length > 0 && (relative[0] == '/' || relative[0] == '\\'))
            relative = relative[1..];
        while (relative.Length > 0 && (relative[^1] == '/' || relative[^1] == '\\'))
            relative = relative[..^1];
        return relative;
    }
}

internal static class PathHasher
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    // Order-independent scan fingerprint. Each entry's path + metadata gets its
    // own FNV-1a hash. Combining via addition avoids depending on directory order.
    internal static ulong EntryHash(ReadOnlySpan<char> relativeDirectory, ReadOnlySpan<char> name,
        long length, FileAttributes attributes, bool isDirectory)
    {
        ulong hash = Offset;
        AppendChars(ref hash, relativeDirectory);
        if (relativeDirectory.Length != 0) { AddByte(ref hash, (byte)'/'); AddByte(ref hash, 0); }
        AppendChars(ref hash, name);
        AddByte(ref hash, 0xff); // path/metadata delimiter
        Append64(ref hash, unchecked((ulong)length));
        Append64(ref hash, (uint)attributes);
        AddByte(ref hash, isDirectory ? (byte)1 : (byte)0);
        return hash;
    }

    internal static ulong EntryHash(string relativePath, long length,
        FileAttributes attributes, bool isDirectory)
    {
        ulong hash = Offset;
        AppendChars(ref hash, relativePath.AsSpan());
        AddByte(ref hash, 0xff);
        Append64(ref hash, unchecked((ulong)length));
        Append64(ref hash, (uint)attributes);
        AddByte(ref hash, isDirectory ? (byte)1 : (byte)0);
        return hash;
    }

    private static void AppendChars(ref ulong hash, ReadOnlySpan<char> value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = Path.DirectorySeparatorChar == '\\' && value[i] == '\\' ? '/' : value[i];
            AddByte(ref hash, unchecked((byte)c));
            AddByte(ref hash, unchecked((byte)(c >> 8)));
        }
    }
    private static void Append64(ref ulong hash, ulong value)
    {
        for (int i = 0; i < 8; i++)
        {
            AddByte(ref hash, (byte)value);
            value >>= 8;
        }
    }
    private static void AddByte(ref ulong hash, byte value)
    {
        hash = unchecked((hash ^ value) * Prime);
    }
}
