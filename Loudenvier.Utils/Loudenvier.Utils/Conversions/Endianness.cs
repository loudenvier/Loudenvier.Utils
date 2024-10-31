using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Loudenvier.Utils;

public static class Endianness
{
    public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;
    public static readonly bool IsBigEndian = !IsLittleEndian;

    /// <summary>
    /// Changes the endiannes (byte order) of the <see cref="long"/> <paramref name="value"/>
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>The value in the "opposite" endiannes/byte-order</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ChangeByteOrder(this long value) =>
        BinaryPrimitives.ReverseEndianness(value);
        /*(((long)ChangeByteOrder((int)value) & 0xFFFFFFFF) << 32) |
        ((long)ChangeByteOrder((int)(value >> 32)) & 0xFFFFFFFF);*/

    /// <summary>
    /// Changes the endiannes (byte order) of the <see cref="int"/> <paramref name="value"/>
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>The value in the "opposite" endiannes/byte-order</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ChangeByteOrder(this int value) =>
        BinaryPrimitives.ReverseEndianness(value);
        /*(((int)ChangeByteOrder((short)value) & 0xFFFF) << 16) |
        ((int)ChangeByteOrder((short)(value >> 16)) & 0xFFFF);*/

    /// <summary>
    /// Changes the endiannes (byte order) of the <see cref="short"/> <paramref name="value"/>
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>The value in the "opposite" endiannes/byte-order</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short ChangeByteOrder(this short value) =>
        // for shorts this is faster than BinaryPrimitives.ReverseEndianness
        (short)((value >> 8) + (value << 8));

    /// <summary>
    /// Changes the endiannes (byte order) of the <see cref="ushort"/> <paramref name="value"/>
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>The value in the "opposite" endiannes/byte-order</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ChangeByteOrder(this ushort value) =>
        (ushort)ChangeByteOrder((short)value);

    /// <summary>
    /// Changes the endiannes (byte order) of the <see cref="ulong"/> <paramref name="value"/>
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>The value in the "opposite" endiannes/byte-order</returns>
    public static ulong ChangeByteOrder(this ulong value) =>
        BinaryPrimitives.ReverseEndianness(value);

    /// <summary>
    /// Changes the endiannes (byte order) of the <see cref="uint"/> <paramref name="value"/>
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>The value in the "opposite" endiannes/byte-order</returns>
    public static uint ChangeByteOrder(this uint value) =>
        BinaryPrimitives.ReverseEndianness(value);

    /// <summary>
    /// Gets a <see cref="long"/> value stored in a <see cref="byte"/> array starting at a specific index 
    /// from network byte order to the host's byte order, converting endianness if needed.
    /// </summary>
    /// <remarks>This method simply calls <see cref="BitConverterBigEndian.ToInt64(byte[], int)"/>.</remarks>
    /// <param name="data">The byte array</param>
    /// <param name="start">The starting index in the array</param>
    /// <returns>The data in the array converted from network byte-order into the hosts order</returns>
    public static long ToLongFromNetworkByteOrder(this byte[] data, int start = 0)
        => BitConverterBigEndian.ToInt64(data, start);

    /// <summary>
    /// Gets an <see cref="int"/> value stored in a <see cref="byte"/> array starting at a specific index 
    /// from network byte order to the host's byte order, converting endianness if needed.
    /// </summary>
    /// <remarks>This method simply calls <see cref="BitConverterBigEndian.ToInt32(byte[], int)"/>.</remarks>
    /// <param name="data">The byte array</param>
    /// <param name="start">The starting index in the array</param>
    /// <returns>The data in the array converted from network byte-order into the hosts order</returns>
    public static int ToIntFromNetworkByteOrder(this byte[] data, int start = 0)
        => BitConverterBigEndian.ToInt32(data, start);

    /// <summary>
    /// Gets a <see cref="short"/> value stored in a <see cref="byte"/> array starting at a specific index 
    /// from network byte order to the host's byte order, converting endianness if needed.
    /// </summary>
    /// <remarks>This method simply calls <see cref="BitConverterBigEndian.ToInt16(byte[], int)"/>.</remarks>
    /// <param name="data">The byte array.</param>
    /// <param name="start">The starting index in the array.</param>
    /// <returns>The data in the array converted from network byte-order into the hosts order.</returns>
    public static int ToShortFromNetworkByteOrder(this byte[] data, int start = 0)
        => BitConverterBigEndian.ToInt16(data, start);

    /// <summary>
    /// Gets an <see cref="ushort"/> value stored in a <see cref="byte"/> array starting at a specific index 
    /// from network byte order to the host's byte order, converting endianness if needed.
    /// </summary>
    /// <remarks>This method simply calls <see cref="BitConverterBigEndian.ToUInt16(byte[], int)"/>.</remarks>
    /// <param name="data">The byte array.</param>
    /// <param name="start">The starting index in the array.</param>
    /// <returns>The data in the array converted from network byte-order into the hosts order.</returns>
    public static ushort ToUShortFromNetworkByteOrder(this byte[] data, int start = 0)
        => BitConverterBigEndian.ToUInt16(data, start);

    #region methods to ENSURE integral types are big or little-endian (e.g., only change byte order if needed)

    /// <summary>
    /// Ensures the value is stored in memory in big-endian format by swapping its byte-order on little-endian
    /// systems. On big-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterBigEndian.ToInt64(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 64-bit signed integer.</param>
    /// <returns>The number swapped to big-endian on little-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long BigEndian(this long v) =>
#if BIGENDIAN 
        v;
#else 
        v.ChangeByteOrder();
#endif

    /// <summary>
    /// Ensures the value is stored in memory in big-endian format by swapping its byte-order on little-endian
    /// systems. On big-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterBigEndian.ToUInt64(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 64-bit unsigned integer.</param>
    /// <returns>The number swapped to big-endian on little-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong BigEndian(this ulong v) =>
#if BIGENDIAN
        v;
#else 
        v.ChangeByteOrder();
#endif

    /// <summary>
    /// Ensures the value is stored in memory in big-endian format by swapping its byte-order on little-endian
    /// systems. On big-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterBigEndian.ToInt32(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 32-bit signed integer.</param>
    /// <returns>The number swapped to big-endian on little-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BigEndian(this int v) =>
#if BIGENDIAN
        v;
#else 
        v.ChangeByteOrder();
#endif

    /// <summary>
    /// Ensures the value is stored in memory in big-endian format by swapping its byte-order on little-endian
    /// systems. On big-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterBigEndian.ToUInt32(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 32-bit unsigned integer.</param>
    /// <returns>The number swapped to big-endian on little-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint BigEndian(this uint v) =>
#if BIGENDIAN
        v;
#else 
        v.ChangeByteOrder();
#endif

    /// <summary>
    /// Ensures the value is stored in memory in big-endian format by swapping its byte-order on little-endian
    /// systems. On big-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterBigEndian.ToInt16(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 16-bit signed integer.</param>
    /// <returns>The number swapped to big-endian on little-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short BigEndian(this short v) =>
#if BIGENDIAN
        v;
#else 
        v.ChangeByteOrder();
#endif

    /// <summary>
    /// Ensures the value is stored in memory in big-endian format by swapping its byte-order on little-endian
    /// systems. On big-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterBigEndian.ToUInt16(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 16-bit unsigned integer.</param>
    /// <returns>The number swapped to big-endian on little-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort BigEndian(this ushort v) =>
#if BIGENDIAN
        v;
#else 
        v.ChangeByteOrder();
#endif

    /// <summary>
    /// Ensures the value is stored in memory in little-endian format by swapping its byte-order on big-endian
    /// systems. On little-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterLittleEndian.ToInt64(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 64-bit signed integer.</param>
    /// <returns>The number swapped to little-endian on big-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long LittleEndian(this long v) =>
#if BIGENDIAN
        v.ChangeByteOrder();
#else
        v;
#endif

    /// <summary>
    /// Ensures the value is stored in memory in little-endian format by swapping its byte-order on big-endian
    /// systems. On little-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterLittleEndian.ToUInt64(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 64-bit unsigned integer.</param>
    /// <returns>The number swapped to little-endian on big-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong LittleEndian(this ulong v) =>
#if BIGENDIAN
        v.ChangeByteOrder();
#else
        v;
#endif

    /// <summary>
    /// Ensures the value is stored in memory in little-endian format by swapping its byte-order on big-endian
    /// systems. On little-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterLittleEndian.ToInt32(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 32-bit signed integer.</param>
    /// <returns>The number swapped to little-endian on big-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LittleEndian(this int v) =>
#if BIGENDIAN
        v.ChangeByteOrder();
#else
        v;
#endif

    /// <summary>
    /// Ensures the value is stored in memory in little-endian format by swapping its byte-order on big-endian
    /// systems. On little-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterLittleEndian.ToUInt64(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 32-bit unsigned integer.</param>
    /// <returns>The number swapped to little-endian on big-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint LittleEndian(this uint v) =>
#if BIGENDIAN
        v.ChangeByteOrder();
#else
        v;
#endif

    /// <summary>
    /// Ensures the value is stored in memory in little-endian format by swapping its byte-order on big-endian
    /// systems. On little-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterLittleEndian.ToInt16(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 16-bit signed integer.</param>
    /// <returns>The number swapped to little-endian on big-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short LittleEndian(this short v) =>
#if BIGENDIAN
        v.ChangeByteOrder();
#else
        v;
#endif

    /// <summary>
    /// Ensures the value is stored in memory in little-endian format by swapping its byte-order on big-endian
    /// systems. On little-endian systems it's an expensive no-op.
    /// </summary>
    /// <remarks>It's better to read a value in the correct byte-order up front with 
    /// <see cref="BitConverterLittleEndian.ToUInt16(byte[], int)"/> whenever possible.
    /// Since this method can't really detect the original endianness of the number,
    /// the caller must know the original endiannes and that it wasn't already byte-reordered.</remarks>
    /// <param name="v">A 16-bit unsigned integer.</param>
    /// <returns>The number swapped to little-endian on big-endian systems; otherwise the number itself</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort LittleEndian(this ushort v) =>
#if BIGENDIAN
        v.ChangeByteOrder();
#else
        v;
#endif

    #endregion

}
