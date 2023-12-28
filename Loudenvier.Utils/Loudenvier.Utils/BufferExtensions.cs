using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Loudenvier.Utils
{
    public static class BufferExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Combine(params byte[][] arrays) => arrays.Combine();


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Combine(this IEnumerable<byte[]> arrays) {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays) {
                Array.Copy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }
    }
}
