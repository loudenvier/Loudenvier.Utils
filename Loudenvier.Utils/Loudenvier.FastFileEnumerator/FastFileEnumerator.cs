using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Loudenvier.Utils.IO;

/// <summary>
/// Synchronous low-allocation filesystem enumeration. On .NET 6+ Auto uses
/// System.IO.Enumeration.FileSystemEnumerable; on older Windows targets it uses
/// NtQueryDirectoryFile; other legacy platforms use DirectoryInfo.
/// </summary>
public static class FastFileEnumerator
{
    /// <summary>
    /// Enumerates entries under <paramref name="rootPath"/> without allocating
    /// an object or filename string for every entry on BCL/NT backends.
    /// A callback may materialize a path with <see cref="FastFileEntry.GetFullPath"/>.
    /// </summary>
    public static void Enumerate(
        string rootPath, FastFileVisitor visitor, FastEnumerationOptions? options = null)
    {
        if (rootPath == null) throw new ArgumentNullException(nameof(rootPath));
        if (visitor == null) throw new ArgumentNullException(nameof(visitor));
        if (rootPath.Length == 0) throw new ArgumentException("Root cannot be empty.", nameof(rootPath));

        options ??= new FastEnumerationOptions();
        if (options.SearchPattern == null) throw new ArgumentException("SearchPattern cannot be null.", nameof(options));
        if (options.BufferSize < 4096 || options.BufferSize > 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(options), "BufferSize must be between 4096 and 1048576 bytes.");
        if (!Enum.IsDefined(typeof(FastEnumerationBackend), options.Backend))
            throw new ArgumentOutOfRangeException(nameof(options), "Unrecognized backend.");

        // Snapshot user configuration before executing callbacks.
        string normalizedRoot;
        try { normalizedRoot = Path.GetFullPath(rootPath); }
        catch (PathTooLongException) when (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // .NET Framework may reject a long path before the NT backend sees it.
            normalizedRoot = WindowsNtFileEnumerator.GetFullPathLong(rootPath);
        }

        var configuration = new Configuration(
            normalizedRoot,
            options.SearchPattern, options.RecurseSubdirectories,
            options.IncludeFiles, options.IncludeDirectories,
            options.IgnoreInaccessible,
            options.IgnoreCase ?? RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            options.BufferSize);

        FastEnumerationBackend backend = options.Backend;
        if (backend == FastEnumerationBackend.Auto)
        {
#if NET6_0_OR_GREATER
            backend = FastEnumerationBackend.Bcl;
#else
            backend = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? FastEnumerationBackend.WindowsNt
                : FastEnumerationBackend.Portable;
#endif
        }

        switch (backend)
        {
            case FastEnumerationBackend.WindowsNt:
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    throw new PlatformNotSupportedException("The Windows NT backend requires Windows.");
                WindowsNtFileEnumerator.Enumerate(configuration, visitor);
                return;
            case FastEnumerationBackend.Bcl:
#if NET6_0_OR_GREATER
                BclFileEnumerator.Enumerate(configuration, visitor);
                return;
#else
                throw new PlatformNotSupportedException("The BCL backend requires .NET 6 or later.");
#endif
            case FastEnumerationBackend.Portable:
                PortableFileEnumerator.Enumerate(configuration, visitor);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(options));
        }
    }

    internal sealed class Configuration
    {
        internal readonly string Root;
        internal readonly string Pattern;
        internal readonly bool Recursive;
        internal readonly bool IncludeFiles;
        internal readonly bool IncludeDirectories;
        internal readonly bool IgnoreInaccessible;
        internal readonly bool IgnoreCase;
        internal readonly int BufferSize;

        internal Configuration(string root, string pattern, bool recursive, bool includeFiles,
            bool includeDirectories, bool ignoreInaccessible, bool ignoreCase, int bufferSize)
        {
            Root = root;
            Pattern = pattern;
            Recursive = recursive;
            IncludeFiles = includeFiles;
            IncludeDirectories = includeDirectories;
            IgnoreInaccessible = ignoreInaccessible;
            IgnoreCase = ignoreCase;
            BufferSize = bufferSize;
        }

        internal bool Matches(ReadOnlySpan<char> fileName) =>
            FastNameMatcher.Matches(Pattern.AsSpan(), fileName, IgnoreCase);

        internal bool Include(bool isDirectory, ReadOnlySpan<char> name) =>
            (isDirectory ? IncludeDirectories : IncludeFiles) && Matches(name);
    }
}
