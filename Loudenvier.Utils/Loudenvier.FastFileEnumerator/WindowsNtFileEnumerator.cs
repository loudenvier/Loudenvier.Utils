using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Loudenvier.Utils.IO;

/// <summary>
/// Windows-only NT directory scanner. This file is safe to COMPILE on all targets;
/// no native entry point is resolved unless the WindowsNt backend is selected.
/// </summary>
internal static unsafe class WindowsNtFileEnumerator
{
    private const uint FileListDirectory = 0x00000001;
    private const uint ShareReadWriteDelete = 0x00000007;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const int FullDirectoryInformation = 2;
    private const int RecordHeaderBytes = 68;
    private const int StatusNoMoreFiles = unchecked((int)0x80000006);
    private const int StatusNoSuchFile = unchecked((int)0xC000000F);
    private const int StatusAccessDenied = unchecked((int)0xC0000022);
    private const int ErrorAccessDenied = 5;

    [StructLayout(LayoutKind.Sequential)]
    private struct IoStatusBlock
    {
        public IntPtr StatusOrPointer;
        public IntPtr Information;
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", ExactSpelling = true,
        CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string path, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", EntryPoint = "GetFullPathNameW", ExactSpelling = true,
        CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFullPathName(string path, uint capacity,
        StringBuilder buffer, IntPtr filePart);

    [DllImport("ntdll.dll", ExactSpelling = true)]
    private static extern int NtQueryDirectoryFile(
        SafeFileHandle handle, IntPtr eventHandle, IntPtr apcRoutine,
        IntPtr apcContext, out IoStatusBlock statusBlock,
        byte* buffer, uint length, int informationClass,
        [MarshalAs(UnmanagedType.U1)] bool returnSingleEntry,
        IntPtr fileName, [MarshalAs(UnmanagedType.U1)] bool restartScan);

    private readonly struct PendingDirectory
    {
        internal readonly string PublicPath;
        internal readonly string NativePath;
        internal readonly bool IsRoot;

        internal PendingDirectory(string publicPath, string nativePath, bool isRoot)
        {
            PublicPath = publicPath;
            NativePath = nativePath;
            IsRoot = isRoot;
        }
    }

    internal static void Enumerate(FastFileEnumerator.Configuration config, FastFileVisitor visitor)
    {
        IntPtr allocation = Marshal.AllocHGlobal(config.BufferSize);
        if (allocation == IntPtr.Zero) throw new OutOfMemoryException();
        try
        {
            byte* buffer = (byte*)allocation.ToPointer();
            var pending = new Stack<PendingDirectory>();
            pending.Push(new PendingDirectory(config.Root, ToExtendedPath(config.Root), true));

            while (pending.Count != 0)
            {
                PendingDirectory current = pending.Pop();
                using SafeFileHandle handle = CreateFile(current.NativePath,
                    FileListDirectory, ShareReadWriteDelete, IntPtr.Zero,
                    OpenExisting, FileFlagBackupSemantics, IntPtr.Zero);
                if (handle.IsInvalid) {
                    int error = Marshal.GetLastWin32Error();
                    if (config.IgnoreInaccessible && !current.IsRoot && error == ErrorAccessDenied)
                        continue;
                    throw new IOException("Cannot open directory '" + current.PublicPath + "'.",
                        new Win32Exception(error));
                }

                bool restart = true;
                while (true) {
                    int status = NtQueryDirectoryFile(handle, IntPtr.Zero, IntPtr.Zero,
                        IntPtr.Zero, out IoStatusBlock io, buffer, (uint)config.BufferSize,
                        FullDirectoryInformation, false, IntPtr.Zero, restart);
                    restart = false;
                    if (status == StatusNoMoreFiles || status == StatusNoSuchFile)
                        break;
                    if (status == StatusAccessDenied && config.IgnoreInaccessible && !current.IsRoot)
                        break;
                    if (status != 0)
                        throw new IOException("NtQueryDirectoryFile failed for '" +
                            current.PublicPath + "': NTSTATUS=0x" + status.ToString("X8") + ".");

                    long byteCount = io.Information.ToInt64();
                    if (byteCount < RecordHeaderBytes || byteCount > config.BufferSize)
                        throw new IOException("Unexpected byte count from NtQueryDirectoryFile.");
                    int returnedBytes = (int)byteCount;
                    int position = 0;
                    while (true) {
                        if (returnedBytes - position < RecordHeaderBytes)
                            throw new IOException("Truncated FILE_FULL_DIR_INFORMATION record.");

                        byte* record = buffer + position;
                        uint nextOffset = *(uint*)(record + 0);
                        long creation = *(long*)(record + 8);
                        long access = *(long*)(record + 16);
                        long write = *(long*)(record + 24);
                        long endOfFile = *(long*)(record + 40);
                        FileAttributes attributes = (FileAttributes)(*(uint*)(record + 56));
                        uint nameByteCount = *(uint*)(record + 60);
                        if ((nameByteCount & 1) != 0 ||
                            nameByteCount > (uint)(returnedBytes - position - RecordHeaderBytes))
                            throw new IOException("Invalid filename length in native directory record.");

                        var name = new ReadOnlySpan<char>((char*)(record + RecordHeaderBytes),
                            (int)(nameByteCount / 2));
                        if (!name.SequenceEqual(".".AsSpan()) &&
                            !name.SequenceEqual("..".AsSpan())) {
                            bool isDirectory = (attributes & FileAttributes.Directory) != 0;
                            if (config.Recursive && isDirectory &&
                                (attributes & FileAttributes.ReparsePoint) == 0) {
                                // Only traversed directory names need to become strings.
                                string childName = name.ToString();
                                pending.Push(new PendingDirectory(
                                    AppendNative(current.PublicPath, childName),
                                    AppendNative(current.NativePath, childName), false));
                            }

                            if (config.Include(isDirectory, name)) {
                                var entry = new FastFileEntry(current.PublicPath.AsSpan(), name,
                                    isDirectory ? 0 : endOfFile, attributes,
                                    creation, access, write);
                                visitor(in entry);
                            }
                        }

                        if (nextOffset == 0) break;
                        if (nextOffset < RecordHeaderBytes ||
                            nextOffset > (uint)(returnedBytes - position))
                            throw new IOException("Invalid NextEntryOffset in native directory record.");
                        position += checked((int)nextOffset);
                    }
                }
            }
        }
        finally { Marshal.FreeHGlobal(allocation); }
    }

    // Used only when Framework's Path.GetFullPath rejects a long Windows root.
    internal static string GetFullPathLong(string path)
    {
        var result = new StringBuilder(512);
        while (true)
        {
            uint required = GetFullPathName(path, (uint)result.Capacity, result, IntPtr.Zero);
            if (required == 0)
                throw new IOException("GetFullPathNameW failed.",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            if (required < result.Capacity) return result.ToString();
            if (required > 32767) throw new PathTooLongException();
            result.EnsureCapacity(checked((int)required + 1));
        }
    }

    private static string ToExtendedPath(string absolutePath)
    {
        if (absolutePath.StartsWith(@"\\?\", StringComparison.Ordinal)) return absolutePath;
        if (absolutePath.StartsWith(@"\\", StringComparison.Ordinal))
            return @"\\?\UNC\" + absolutePath.Substring(2);
        return @"\\?\" + absolutePath;
    }

    private static string AppendNative(string parent, string name) =>
        parent.EndsWith("\\", StringComparison.Ordinal) ? parent + name : parent + "\\" + name;
}
