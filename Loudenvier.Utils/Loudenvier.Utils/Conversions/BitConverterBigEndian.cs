using System.Buffers.Binary;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

// I'm putting it into the System namespace so that people using BitConverter may become
// aware of BitConverterBigEndian when using intellisense
namespace System {

    /// <summary>
    /// The <c>BitConverterBigEndian</c> class replicates the API surface of
    /// <see cref="BitConverter"/> but assumes the source array of bytes to 
    /// convert to one of the base data types, and the destination array of bytes
    /// for converting base data types into arrays are in big-endian byte-order,
    /// while still being agnostic to the host's byte-order.
    /// </summary>
    /// <remarks>
    /// It tries to be as performant as possible taking into account the data size and 
    /// the runtime's target byte-order when performing conversions. 
    /// As with <see cref="BitConverter" /> it takes into account the host's byte-order 
    /// when converting from/to big-endian. 
    /// It removes ambiguity when reading from sources or writing to destinations in 
    /// big-endian byte-order (file formats, network protocols). 
    /// It reads the data and assemble it in memory in the correct byte-order, 
    /// avoiding an extra step to reverse byte-order, which may increase performance 
    /// when dealing with big-endian data.
    /// </remarks>
    public static class BitConverterBigEndian
    {

        // This field indicates the "endianess" of the architecture.
        // The value is set to true if the architecture is
        // little endian; false if it is big endian.
        public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

        /// <summary>Returns the specified Boolean value as a byte array.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A byte array with length 1.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetBytes(bool value) {
            const byte one = 1;
            const byte zero = 0;
            byte[] r = [value ? one : zero];
            return r;
        }

        /// <summary>
        /// Returns the specified Unicode character value as an array of bytes in big-endian byte-order.
        /// </summary>
        /// <param name="value">The character to convert.</param>
        /// <remarks>Differently from <see cref="BitConverter.GetBytes(char)"/> the order of bytes in 
        /// the array returned by the <c>GetBytes</c> method will always be in big-endian byte-order 
        /// regardless of the computer's architecture endianness.</remarks>
        /// <returns>An array of bytes with length 2.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetBytes(char value) 
            => GetBytes((short)value);

        /// <summary>Returns the specified 16-bit signed integer value as an array bytes 
        /// in big-endian byte-order.</summary>
        /// <param name="value">The number to convert</param>
        /// <remarks>Differently from <see cref="BitConverter.GetBytes(short)"/> the order of bytes in 
        /// the array returned by the <c>GetBytes</c> method will always be in big-endian byte-order 
        /// regardless of the computer's architecture endianness.</remarks>
        /// <returns>An array of bytes with length 2.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static byte[] GetBytes(short value) {
            byte[] bytes = [(byte)(value >> 8), (byte)value];

#if BIGENDIAN
#else
            return bytes;
#endif
        }

        /// <summary>Returns the specified 32-bit signed integer value as an array of bytes 
        /// in big-endian byte-order.</summary>
        /// <param name="value">The number to convert</param>
        /// <remarks>Differently from <see cref="BitConverter.GetBytes(int)"/> the order of bytes in 
        /// the array returned by the <c>GetBytes</c> method will always be in big-endian byte-order 
        /// regardless of the computer's architecture endianness.</remarks>
        /// <returns>An array of bytes with length 4.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static byte[] GetBytes(int value) {
            byte[] bytes = [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];
            return bytes;
        }

        /// <summary>Returns the specified 64-bit signed integer value as an array of bytes 
        /// in big-endian byte-order.</summary>
        /// <param name="value">The number to convert</param>
        /// <remarks>Differently from <see cref="BitConverter.GetBytes(long)"/> the order of bytes in 
        /// the array returned by the <c>GetBytes</c> method will always be in big-endian byte-order 
        /// regardless of the computer's architecture endianness.</remarks>
        /// <returns>An array of bytes with length 8.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static byte[] GetBytes(long value) {
            byte[] bytes =
            [
                (byte)(value >> 56),
                (byte)(value >> 48),
                (byte)(value >> 40),
                (byte)(value >> 32),
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value,
            ];
            return bytes;
        }

        /// <summary>Returns the specified 16-bit unsigned integer value as an array bytes 
        /// in big-endian byte-order.</summary>
        /// <param name="value">The number to convert</param>
        /// <remarks>Differently from <see cref="BitConverter.GetBytes(ushort)"/> the order of bytes in 
        /// the array returned by the <c>GetBytes</c> method will always be in big-endian byte-order 
        /// regardless of the computer's architecture endianness.</remarks>
        /// <returns>An array of bytes with length 2.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetBytes(ushort value) {
            return GetBytes((short)value);
        }

        /// <summary>Returns the specified 32-bit unsigned integer value as an array of bytes 
        /// in bit-endian byte-order.</summary>
        /// <param name="value">The number to convert</param>
        /// <remarks>Differently from <see cref="BitConverter.GetBytes(uint)"/> the order of bytes in 
        /// the array returned by the <c>GetBytes</c> method will always be in big-endian byte-order 
        /// regardless of the computer's architecture endianness.</remarks>
        /// <returns>An array of bytes with length 4.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetBytes(uint value) {
            return GetBytes((int)value);
        }

        /// <summary>Returns the specified 64-bit unsigned integer value as an array of bytes 
        /// in big-endian byte-order.</summary>
        /// <param name="value">The number to convert</param>
        /// <remarks>Differently from <see cref="BitConverter.GetBytes(long)"/> the order of bytes in 
        /// the array returned by the <c>GetBytes</c> method will always be in big-endian byte-order 
        /// regardless of the computer's architecture endianness.</remarks>
        /// <returns>An array of bytes with length 8.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetBytes(ulong value) {
            return GetBytes((long)value);
        }

        /// <summary>Returns the specified single-precision floating point value as an array of bytes 
        /// in big-endian byte-order.</summary>
        /// <param name="value">The number to convert</param>
        /// <remarks>Differently from <see cref="BitConverter.GetBytes(float)"/> the order of bytes in 
        /// the array returned by the <c>GetBytes</c> method will always be in big-endian byte-order 
        /// regardless of the computer's architecture endianness.</remarks>
        /// <returns>An array of bytes with length 4.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static byte[] GetBytes(float value) {
            return GetBytes(*(int*)&value);
        }

        /// <summary>Returns the specified double-precision floating point value as an array of bytes 
        /// in big-endian byte-order.</summary>
        /// <param name="value">The number to convert</param>
        /// <remarks>Differently from <see cref="BitConverter.GetBytes(double)"/> the order of bytes in 
        /// the array returned by the <c>GetBytes</c> method will always be in big-endian byte-order 
        /// regardless of the computer's architecture endianness.</remarks>
        /// <returns>An array of bytes with length 8.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static byte[] GetBytes(double value) {
            return GetBytes(*(long*)&value);
        }

        /// <summary>
        /// Returns a Unicode character converted from two bytes at a specified position in a byte array
        /// stored in big-endian byte-order.
        /// </summary>
        /// <param name="value">An array that includes the two bytes to convert.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <remarks>Differently from <see cref="BitConverter.ToChar(byte[], int)"/> the order of 
        /// bytes in the array is always assumed to be in big-endian byte-order regardless 
        /// of the computer's architecture endianness.</remarks>
        /// <returns>The character formed by two bytes beginning at <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToChar(byte[] value, int startIndex = 0) {
            return (char)ToInt16(value, startIndex);
        }

        /// <summary>
        /// Returns a 16-bit signed integer converted from two bytes at a specified position in a byte array
        /// stored in big-endian byte-order.
        /// </summary>
        /// <param name="value">An array of bytes that includes the two bytes to convert.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <remarks>Differently from <see cref="BitConverter.ToInt16(byte[], int)"/> the order of 
        /// bytes in the array is always assumed to be in big-endian byte-order regardless 
        /// of the computer's architecture endianness.</remarks>
        /// <returns>A 16-bit signed integer formed by two bytes beginning at <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short ToInt16(byte[] value, int startIndex = 0) {
            fixed (byte* pbyte = &value[startIndex]) {
#if BIGENDIAN
#if NET5_0_OR_GREATER
                return Unsafe.ReadUnaligned<short>(pbyte);
#endif
                if (startIndex % 2 == 0)  // data is aligned 
                    return *((short*)pbyte);
                return (short)((*pbyte) | (*(pbyte + 1) << 8));
#endif
                // for shorts the bit-shifting conversion is faster than the commented code bellow
                // return BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<short>(pbyte));
                return (short)((*pbyte << 8) | (*(pbyte + 1)));
            }
        }

        /// <summary>
        /// Returns a 32-bit signed integer converted from two bytes at a specified position in a byte array
        /// stored in big-endian byte-order.
        /// </summary>
        /// <param name="value">An array of bytes that includes the four bytes to convert.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <remarks>Differently from <see cref="BitConverter.ToInt32(byte[], int)"/> the order of 
        /// bytes in the array is always assumed to be in big-endian byte-order regardless 
        /// of the computer's architecture endianness.</remarks>
        /// <returns>A 32-bit signed integer formed by four bytes beginning at <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int ToInt32(byte[] value, int startIndex = 0) {
            fixed (byte* pbyte = &value[startIndex]) {
#if BIGENDIAN
#if NET5_0_OR_GREATER
                return Unsafe.ReadUnaligned<int>(pbyte);
#endif
                if (startIndex % 4 == 0)  // data is aligned 
                    return *((int*)pbyte);
                return (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
#endif
                // for ints the bit-shifting conversion is faster than the commented code bellow
                // return BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(pbyte));
                return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
            }
        }

        /// <summary>
        /// Returns a 64-bit signed integer converted from eight bytes at a specified position in a byte array
        /// stored in big-endian byte-order.
        /// </summary>
        /// <param name="value">An array of bytes that includes the eight bytes to convert.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <remarks>Differently from <see cref="BitConverter.ToInt64(byte[], int)"/> the order of 
        /// bytes in the array is always assumed to be in big-endian byte-order regardless 
        /// of the computer's architecture endianness.</remarks>
        /// <returns>A 64-bit signed integer formed by eight bytes beginning at <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long ToInt64(byte[] value, int startIndex = 0) {
            fixed (byte* pbyte = &value[startIndex]) {
                //
                //for Int64 ReverseEndianness + ReadUnaligned performs faster than bit-shifting
                //
#if BIGENDIAN
                return Unsafe.ReadUnaligned<long>(pbyte);
                /* ReadUnaligned performs better
                if (startIndex % 8 == 0)  // data is aligned 
                    return *((long*)pbyte);
                int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                return (uint)i1 | ((long)i2 << 32);
                */
#endif
                return BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<long>(pbyte));
                /* ReadUnaligned performs better
                int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                return (uint)i2 | ((long)i1 << 32);
                */
            }
        }


        /// <summary>
        /// Returns a 16-bit unsigned integer converted from two bytes at a specified position in a byte array
        /// stored in big-endian byte-order.
        /// </summary>
        /// <param name="value">An array of bytes that includes the two bytes to convert.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <remarks>Differently from <see cref="BitConverter.ToUInt16(byte[], int)"/> the order of 
        /// bytes in the array is always assumed to be in big-endian byte-order regardless 
        /// of the computer's architecture endianness.</remarks>
        /// <returns>A 16-bit unsigned integer formed by two bytes beginning at <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(byte[] value, int startIndex = 0) 
            => (ushort)ToInt16(value, startIndex);

        /// <summary>
        /// Returns a 32-bit unsigned integer converted from two bytes at a specified position in a byte array
        /// stored in big-endian byte-order.
        /// </summary>
        /// <param name="value">An array of bytes that includes the four bytes to convert.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <remarks>Differently from <see cref="BitConverter.ToUInt32(byte[], int)"/> the order of 
        /// bytes in the array is always assumed to be in big-endian byte-order regardless 
        /// of the computer's architecture endianness.</remarks>
        /// <returns>A 32-bit unsigned integer formed by four bytes beginning at <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(byte[] value, int startIndex = 0) 
            => (uint)ToInt32(value, startIndex);

        /// <summary>
        /// Returns a 64-bit unsigned integer converted from eight bytes at a specified position in a byte array
        /// stored in big-endian byte-order.
        /// </summary>
        /// <param name="value">An array of bytes that includes the eight bytes to convert.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <remarks>Differently from <see cref="BitConverter.ToUInt64(byte[], int)"/> the order of 
        /// bytes in the array is always assumed to be in big-endian byte-order regardless 
        /// of the computer's architecture endianness.</remarks>
        /// <returns>A 64-bit unsigned integer formed by eight bytes beginning at <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(byte[] value, int startIndex = 0) 
            => (ulong)ToInt64(value, startIndex);

        /// <summary>Returns a Boolean value converted from the byte at a specified position in a byte array.</summary>
        /// <param name="value">A byte array.</param>
        /// <param name="index">The index of the byte within <paramref name="value"/> to convert.</param>
        /// <returns><c>true</c> if the byte at <paramref name="index"/> in <paramref name="value"/> is nonzero; otherwise, <c>false</c></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBoolean(byte[] value, int index = 0) 
            => value[index] != 0;


        /// <summary>Converts a read-only byte span to a Boolean value.</summary>
        /// <param name="value">A read-only span containing the bytes to convert.</param>
        /// <returns>A Boolean representing the converted bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBoolean(ReadOnlySpan<byte> value) 
            => value[0] != 0;

        /// <summary>
        /// Returns a single-precision floating point number converted from four bytes at a 
        /// specified position in a byte array stored in big-endian byte-order.
        /// </summary>
        /// <param name="value">An array of bytes that includes the four bytes to convert.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <remarks>Differently from <see cref="BitConverter.ToSingle(byte[], int)"/> the order of 
        /// bytes in the array is always assumed to be in big-endian byte-order regardless 
        /// of the computer's architecture endianness.</remarks>
        /// <returns>A single-precision floating point number formed by four bytes beginning at <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static float ToSingle(byte[] value, int startIndex) {
            int val = ToInt32(value, startIndex);
            return *(float*)&val;
        }

        /// <summary>
        /// Returns a double-precision floating point number converted from eight bytes at a 
        /// specified position in a byte array stored in big-endian byte-order.
        /// </summary>
        /// <param name="value">An array of bytes that includes the eight bytes to convert.</param>
        /// <param name="startIndex">The starting position within <paramref name="value"/>.</param>
        /// <remarks>Differently from <see cref="BitConverter.ToDouble(byte[], int)"/> the order of 
        /// bytes in the array is always assumed to be in big-endian byte-order regardless 
        /// of the computer's architecture endianness.</remarks>
        /// <returns>A double-precision floating point number formed by eight bytes beginning at <paramref name="startIndex"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static double ToDouble(byte[] value, int startIndex) {
            long val = ToInt64(value, startIndex);
            return *(double*)&val;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string ToString(byte[] value, int startIndex, int length) 
            => BitConverter.ToString(value, startIndex, length);

        public static string ToString(byte[] value) 
            => BitConverter.ToString(value);

        // Converts an array of bytes into a String.  
        public static string ToString(byte[] value, int startIndex) 
            => BitConverter.ToString(value, startIndex);

        /* These methods won't be converted

            [SecuritySafeCritical]
            public static unsafe long DoubleToInt64Bits(double value) {
                // If we're on a big endian machine, what should this method do?  You could argue for
                // either big endian or little endian, depending on whether you are writing to a file that
                // should be used by other programs on that processor, or for compatibility across multiple
                // formats.  Because this is ambiguous, we're excluding this from the Portable Library & Win8 Profile.
                // If we ever run on big endian machines, produce two versions where endianness is specified.
                Contract.Assert(IsLittleEndian, "This method is implemented assuming little endian with an ambiguous spec.");
                return *((long*)&value);
            }

            [SecuritySafeCritical]
            public static unsafe double Int64BitsToDouble(long value) {
                // If we're on a big endian machine, what should this method do?  You could argue for
                // either big endian or little endian, depending on whether you are writing to a file that
                // should be used by other programs on that processor, or for compatibility across multiple
                // formats.  Because this is ambiguous, we're excluding this from the Portable Library & Win8 Profile.
                // If we ever run on big endian machines, produce two versions where endianness is specified.
                Contract.Assert(IsLittleEndian, "This method is implemented assuming little endian with an ambiguous spec.");
                return *((double*)&value);
            }

        }
        */
    }

    /* The original BitConverterBigEndian was replaced with a better performing version with almost the same API surface as DOTNET's BitConverter
     *
    /// <summary>
    /// A simplified, big-endian version of <see cref="BitConverter"/>. If the host is big-endian 
    /// it will simply forward calls to <see cref="BitConverter"/> itself.
    /// </summary>
    public static class BitConverterBigEndian
    {
        public static long ToInt64(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToInt64(value, startIndex);
#else
            BinaryPrimitives.ReverseEndianness(BitConverter.ToInt64(value, startIndex)); //.ChangeByteOrder();
#endif

        public static ulong ToUInt64(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToUInt64(value, startIndex);
#else
            BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt64(value, startIndex)); //.ChangeByteOrder();
#endif

        public static int ToInt32(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToInt32(value, startIndex);
#else
            BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(value, startIndex)); //.ChangeByteOrder();
#endif

        public static uint ToUInt32(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToUInt32(value, startIndex);
#else
            BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(value, startIndex)); //.ChangeByteOrder();
#endif

        public static int ToInt16(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToInt16(value, startIndex);
#else
            BinaryPrimitives.ReverseEndianness(BitConverter.ToInt16(value, startIndex)); //.ChangeByteOrder();
#endif

        public static ushort ToUInt16(byte[] value, int startIndex) =>
#if BIGENDIAN
            BitConverter.ToUInt16(value, startIndex);
#else
            BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt16(value, startIndex)); //.ChangeByteOrder();
#endif
    }
    */
}

