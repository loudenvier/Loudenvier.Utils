using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Loudenvier.Utils;

/// <summary>
/// Provides functionality to parse formatted strings into a byte sequence.
/// Supports mixing ASCII string literals, binary, hexadecimal, and decimal byte representations.
/// </summary>
public static class ByteSequenceParser
{
    /// <summary>
    /// Parses an input string into an exact-sized array of bytes.
    /// Recognizes single-quoted ASCII strings (with escape sequences), binary numbers (e.g., b01010101),
    /// hexadecimal numbers (exactly 2 hex digits, e.g., FF), and decimal numbers (0-255, 0, 010, 100: two digit decimal numbers must be 
    /// prefixed with a zero).
    /// </summary>
    /// <remarks>
    /// Valid tokens are separated by whitespace. The parser is case-insensitive for binary prefixes and hexadecimal digits.
    /// Hexadecimal tokens must be exactly two characters long, while decimal tokens can be one or three digits (0-255),
    /// but not two digits (or they will be considered hexadecimal). 
    /// String literals can be enclosed in single or double quotes and support common escape sequences.
    /// </remarks>
    /// <param name="input">The raw string sequence to be parsed.</param>
    /// <returns>A byte array with the exact size of the parsed tokens.</returns>
    /// <exception cref="FormatException">Thrown when a string literal is not closed properly or a numeric token is invalid/out of bounds.</exception>
    public static byte[] Parse(string input) {
        if (string.IsNullOrWhiteSpace(input)) return Array.Empty<byte>();

        var bytes = new List<byte>(16); // optimizes for small sequences (the most common usecase)
        ReadOnlySpan<char> span = input.AsSpan();
        int i = 0;

        while (i < span.Length) {
            if (char.IsWhiteSpace(span[i])) {
                i++;
                continue;
            }

            // strings may be delimited by single or double quotes, and can contain escape sequences
            if (span[i] == '\'' || span[i] == '"') {
                char quoteChar = span[i]; // Remembers opening quote type
                i++; // Skip opening quote
                int startQuoteIndex = i;

                while (i < span.Length && span[i] != quoteChar) {
                    if (span[i] == '\\' && i + 1 < span.Length) {
                        i++; // Skip backslash
                        char escaped = span[i] switch {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            '\'' => '\'',
                            '"' => '"',
                            '\\' => '\\',
                            _ => span[i]
                        };

                        bytes.Add((byte)escaped);
                    } else {
                        bytes.Add((byte)span[i]);
                    }
                    i++;
                }

                if (i >= span.Length)
                    throw new FormatException($"Syntax error: String literal was not closed with {quoteChar}. Base: {span[startQuoteIndex..].ToString()}");

                i++; // Skip closing quote
            } else {
                // numeric tokens
                int start = i;
                while (i < span.Length && !char.IsWhiteSpace(span[i])) {
                    i++;
                }

                ReadOnlySpan<char> token = span[start..i];
                bytes.Add(ParseNumericToken(token));
            }
        }

        return [.. bytes];
    }

    /// <summary>
    /// Parses a single numeric token into a byte based on its formatting constraints.
    /// </summary>
    private static byte ParseNumericToken(ReadOnlySpan<char> token) {
        // Binário via prefixo 'b' ou 'B'
        if (token.Length > 1 && (token[0] == 'b' || token[0] == 'B')) {
#if NET6_0_OR_GREATER
            byte b = 0;
            for (int j = 1; j < token.Length; j++)
            {
                b <<= 1;
                if (token[j] == '1') b |= 1;
                else if (token[j] != '0') throw new FormatException("Invalid binary digit.");
            }
            return b;
#else
            return Convert.ToByte(token[1..].ToString(), 2);
#endif
        }

        // Hexadecimal (Exatamente 2 caracteres)
        if (token.Length == 2 && IsHexDigit(token[0]) && IsHexDigit(token[1])) {
#if NET6_0_OR_GREATER
            return byte.Parse(token, System.Globalization.NumberStyles.AllowHexSpecifier);
#else
            return Convert.ToByte(token.ToString(), 16);
#endif
        }

        // Fallback Decimal (0 a 255)
#if NET6_0_OR_GREATER
        if (byte.TryParse(token, out byte dec))
        {
            return dec;
        }
#else
        if (int.TryParse(token.ToString(), out int legacyDec) && legacyDec >= 0 && legacyDec <= 255) {
            return (byte)legacyDec;
        }
#endif

        throw new FormatException("Invalid token or out of byte bounds (0-255).");
    }

    /// <summary>
    /// Evaluates if a given character is a valid hexadecimal digit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsHexDigit(char c) {
        return (c >= '0' && c <= '9') ||
               (c >= 'a' && c <= 'f') ||
               (c >= 'A' && c <= 'F');
    }
}