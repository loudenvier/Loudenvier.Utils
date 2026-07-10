using System;

namespace Loudenvier.Utils.ChecksumsAndHashes;

/// <summary>
/// Provides high-performance extension method for computing non-cryptographic hashes using Knuth's multiplicative hashing algorithm.
/// </summary>
public static class KnuthHasher
{
    /// <summary>
    /// Computes a 64-bit non-cryptographic multiplicative hash over the specified string.
    /// </summary>
    /// <param name="str">The string instance to be hashed.</param>
    /// <returns>A 64-bit unsigned integer (<see cref="ulong"/>) representing the hash value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="str"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method implements Donald Knuth's Multiplicative Hashing Algorithm, as detailed in 
    /// <i>The Art of Computer Programming, Volume 3: Sorting and Searching</i> (Section 6.4). 
    /// <para/>
    /// It utilizes specific 64-bit constants to distribute entropy across the unsigned 64-bit integer space 
    /// via modular arithmetic wrapping.
    /// <para/>
    /// <b>Security Warning:</b> This is a non-cryptographic hash function intended strictly for data distribution, 
    /// hashing data structures, and performance-critical lookups. It is not collision-resistant and must not be used 
    /// for cryptographic or security purposes.
    /// </remarks>
    public static ulong ComputeKnuthHash(this string str) {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(str, nameof(str));
#else
        if (str == null) throw new ArgumentNullException(nameof(str));
#endif

        ulong hashedValue = 3074457345618258791ul;
        for (int i = 0; i < str.Length; i++) {
            hashedValue += str[i];
            hashedValue *= 3074457345618258799ul;
        }
        return hashedValue;
    }
}