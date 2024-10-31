using System.Runtime.CompilerServices;

namespace System.Numerics;


#if NETSTANDARD2_0

/// <summary>
/// Backport to NETSTANDARD 2.0 of some of the functionality in System.Numerics.BitOperations 
/// found in NETSTANDARD 2.1 and above.
/// </summary>
public static class BitOperations
{
    /// <summary>
    /// Rotates the specified value left by the specified number of bits.
    /// Similar in behavior to the x86 instruction ROL.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.
    /// Any value outside the range [0..31] is treated as congruent mod 32.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateLeft(uint value, int offset)
        => (value << offset) | (value >> (32 - offset));

    /// <summary>
    /// Rotates the specified value left by the specified number of bits.
    /// Similar in behavior to the x86 instruction ROL.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.
    /// Any value outside the range [0..63] is treated as congruent mod 64.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateLeft(ulong value, int offset)
        => (value << offset) | (value >> (64 - offset));

    /// <summary>
    /// Rotates the specified value left by the specified number of bits.
    /// Similar in behavior to the x86 instruction ROL.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.
    /// Any value outside the range [0..31] is treated as congruent mod 32 on a 32-bit process,
    /// and any value outside the range [0..63] is treated as congruent mod 64 on a 64-bit process.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint RotateLeft(nuint value, int offset) {
#if TARGET_64BIT
        return (nuint)RotateLeft((ulong)value, offset);
#else
        return (nuint)RotateLeft((uint)value, offset);
#endif
    }

    /// <summary>
    /// Rotates the specified value right by the specified number of bits.
    /// Similar in behavior to the x86 instruction ROR.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.
    /// Any value outside the range [0..31] is treated as congruent mod 32.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateRight(uint value, int offset)
        => (value >> offset) | (value << (32 - offset));

    /// <summary>
    /// Rotates the specified value right by the specified number of bits.
    /// Similar in behavior to the x86 instruction ROR.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.
    /// Any value outside the range [0..63] is treated as congruent mod 64.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateRight(ulong value, int offset)
        => (value >> offset) | (value << (64 - offset));

    /// <summary>
    /// Rotates the specified value right by the specified number of bits.
    /// Similar in behavior to the x86 instruction ROR.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.
    /// Any value outside the range [0..31] is treated as congruent mod 32 on a 32-bit process,
    /// and any value outside the range [0..63] is treated as congruent mod 64 on a 64-bit process.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint RotateRight(nuint value, int offset) {
#if TARGET_64BIT
        return (nuint)RotateRight((ulong)value, offset);
#else
        return (nuint)RotateRight((uint)value, offset);
#endif
    }
}

#endif
