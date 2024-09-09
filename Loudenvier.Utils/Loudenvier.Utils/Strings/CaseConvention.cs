using System;
using System.Linq;
using System.Text;

namespace Loudenvier.Utils;

/// <summary>
/// Aggregate various possible (programming) case conventions (camel case, pascal case, etc.) including some exoteric ones.
/// </summary>
public enum CaseConvention { 
    /// <summary>Keep the case intact when processing</summary>
    Original, 
    /// <summary>Lowercases all characaters and remove common separators ('_', '-') (ex.: Flat-Case => flatcase)</summary>
    Flat, 
    /// <summary>The beginning char and every first char after a separator is in uppercase (ex.: pascal-case => PascalCase)</summary>
    Pascal, 
    /// <summary>The first char is lowercase and every other first char after a separator is in uppercase 
    /// (ex.: Camel_case => camelCase)</summary>
    Camel, 
    /// <summary>Every uppercase characater is converted to lowercase and the first in a sequence is prepended with '-'
    /// (ex.: KebabCase => kebab-case)</summary>
    Kebab,
    /// <summary>Every uppercase characater is converted to lowercase and the first in a sequence is prepended with '_'
    /// (ex.: SnakeCase => snake_case)</summary>
    Snake, 
    /// <summary>
    /// Uppercases all characters and prepends every original first upper case in a sequence with '_'
    /// (ex.: ConstantCase => CONSTANT_CASE)
    /// </summary>
    Constant,
    /// <summary>
    /// Uppercases all characters and prepends every original first upper case in a sequence with '-'
    /// (ex.: CobolCase => COBOL-CASE)
    /// </summary>
    Cobol,
}

public static class CaseConventionExtensions {

    const char snake = '_';
    const char kebab = '-';
    static readonly char[] separators = [snake, kebab];
    
    public static string ApplyCase(this CaseConvention cmdCase, string s) {
        return cmdCase switch {
            CaseConvention.Original => s,
            CaseConvention.Flat => s.StripChars(separators).ToLowerInvariant(),
            CaseConvention.Pascal => s.ToPascalCase(),
            CaseConvention.Camel => s.ToCamelCase(),
            CaseConvention.Kebab => s.ToKebabCase(),
            CaseConvention.Snake => s.ToSnakeCase(),
            CaseConvention.Constant => s.ToConstantCase(),
            CaseConvention.Cobol => s.ToCobolCase(),
            _ => throw new ArgumentOutOfRangeException($"This CommandCase is not supported: {cmdCase}", nameof(cmdCase)),
        };
    }

    static string ToCase(this string s, Func<char, char> firstCase, Func<char, char> middleCase) {
        if (string.IsNullOrEmpty(s)) return s;
        var parts = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        parts[0] = $"{firstCase(parts[0][0])}{parts[0][1..]}";
        if (parts.Length == 1)
            return parts[0];
        var combined = parts.Skip(1).Select(p => $"{middleCase(p[0])}{p[1..]}");
        return $"{parts[0]}{string.Concat(combined)}";
    }

    static string ToCaseWithSeparator(this string s, char separator = '-') {
        var sb = new StringBuilder(s.Length);
        bool? lastIsUpper = null;
        bool lastIsSep = false;
        foreach (var ch in s) {
            if (lastIsUpper == false && !lastIsSep && char.IsUpper(ch))
                sb.Append(separator);
            sb.Append(char.ToLowerInvariant(ch));
            lastIsUpper = char.IsUpper(ch);
            lastIsSep = ch == separator;
        }
        return sb.ToString();
    }

    static string ToPascalCase(this string s)
        => ToCase(s, char.ToUpperInvariant, char.ToUpperInvariant);
    static string ToCamelCase(this string s)
        => ToCase(s, char.ToLowerInvariant, char.ToUpperInvariant);
    static string ToKebabCase(this string s) => ToCaseWithSeparator(s);
    static string ToSnakeCase(this string s) => ToCaseWithSeparator(s, '_');
    static string ToConstantCase(this string s) => ToSnakeCase(s).ToUpperInvariant();
    static string ToCobolCase(this string s) => ToKebabCase(s).ToUpperInvariant();

}