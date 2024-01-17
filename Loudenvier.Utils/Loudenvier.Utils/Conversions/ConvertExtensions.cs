using System;
using System.Runtime.CompilerServices;

namespace Loudenvier.Utils
{
    public static class ConvertExtensions
    {
        /// <summary>
        /// Converts the <see cref="object"/> passed in <paramref name="o"/> into the type specified by <typeparamref name="T"/>,
        /// taking into account the possibilities of <see cref="null"/>, <see cref="DBNull"/> or <see cref="string.Empty"/> 
        /// (somewhat useful in data reading contexts)
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to convert to</typeparam>
        /// <param name="o">The object to convert</param>
        /// <returns>The <paramref name="o"/> converted to type <typeparamref name="T"/></returns>
        public static T? ConvertTo<T>(this object? o, T? defaultValue = default) {
            if (o is null || o == DBNull.Value || string.Empty.Equals(o))
                return defaultValue;
            Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(o, t);
        }

        /// <summary>
        /// Converts the <see cref="object"/> passed in <paramref name="o"/> into the type specified by <typeparamref name="T"/>,
        /// taking into account the possibilities of <see cref="null"/>, <see cref="DBNull"/> or <see cref="string.Empty"/> 
        /// (somewhat useful in data reading contexts)
        /// </summary>
        /// <remarks>This is a shorthand for <see cref="ConvertTo{T}(object, T)"/></remarks>
        /// <typeparam name="T">The <see cref="Type"/> to convert to</typeparam>
        /// <param name="o">The object to convert</param>
        /// <returns>The <paramref name="o"/> converted to type <typeparamref name="T"/></returns>
        public static T? To<T>(this object o, T? defaultValue = default) => ConvertTo(o, defaultValue);


        /// <summary>Converts an <paramref name="array"/> of 8-bit unsigned integers to its equivalent 
        /// string representation that is encoded with uppercase hex characters.</summary>
        /// <remarks>If targeting NET Standard/Framework this will use a very fast, unsafe, hand-coded
        /// conversion (source: https://stackoverflow.com/a/71904920/285678). If on DOTNET CORE 5.0 or higher
        /// it will call the even faster <c>Convert.ToHexString(byte[])</c></remarks>
        /// <param name="array">An array of 8-bit unsigned integers</param>
        /// <returns>The string representation in hex of the elements in the <paramref name="array"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHexString(this byte[] array) =>
#if NET5_0_OR_GREATER
            Convert.ToHexString(array);
#else
            ByteArrayToHexViaLookup32UnsafeDirect(array);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static string ByteArrayToHexViaLookup32UnsafeDirect(byte[] source) {
            var result = new string((char)0, source.Length * 2);
            unsafe {
                fixed (uint* lookupP = _lookup32Unsafe)
                fixed (byte* bytesP = source)
                fixed (char* resultP = result) {
                    uint* resultP2 = (uint*)resultP;
                    for (int i = 0; i < source.Length; i++) {
                        resultP2[i] = lookupP[bytesP[i]];
                    }
                }
            }
            return result;
        }
        static readonly uint[] _lookup32Unsafe = CreateLookup32Unsafe();
        static uint[] CreateLookup32Unsafe() {
            var result = new uint[256];
            for (int i = 0; i < 256; i++) {
                string s = i.ToString("X2");
                if (BitConverter.IsLittleEndian)
                    result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
                else
                    result[i] = ((uint)s[1]) + ((uint)s[0] << 16);
            }
            return result;
        }

    }
}
