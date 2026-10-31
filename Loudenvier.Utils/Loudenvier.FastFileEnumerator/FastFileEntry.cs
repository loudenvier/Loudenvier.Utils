using System;
using System.IO;
#if !NET6_0_OR_GREATER
using System.Buffers;
#endif
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

    /// <summary>
    /// Materialize the full path without allocating separate directory and file-name strings.
    /// Like <see cref="Path.Combine(string, string)"/>, an empty component or a rooted
    /// second component retains its usual behavior.
    /// </summary>
    public unsafe string GetFullPath()
    {
        if (_directory.IsEmpty) return _fileName.ToString();
        if (_fileName.IsEmpty) return _directory.ToString();

        // An actual file name from an enumerator cannot be rooted. Preserve
        // Path.Combine semantics for synthetic/exceptional input nevertheless.
        if (MayBeRooted(_fileName))
            return Path.Combine(_directory.ToString(), _fileName.ToString());

        char last = _directory[_directory.Length - 1];

        // A trailing volume separator (':' on Windows) is not handled
        // consistently by Path.Combine across runtime generations.
        // For example, modern .NET inserts a slash after "folder:", whereas
        // .NET Framework may not. This case cannot occur at the end of an
        // ordinary fully-qualified enumerated directory. Use the runtime's
        // own semantics for it without affecting the fast path.
        if (last == Path.VolumeSeparatorChar &&
            last != Path.DirectorySeparatorChar &&
            last != Path.AltDirectorySeparatorChar)
            return Path.Combine(_directory.ToString(), _fileName.ToString());

        bool addSeparator = last != Path.DirectorySeparatorChar &&
                            last != Path.AltDirectorySeparatorChar;
        int totalLength = checked(_directory.Length + _fileName.Length + (addSeparator ? 1 : 0));

#if NET6_0_OR_GREATER
        // string.Create allocates the output once and fills it directly.
        // Spans cannot be generic state arguments; pin them for the duration
        // of the synchronous string.Create callback instead.
        fixed (char* directory = _directory)
        fixed (char* fileName = _fileName)
        {
            var state = new PathCreateState(directory, _directory.Length,
                fileName, _fileName.Length, addSeparator);
            return string.Create(totalLength, state, static (destination, source) =>
            {
                var dir = new ReadOnlySpan<char>(source.Directory, source.DirectoryLength);
                var name = new ReadOnlySpan<char>(source.FileName, source.FileNameLength);
                FillPath(destination, dir, name, source.AddSeparator);
            });
        }
#else
        // String.Create is not part of netstandard2.0 or .NET Framework 4.7.2.
        // For ordinary paths, stackalloc plus the pointer string constructor
        // creates only the final managed string, as validated in the benchmark.
        const int StackChars = 2048;
        if (totalLength <= StackChars)
        {
            char* chars = stackalloc char[totalLength];
            FillPath(new Span<char>(chars, totalLength), _directory, _fileName, addSeparator);
            return new string(chars, 0, totalLength);
        }

        // Very long paths must not exhaust the stack. Rent an intermediate
        // buffer; no intermediate managed strings are created.
        char[] rented = ArrayPool<char>.Shared.Rent(totalLength);
        try
        {
            FillPath(rented.AsSpan(0, totalLength), _directory, _fileName, addSeparator);
            return new string(rented, 0, totalLength);
        }
        finally { ArrayPool<char>.Shared.Return(rented); }
#endif
    }

    private static void FillPath(Span<char> destination, ReadOnlySpan<char> directory,
        ReadOnlySpan<char> fileName, bool addSeparator)
    {
        directory.CopyTo(destination);
        int offset = directory.Length;
        if (addSeparator) destination[offset++] = Path.DirectorySeparatorChar;
        fileName.CopyTo(destination.Slice(offset));
    }

    private static bool MayBeRooted(ReadOnlySpan<char> fileName)
    {
        char first = fileName[0]; // Called only when nonempty.
        return first == Path.DirectorySeparatorChar ||
               first == Path.AltDirectorySeparatorChar ||
               (Path.DirectorySeparatorChar == '\\' && fileName.Length >= 2 &&
                fileName[1] == Path.VolumeSeparatorChar);
    }

#if NET6_0_OR_GREATER
    private readonly unsafe struct PathCreateState
    {
        internal readonly char* Directory;
        internal readonly int DirectoryLength;
        internal readonly char* FileName;
        internal readonly int FileNameLength;
        internal readonly bool AddSeparator;

        internal PathCreateState(char* directory, int directoryLength, char* fileName,
            int fileNameLength, bool addSeparator)
        {
            Directory = directory;
            DirectoryLength = directoryLength;
            FileName = fileName;
            FileNameLength = fileNameLength;
            AddSeparator = addSeparator;
        }
    }
#endif
}

/// <summary>
/// Called synchronously for each matching entry. Do not capture entry spans after return.
/// Exceptions thrown by the callback propagate to the caller.
/// </summary>
public delegate void FastFileVisitor(in FastFileEntry entry);
