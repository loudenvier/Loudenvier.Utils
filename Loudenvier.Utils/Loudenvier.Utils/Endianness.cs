using System;

namespace Loudenvier.Utils
{
    public static class Endianness
    {
#if BIGENDIAN
        public static readonly bool IsLittleEndian;
#else
        public static readonly bool IsLittleEndian = true;
#endif
        public static readonly bool IsBigEndian = !IsLittleEndian;

        /// <summary>
        /// Changes the endiannes (byte order) of the <see cref="long"/> <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The value in the "opposite" endiannes/byte-order</returns>
        public static long ChangeByteOrder(this long value) =>
            (((long)ChangeByteOrder((int)value) & 0xFFFFFFFF) << 32) |
            ((long)ChangeByteOrder((int)(value >> 32)) & 0xFFFFFFFF);

        /// <summary>
        /// Changes the endiannes (byte order) of the <see cref="int"/> <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The value in the "opposite" endiannes/byte-order</returns>
        public static int ChangeByteOrder(this int value) =>
            (((int)ChangeByteOrder((short)value) & 0xFFFF) << 16) |
            ((int)ChangeByteOrder((short)(value >> 16)) & 0xFFFF);

        /// <summary>
        /// Changes the endiannes (byte order) of the <see cref="short"/> <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The value in the "opposite" endiannes/byte-order</returns>
        public static short ChangeByteOrder(this short value) =>
            (short)(
                (((int)value & 0xFF) << 8) |
                (int)((value >> 8) & 0xFF)
            );

        /// <summary>
        /// Changes the endiannes (byte order) of the <see cref="ushort"/> <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The value in the "opposite" endiannes/byte-order</returns>
        public static ushort ChangeByteOrder(this ushort value) =>
            (ushort)ChangeByteOrder((short)value);

        /// <summary>
        /// Changes the endiannes (byte order) of the <see cref="ulong"/> <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The value in the "opposite" endiannes/byte-order</returns>
        public static ulong ChangeByteOrder(this ulong value) =>
            (ulong)ChangeByteOrder((long)value);

        /// <summary>
        /// Changes the endiannes (byte order) of the <see cref="uint"/> <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The value in the "opposite" endiannes/byte-order</returns>
        public static uint ChangeByteOrder(this uint value) =>
            (uint)ChangeByteOrder((int)value);

        /// <summary>
        /// Gets a <see cref="long"/> value stored in a <see cref="byte"/> array starting at a specific index 
        /// from network byte order to the host's byte order, converting endianness if needed.
        /// </summary>
        /// <param name="data">The byte array</param>
        /// <param name="start">The starting index in the array</param>
        /// <returns>The data in the array converted from network byte-order into the hosts order</returns>
        public static long ToLongFromNetworkByteOrder(this byte[] data, int start = 0)
            => BitConverterBigEndian.ToInt64(data, start);

        /// <summary>
        /// Gets an <see cref="int"/> value stored in a <see cref="byte"/> array starting at a specific index 
        /// from network byte order to the host's byte order, converting endianness if needed.
        /// </summary>
        /// <param name="data">The byte array</param>
        /// <param name="start">The starting index in the array</param>
        /// <returns>The data in the array converted from network byte-order into the hosts order</returns>
        public static int ToIntFromNetworkByteOrder(this byte[] data, int start = 0)
            => BitConverterBigEndian.ToInt32(data, start);

        /// <summary>
        /// Gets a <see cref="short"/> value stored in a <see cref="byte"/> array starting at a specific index 
        /// from network byte order to the host's byte order, converting endianness if needed.
        /// </summary>
        /// <param name="data">The byte array</param>
        /// <param name="start">The starting index in the array</param>
        /// <returns>The data in the array converted from network byte-order into the hosts order</returns>
        public static int ToShortFromNetworkByteOrder(this byte[] data, int start = 0)
            => BitConverterBigEndian.ToInt16(data, start);

        /// <summary>
        /// Gets an <see cref="ushort"/> value stored in a <see cref="byte"/> array starting at a specific index 
        /// from network byte order to the host's byte order, converting endianness if needed.
        /// </summary>
        /// <param name="data">The byte array</param>
        /// <param name="start">The starting index in the array</param>
        /// <returns>The data in the array converted from network byte-order into the hosts order</returns>
        public static ushort ToUShortFromNetworkByteOrder(this byte[] data, int start = 0)
            => BitConverterBigEndian.ToUInt16(data, start);
    }

    /// <summary>
    /// A simplified, big-endian version of <see cref="BitConverter"/>. If the host is big-endian 
    /// it will only forward calls to <see cref="BitConverter"/> itself.
    /// </summary>
    public static class BitConverterBigEndian
    {
        public static long ToInt64(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToInt64(value, startIndex);
#else
            BitConverter.ToInt64(value, startIndex).ChangeByteOrder();
#endif

        public static ulong ToUInt64(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToUInt64(value, startIndex);
#else
    BitConverter.ToUInt64(value, startIndex).ChangeByteOrder();
#endif

        public static int ToInt32(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToInt32(value, startIndex);
#else
            BitConverter.ToInt32(value, startIndex).ChangeByteOrder();
#endif

        public static uint ToUInt32(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToUInt32(value, startIndex);
#else
            BitConverter.ToUInt32(value, startIndex).ChangeByteOrder();
#endif

        public static int ToInt16(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToInt16(value, startIndex);
#else
            BitConverter.ToInt16(value, startIndex).ChangeByteOrder();
#endif

        public static ushort ToUInt16(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToUInt16(value, startIndex);
#else
            BitConverter.ToUInt16(value, startIndex).ChangeByteOrder();
#endif
    }
}
