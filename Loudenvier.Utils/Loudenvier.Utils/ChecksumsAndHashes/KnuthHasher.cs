using System;
using System.Runtime.CompilerServices;

namespace Loudenvier.Utils.ChecksumsAndHashes;

/// <summary>
/// Provides high-performance extension methods for computing non-cryptographic hashes using Knuth's multiplicative hashing algorithm.
/// </summary>
public static class KnuthHasher
{
    private const ulong InitialSeed = 3074457345618258791UL;
    private const ulong Multiplier = 3074457345618258799UL;

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
    public static ulong ComputeKnuthHash(this string? str, bool applyBitMixing = false) {
        if (string.IsNullOrEmpty(str))
            return 0UL;

#if NETCOREAPP || NET5_0_OR_GREATER
        return ComputeKnuthHash(str.AsSpan(), applyBitMixing);
#else
        ulong hashedValue = InitialSeed;
        for (int i = 0; i < str!.Length; i++) {
            hashedValue += str[i];
            hashedValue *= Multiplier;
        }

        return applyBitMixing ? MixBits(hashedValue) : hashedValue;
#endif
    }

#if NETCOREAPP || NET5_0_OR_GREATER
    /// <summary>
    /// Computes a 64-bit non-cryptographic multiplicative hash over the specified character span.
    /// </summary>
    /// <param name="span">The read-only span of characters to be hashed.</param>
    /// <param name="applyBitMixing">
    /// When <see langword="true"/>, applies a SplitMix64 avalanching step to evenly distribute entropy from higher-order bits into lower-order bits. 
    /// Recommended when the hash will undergo modulo reduction (e.g., hash tables or fixed-digit ranges) to avoid clustering. 
    /// Defaults to <see langword="false"/> to preserve legacy behavior.
    /// </param>
    /// <returns>
    /// A 64-bit unsigned integer (<see cref="ulong"/>) representing the hash value, or <c>0UL</c> if <paramref name="span"/> is empty.
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
    public static ulong ComputeKnuthHash(this ReadOnlySpan<char> span, bool applyBitMixing = false) 
    {
        if (span.IsEmpty)
            return 0UL;

        ulong hashedValue = InitialSeed;
        foreach (ref readonly char c in span) 
        {
            hashedValue += c;
            hashedValue *= Multiplier;
        }

        return applyBitMixing ? MixBits(hashedValue) : hashedValue;
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MixBits(ulong hashedValue) {
        hashedValue ^= hashedValue >> 30;
        hashedValue *= 0xbf58476d1ce4e5b9UL;
        hashedValue ^= hashedValue >> 27;
        hashedValue *= 0x94d049bb133111ebUL;
        hashedValue ^= hashedValue >> 31;

        return hashedValue;
    }
}