using System.Runtime.CompilerServices;

namespace Loudenvier.Utils.ChecksumsAndHashes;

/// <summary>
/// Provides high-performance extension methods for computing non-cryptographic hashes using Knuth's multiplicative hashing algorithm.
/// </summary>
public static class KnuthHasher
{
    /// <summary>
    /// Computes a 64-bit non-cryptographic multiplicative hash over the specified string.
    /// </summary>
    /// <param name="str">The string instance to be hashed.</param>
    /// <param name="applyBitMixing">
    /// When <see langword="true"/>, applies a SplitMix64 avalanching step to evenly distribute entropy from higher-order bits into lower-order bits. 
    /// Recommended when the hash will undergo modulo reduction (e.g., hash tables or fixed-digit ranges) to avoid clustering. 
    /// Defaults to <see langword="false"/> to preserve legacy behavior.
    /// </param>
    /// <returns>
    /// A 64-bit unsigned integer (<see cref="ulong"/>) representing the hash value, or <c>0UL</c> if <paramref name="str"/> is <see langword="null"/> or empty (<c>""</c>).
    /// </returns>
    /// <remarks>
    /// This method implements a string-accumulator variant of Donald Knuth's Multiplicative Hashing Algorithm, inspired by 
    /// <i>The Art of Computer Programming, Volume 3: Sorting and Searching</i> (Section 6.4). 
    /// <para/>
    /// It utilizes specific 64-bit constants to distribute entropy across the unsigned 64-bit integer space 
    /// via modular arithmetic wrapping.
    /// <para/>
    /// <b>Security Warning:</b> This is a non-cryptographic hash function intended strictly for data distribution, 
    /// hashing data structures, and performance-critical lookups. It is not collision-resistant and must not be used 
    /// for cryptographic or security purposes.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ComputeKnuthHash(this string str, bool applyBitMixing = false) {
        if (string.IsNullOrEmpty(str))
            return 0ul;

        ulong hashedValue = 3074457345618258791ul;
        for (int i = 0; i < str.Length; i++) {
            hashedValue += str[i];
            hashedValue *= 3074457345618258799ul;
        }

        if (applyBitMixing) {
            hashedValue ^= hashedValue >> 30;
            hashedValue *= 0xbf58476d1ce4e5b9ul;
            hashedValue ^= hashedValue >> 27;
            hashedValue *= 0x94d049bb133111ebul;
            hashedValue ^= hashedValue >> 31;
        }

        return hashedValue;
    }
}