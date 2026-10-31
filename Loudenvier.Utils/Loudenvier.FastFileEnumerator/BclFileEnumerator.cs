#if NET6_0_OR_GREATER
using System;
using System.IO;
using System.IO.Enumeration;

namespace Loudenvier.IO;

internal static class BclFileEnumerator
{
    internal static void Enumerate(FastFileEnumerator.Configuration config, FastFileVisitor visitor)
    {
        var options = new EnumerationOptions
        {
            BufferSize = config.BufferSize,
            AttributesToSkip = 0,
            RecurseSubdirectories = config.Recursive,
            IgnoreInaccessible = config.IgnoreInaccessible,
            ReturnSpecialDirectories = false
        };

        var entries = new FileSystemEnumerable<byte>(config.Root,
            (ref e) =>
            {
                // The projection executes synchronously while e's spans are valid.
                var entry = new FastFileEntry(ref e);
                visitor(in entry);
                return 0;
            }, options)
        {
            ShouldIncludePredicate = (ref FileSystemEntry e) =>
                config.Include(e.IsDirectory, e.FileName),
            ShouldRecursePredicate = (ref FileSystemEntry e) =>
                config.Recursive && e.IsDirectory &&
                (e.Attributes & FileAttributes.ReparsePoint) == 0
        };
        foreach (byte _ in entries) { }
    }
    /// <summary>Enumerates one directory; recursion is handled by the scheduler.</summary>
    internal static void ScanDirectory(FastFileEnumerator.Configuration config, string directory,
        FastFileVisitor visitor, Action<string> addChild)
    {
        var options = new EnumerationOptions
        {
            BufferSize = config.BufferSize,
            AttributesToSkip = 0,
            RecurseSubdirectories = false,
            IgnoreInaccessible = config.IgnoreInaccessible,
            ReturnSpecialDirectories = false
        };

        var entries = new FileSystemEnumerable<byte>(directory,
            (ref FileSystemEntry e) =>
            {
                if (e.IsDirectory && (e.Attributes & FileAttributes.ReparsePoint) == 0)
                    addChild(e.ToFullPath());
                // Files have already passed the predicate; do not match their
                // names a second time on the per-file hot path.
                if (!e.IsDirectory ||
                    (config.IncludeDirectories && config.Matches(e.FileName)))
                {
                    var entry = new FastFileEntry(ref e);
                    visitor(in entry);
                }
                return 0;
            }, options)
        {
            // Directory entries are needed for traversal even when the user
            // excludes them or the name doesn't match the search pattern.
            ShouldIncludePredicate = (ref FileSystemEntry e) =>
                e.IsDirectory || (config.IncludeFiles && config.Matches(e.FileName))
        };
        foreach (byte _ in entries) { }
    }

}
#endif
