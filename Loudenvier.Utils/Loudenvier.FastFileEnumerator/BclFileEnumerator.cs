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
}
#endif
