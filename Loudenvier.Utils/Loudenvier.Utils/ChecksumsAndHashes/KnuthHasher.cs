using System;
using System.Collections.Generic;
using System.Text;

namespace Loudenvier.Utils.ChecksumsAndHashes;

public static class KnuthHasher
{
    /// <summary>Computes Knuth Hash over a string.</summary>
    /// <param name="str">The string being hashed.</param>
    /// <returns>The multiplicative Knuth Hash of the string.</returns>
    public static ulong ComputeKnuthHash(this string str) {
        ulong hashedValue = 3074457345618258791ul;
        for (int i = 0; i < str.Length; i++) {
            hashedValue += str[i];
            hashedValue *= 3074457345618258799ul;
        }
        return hashedValue;
    }
}
