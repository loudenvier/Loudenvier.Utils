using System;
using System.IO;
#if NET6_0_OR_GREATER
using System.IO.Enumeration;
#endif

namespace Loudenvier.IO;

/// <summary>
/// A transient view of a directory entry. Its spans are valid ONLY during the
/// synchronous <see cref="FastFileVisitor"/> invocation; copy them if retaining data.
/// Timestamps are converted lazily; no date conversion is needed for Count scans.
/// </summary>
public readonly ref struct FastFileEntry
{
    private readonly ReadOnlySpan<char> _directory;
    private readonly ReadOnlySpan<char> _fileName;
    private readonly long _creation;
    private readonly long _lastAccess;
    private readonly long _lastWrite;
    private readonly byte _timeFormat; // 0: NT FILETIME, 1: UTC DateTime ticks, 2: BCL entry.
#if NET6_0_OR_GREATER
    private readonly FileSystemEntry _bclEntry;
#endif

    // Native timestamps are 100-nanosecond intervals since 1601-01-01 UTC.
    internal FastFileEntry(ReadOnlySpan<char> directory, ReadOnlySpan<char> fileName,
        long length, FileAttributes attributes,
        long creationFileTime, long lastAccessFileTime, long lastWriteFileTime)
    {
        _directory = directory;
        _fileName = fileName;
        Length = length;
        Attributes = attributes;
        _creation = creationFileTime;
        _lastAccess = lastAccessFileTime;
        _lastWrite = lastWriteFileTime;
        _timeFormat = 0;
#if NET6_0_OR_GREATER
        _bclEntry = default;
#endif
    }

    // Portable DateTime metadata is already supplied by the fallback's FileSystemInfo.
    internal FastFileEntry(ReadOnlySpan<char> directory, ReadOnlySpan<char> fileName,
        long length, FileAttributes attributes,
        DateTime creationTimeUtc, DateTime lastAccessTimeUtc, DateTime lastWriteTimeUtc)
    {
        _directory = directory;
        _fileName = fileName;
        Length = length;
        Attributes = attributes;
        _creation = creationTimeUtc.Ticks;
        _lastAccess = lastAccessTimeUtc.Ticks;
        _lastWrite = lastWriteTimeUtc.Ticks;
        _timeFormat = 1;
#if NET6_0_OR_GREATER
        _bclEntry = default;
#endif
    }

#if NET6_0_OR_GREATER
    internal FastFileEntry(ref FileSystemEntry entry)
    {
        _directory = entry.Directory;
        _fileName = entry.FileName;
        Length = entry.IsDirectory ? 0 : entry.Length;
        Attributes = entry.Attributes;
        _creation = 0;
        _lastAccess = 0;
        _lastWrite = 0;
        _timeFormat = 2;
        _bclEntry = entry;
    }
#endif

    /// <summary>Full path of the directory containing this entry. Not null terminated.</summary>
    public ReadOnlySpan<char> Directory => _directory;

    /// <summary>Name of this entry, without its parent path. Not null terminated.</summary>
    public ReadOnlySpan<char> FileName => _fileName;

    /// <summary>Size in bytes for a file; zero for a directory.</summary>
    public long Length { get; }
    public FileAttributes Attributes { get; }
    public bool IsDirectory => (Attributes & FileAttributes.Directory) != 0;
    public bool IsReparsePoint => (Attributes & FileAttributes.ReparsePoint) != 0;
    public DateTime CreationTimeUtc
    {
        get
        {
#if NET6_0_OR_GREATER
            if (_timeFormat == 2) return _bclEntry.CreationTimeUtc.UtcDateTime;
#endif
            return _timeFormat == 0 ? DateTime.FromFileTimeUtc(_creation)
                : new DateTime(_creation, DateTimeKind.Utc);
        }
    }
    public DateTime LastAccessTimeUtc
    {
        get
        {
#if NET6_0_OR_GREATER
            if (_timeFormat == 2) return _bclEntry.LastAccessTimeUtc.UtcDateTime;
#endif
            return _timeFormat == 0 ? DateTime.FromFileTimeUtc(_lastAccess)
                : new DateTime(_lastAccess, DateTimeKind.Utc);
        }
    }
    public DateTime LastWriteTimeUtc
    {
        get
        {
#if NET6_0_OR_GREATER
            if (_timeFormat == 2) return _bclEntry.LastWriteTimeUtc.UtcDateTime;
#endif
            return _timeFormat == 0 ? DateTime.FromFileTimeUtc(_lastWrite)
                : new DateTime(_lastWrite, DateTimeKind.Utc);
        }
    }

    /// <summary>Materialize the full path, allocating strings only when requested.</summary>
    public string GetFullPath() => Path.Combine(_directory.ToString(), _fileName.ToString());
}

/// <summary>
/// Called synchronously for each matching entry. Do not capture entry spans after return.
/// Exceptions thrown by the callback propagate to the caller.
/// </summary>
public delegate void FastFileVisitor(in FastFileEntry entry);
