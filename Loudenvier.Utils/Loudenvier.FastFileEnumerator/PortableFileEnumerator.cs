using System;
using System.Collections.Generic;
using System.IO;

namespace Loudenvier.IO;

/// <summary>Dependency-free fallback for any platform supported by netstandard2.0.</summary>
internal static class PortableFileEnumerator
{
    internal static void Enumerate(FastFileEnumerator.Configuration config, FastFileVisitor visitor)
    {
        var pending = new Stack<string>();
        pending.Push(config.Root);
        bool isRoot = true;

        while (pending.Count != 0)
        {
            string current = pending.Pop();
            bool root = isRoot;
            isRoot = false;
            IEnumerator<FileSystemInfo> iterator;
            try
            {
                iterator = new DirectoryInfo(current)
                    .EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly).GetEnumerator();
            }
            catch (UnauthorizedAccessException) when (config.IgnoreInaccessible && !root) { continue; }
            catch (IOException) when (config.IgnoreInaccessible && !root) { continue; }

            using (iterator)
            {
                while (true)
                {
                    FileSystemInfo info;
                    try
                    {
                        if (!iterator.MoveNext()) break;
                        info = iterator.Current;
                    }
                    catch (UnauthorizedAccessException) when (config.IgnoreInaccessible && !root) { break; }
                    catch (IOException) when (config.IgnoreInaccessible && !root) { break; }

                    FileAttributes attrs;
                    try { attrs = info.Attributes; }
                    catch (UnauthorizedAccessException) when (config.IgnoreInaccessible && !root) { continue; }
                    catch (IOException) when (config.IgnoreInaccessible && !root) { continue; }

                    bool isDirectory = (attrs & FileAttributes.Directory) != 0;
                    ReadOnlySpan<char> name = info.Name.AsSpan();
                    if (config.Recursive && isDirectory && (attrs & FileAttributes.ReparsePoint) == 0)
                        pending.Push(info.FullName);

                    if (!config.Include(isDirectory, name)) continue;

                    long length;
                    DateTime creationTime, accessTime, writeTime;
                    try
                    {
                        length = isDirectory ? 0 : ((FileInfo)info).Length;
                        creationTime = info.CreationTimeUtc;
                        accessTime = info.LastAccessTimeUtc;
                        writeTime = info.LastWriteTimeUtc;
                    }
                    catch (UnauthorizedAccessException) when (config.IgnoreInaccessible && !root) { continue; }
                    catch (IOException) when (config.IgnoreInaccessible && !root) { continue; }

                    var entry = new FastFileEntry(current.AsSpan(), name, length, attrs,
                        creationTime, accessTime, writeTime);
                    // Do not catch user callbacks: all their exceptions must propagate.
                    visitor(in entry);
                }
            }
        }
    }
}
