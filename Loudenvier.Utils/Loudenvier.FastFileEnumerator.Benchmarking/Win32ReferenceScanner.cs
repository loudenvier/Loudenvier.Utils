using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Loudenvier.FastFileEnumerator.Benchmarking;

#if NETFRAMEWORK
/// <summary>
/// Independent Win32 FindFirstFileW / FindNextFileW reference for legacy Windows.
/// Uses the extended path prefix for *internal* filesystem calls, but keeps
/// ordinary absolute paths in the records so they match the library's public API.
/// This is validation-only, always outside the timed workloads.
/// </summary>
internal static class Win32ReferenceScanner
{
    private const int ErrorFileNotFound = 2;  // Empty directory's "*" pattern.
    private const int ErrorNoMoreFiles = 18;
    private static readonly IntPtr InvalidHandle = new IntPtr(-1);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Win32FindData
    {
        public FileAttributes Attributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint Reserved0;
        public uint Reserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string FileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string AlternateFileName;
    }

    [DllImport("kernel32.dll", EntryPoint = "FindFirstFileW", ExactSpelling = true,
        CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindFirstFile(string pattern, out Win32FindData data);

    [DllImport("kernel32.dll", EntryPoint = "FindNextFileW", ExactSpelling = true,
        CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FindNextFile(IntPtr handle, out Win32FindData data);

    [DllImport("kernel32.dll", EntryPoint = "FindClose", ExactSpelling = true,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FindClose(IntPtr handle);

    internal static ReferenceSnapshot Build(string root)
    {
        string normalizedRoot = Path.GetFullPath(root);
        var records = new Dictionary<string, EntryRecord>(StringComparer.Ordinal);
        var pending = new Stack<string>();
        pending.Push(normalizedRoot);
        long files = 0, dirs = 0, bytes = 0;
        ulong hash = 0;

        while (pending.Count != 0)
        {
            string directory = pending.Pop();
            string wildcard = Append(ExtendedPath(directory), "*");
            IntPtr handle = FindFirstFile(wildcard, out Win32FindData data);
            if (handle == InvalidHandle)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == ErrorFileNotFound) continue; // Empty directory.
                throw new IOException("Win32 reference cannot enumerate '" + directory +
                    "' (Win32 error " + error + ").", new Win32Exception(error));
            }

            try
            {
                while (true)
                {
                    string name = data.FileName;
                    if (name != "." && name != "..")
                    {
                        string path = Append(directory, name);
                        FileAttributes attributes = data.Attributes;
                        bool isDirectory = (attributes & FileAttributes.Directory) != 0;
                        long length = isDirectory ? 0 :
                            unchecked((long)(((ulong)data.FileSizeHigh << 32) | data.FileSizeLow));

                        if (isDirectory && (attributes & FileAttributes.ReparsePoint) == 0)
                            pending.Push(path);

                        string relative = ReferenceScanner.ToRelative(normalizedRoot, path);
                        if (records.ContainsKey(relative))
                            throw new InvalidOperationException("Duplicate reference path: " + relative);
                        records.Add(relative, new EntryRecord(path, length, attributes, isDirectory));
                        hash = unchecked(hash + PathHasher.EntryHash(relative, length, attributes, isDirectory));
                        if (isDirectory) ++dirs;
                        else { ++files; bytes = unchecked(bytes + length); }
                    }
                    if (FindNextFile(handle, out data)) continue;
                    int nextError = Marshal.GetLastWin32Error();
                    if (nextError != ErrorNoMoreFiles)
                        throw new IOException("Win32 reference failed while enumerating '" + directory +
                            "' (Win32 error " + nextError + ").", new Win32Exception(nextError));
                    break;
                }
            }
            finally
            {
                FindClose(handle);
            }
        }
        return new ReferenceSnapshot(records, files, dirs, bytes, hash);
    }

    private static string ExtendedPath(string absolutePath)
    {
        if (absolutePath.StartsWith(@"\\?\", StringComparison.Ordinal)) return absolutePath;
        if (absolutePath.StartsWith(@"\\", StringComparison.Ordinal))
            return @"\\?\UNC\" + absolutePath[2..];
        return @"\\?\" + absolutePath;
    }

    private static string Append(string directory, string name)
    {
        return (directory.EndsWith("\\", StringComparison.Ordinal) ? directory : directory + "\\") + name;
    }
}
#endif
