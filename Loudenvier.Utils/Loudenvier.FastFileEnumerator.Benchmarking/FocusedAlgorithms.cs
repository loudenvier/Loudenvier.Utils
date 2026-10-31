using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Loudenvier.FastFileEnumerator.Benchmarking;

// ALL code here is experimental; production Loudenvier.FastFileEnumerator is untouched.
internal static class FocusedHash
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    // Full, contiguous ReadOnlySpan<char>, including the SAME metadata encoding
    // and path normalization as PathHasher.EntryHash(string, ...).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ContiguousInline(ReadOnlySpan<char> relativePath, long length,
        FileAttributes attributes, bool directory) => ContiguousCore(relativePath, length, attributes, directory);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static ulong ContiguousNoInline(ReadOnlySpan<char> relativePath, long length,
        FileAttributes attributes, bool directory) => ContiguousCore(relativePath, length, attributes, directory);

    internal static ulong ContiguousCore(ReadOnlySpan<char> relativePath, long length,
        FileAttributes attributes, bool directory)
    {
        ulong hash = Offset;
        Append(ref hash, relativePath);
        Byte(ref hash, 0xff);
        U64(ref hash, unchecked((ulong)length));
        U64(ref hash, (uint)attributes);
        Byte(ref hash, directory ? (byte)1 : (byte)0);
        return hash;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static ulong ContiguousStringNoInline(string relativePath, long length,
        FileAttributes attributes, bool directory)
        => PathHasher.EntryHash(relativePath, length, attributes, directory);

    private static void Append(ref ulong hash, ReadOnlySpan<char> value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = Path.DirectorySeparatorChar == '\\' && value[i] == '\\' ? '/' : value[i];
            Byte(ref hash, unchecked((byte)c));
            Byte(ref hash, unchecked((byte)(c >> 8)));
        }
    }
    private static void U64(ref ulong hash, ulong value)
    {
        for (int i = 0; i < 8; ++i) { Byte(ref hash, (byte)value); value >>= 8; }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Byte(ref ulong hash, byte value)
        => hash = unchecked((hash ^ value) * Prime);
}

internal static class FocusedPaths
{
    // Builds ONE managed string from the actual directory and filename spans.
    // The stack buffer is temporary memory, not a managed allocation. The rare
    // very-long-path case uses a managed char[] fallback to avoid large stackalloc.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static unsafe string OneString(ReadOnlySpan<char> directory, ReadOnlySpan<char> filename)
    {
        bool separator = directory.Length != 0 &&
            directory[directory.Length - 1] != Path.DirectorySeparatorChar &&
            directory[directory.Length - 1] != Path.AltDirectorySeparatorChar;
        int length = checked(directory.Length + filename.Length + (separator ? 1 : 0));
        if (length <= 2048)
        {
            char* chars = stackalloc char[length];
            Fill(new Span<char>(chars, length), directory, filename, separator);
            return new string(chars, 0, length);
        }
        // Still correct on pathological long paths, with one extra allocation.
        char[] temp = new char[length];
        Fill(temp.AsSpan(), directory, filename, separator);
        fixed (char* chars = temp) return new string(chars, 0, length);
    }

    private static void Fill(Span<char> target, ReadOnlySpan<char> directory,
        ReadOnlySpan<char> filename, bool separator)
    {
        directory.CopyTo(target);
        int offset = directory.Length;
        if (separator) target[offset++] = Path.DirectorySeparatorChar;
        filename.CopyTo(target.Slice(offset));
    }
}
