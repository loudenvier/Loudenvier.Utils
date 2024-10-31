using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Loudenvier.Utils;

/// <summary>Implements a 64-bit CRC hash algorithm with a given polynomial and seed.</summary>
/// <remarks>For ISO 3309 compliant 64-bit CRC's use<see cref="Crc64Iso"/>.</remarks>
public class Crc64 : HashAlgorithm
{
    public const ulong DefaultSeed = 0x0;

    readonly ulong[] table;

    readonly ulong seed;
    ulong hash;

    /// <summary>
    /// Instantiates a new <see cref="Crc64"/> object with the given <paramref name="polynomial"/> and the default seed (0x0)
    /// </summary>
    /// <param name="polynomial">The polynomial for CRC64 calculations</param>
    public Crc64(ulong polynomial): this(polynomial, DefaultSeed) { }

    /// <summary>
    /// Instantiates a new <see cref="Crc64"/> object with the given <paramref name="polynomial"/> and the given <paramref name="seed"/>
    /// </summary>
    /// <param name="polynomial">The polynomial to use for CRC64 calculations</param>
    /// <param name="seed">The seed to use for CRC64 calculations</param>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public Crc64(ulong polynomial, ulong seed) {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("Not supported on Big Endian processors");

        table = InitializeTable(polynomial);
        this.seed = hash = seed;
    }

    public override void Initialize() => hash = seed;

    protected override void HashCore(byte[] array, int ibStart, int cbSize) 
        => CalculateHash(hash, table, array, ibStart, cbSize);
    
    protected override byte[] HashFinal() {
        var hashBuffer = UInt64ToBigEndianBytes(hash);
        HashValue = hashBuffer;
        return hashBuffer;
    }

    public override int HashSize => 64;

    /// <summary>
    /// Calculates a CRC64 hash using 
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="table"></param>
    /// <param name="buffer"></param>
    /// <param name="start"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    protected static ulong CalculateHash(ulong seed, ulong[] table, byte[] buffer, int start, int size) {
        var hash = seed;
        for (var i = start; i < start + size; i++) {
            unchecked {
                hash = (hash >> 8) ^ table[(buffer[i] ^ hash) & 0xff];
            }
        }
        return hash;
    }

    static byte[] UInt64ToBigEndianBytes(ulong value) {
        var result = BitConverter.GetBytes(value);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(result);

        return result;
    }

    static ulong[] InitializeTable(ulong polynomial) 
        => polynomial == Crc64Iso.Iso3309Polynomial 
            ? Crc64Iso.Table.Value
            : CreateTable(polynomial);

    protected static ulong[] CreateTable(ulong polynomial) {
        var createTable = new ulong[256];
        for (var i = 0; i < 256; ++i) {
            var entry = (ulong)i;
            for (var j = 0; j < 8; ++j)
                if ((entry & 1) == 1)
                    entry = (entry >> 1) ^ polynomial;
                else
                    entry = entry >> 1;
            createTable[i] = entry;
        }
        return createTable;
    }
}

/// <summary>
/// Implements a ISO 3309 complient CRC64 checksum. Also allows custom seeds to be used.
/// </summary>
public class Crc64Iso : Crc64
{
    internal static Lazy<ulong[]> Table = new(() => CreateTable(Iso3309Polynomial));

    public const ulong Iso3309Polynomial = 0xD800000000000000;

    /// <summary>Instantiates a new <see cref="Crc64Iso"/> object the default seed (0x0)</summary>
    public Crc64Iso(): base(Iso3309Polynomial) { }
    /// <summary>Instantiates a new <see cref="Crc64Iso"/> object the given <paramref name="seed"/></summary>
    public Crc64Iso(ulong seed) : base(Iso3309Polynomial, seed) { }

    public static ulong Compute(byte[] buffer) => Compute(DefaultSeed, buffer);

    public static ulong Compute(ulong seed, byte[] buffer) 
        => CalculateHash(seed, Table.Value, buffer, 0, buffer.Length);
    
}
