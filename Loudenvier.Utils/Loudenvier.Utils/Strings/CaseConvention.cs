using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// <summary>Uppercases all characaters and remove common separators ('_', '-') (ex.: Upper-Case => UPPERCASE)</summary>
    Upper,
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
    /// <summary>The first char is lowercase and every other first char after a separator is in uppercase with 
    /// a '_' prepended (ex.: Camel_case => camel-Case)</summary>
    CamelSnake,
    /// <summary>The beginning char is in uppercase and every first char after a separator 
    /// is in uppercase prepended with '_' (ex.: pascalCase => Pascal-Case)</summary>
    PascalSnake,
    /// <summary>The beginning char is in uppercase and every first char after a separator 
    /// is in uppercase prepended with '-' (ex.: HTTP-HeaderCase => HTTP-Header-Case)</summary>
    HTTPHeader,
}

public static class CaseConventionExtensions {

    const char snake = '_';
    const char kebab = '-';
    static readonly char[] separators = [snake, kebab];
    
    public static string ApplyCase(this CaseConvention cmdCase, string s) {
        return cmdCase switch {
            CaseConvention.Original => s,
            CaseConvention.Flat => s.StripChars(separators).ToLowerInvariant(),
            CaseConvention.Upper => s.StripChars(separators).ToUpperInvariant(),
            CaseConvention.Pascal => s.ToPascalCase(),
            CaseConvention.Camel => s.ToCamelCase(),
            CaseConvention.Kebab => s.ToKebabCase(),
            CaseConvention.Snake => s.ToSnakeCase(),
            CaseConvention.Constant => s.ToConstantCase(),
            CaseConvention.Cobol => s.ToCobolCase(),
            CaseConvention.CamelSnake => s.ToCamelSnake(),
            CaseConvention.PascalSnake => s.ToPascalSnake(),
            CaseConvention.HTTPHeader => s.ToHTTPHeader(),
            _ => throw new ArgumentOutOfRangeException(nameof(cmdCase), $"This CommandCase is not supported: {cmdCase}"),
        };
    }

    // fast way to apply a "char" transform to the first char in a string
    [MethodImpl(MethodImplOptions.AggressiveInlining)]  
    static string ApplyFistCharTransform(string s, Func<char, char> transform) {
        var arr = s.ToCharArray();
        arr[0] = transform(arr[0]);
        return new string(arr);
    }

    static string ToCase(this string s, Func<char, char> firstCase, Func<char, char> middleCase) {
        if (string.IsNullOrEmpty(s)) return s;
        var parts = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        parts[0] = ApplyFistCharTransform(parts[0], firstCase); // $"{firstCase(parts[0][0])}{parts[0][1..]}";
        if (parts.Length == 1)
            return parts[0];
        var combined = parts.Skip(1).Select(p => ApplyFistCharTransform(p, middleCase)); //$"{middleCase(p[0])}{p[1..]}");
        return $"{parts[0]}{string.Concat(combined)}";
    }

    static string ToCaseWithSeparator(this string s, Func<string, string> transform,char separator = '-') {
        var parts = s.SplitOnUpperOrSeparator(keepSeparators: true, separator);
        var sb = new StringBuilder();
        bool lastIsSeparator = true;
        foreach (var part in parts) {
            var needsSep = !lastIsSeparator && part[0].NotIn(separators);
            sb.Append(needsSep ? $"{separator}{transform(part)}" : transform(part));
            lastIsSeparator = part.Contains(separator);
        }
        return sb.ToString();
        /*var sb = new StringBuilder(s.Length);
        bool? lastIsUpper = null;
        bool lastIsSep = false;
        foreach (var ch in s) {
            if (lastIsUpper == false && !lastIsSep && ch != separator && char.IsUpper(ch))
                sb.Append($"{separator}{transform(ch)}");
            else 
                sb.Append(lastIsUpper == null ? transform(ch) : ch);
            lastIsUpper = char.IsUpper(ch);
            lastIsSep = ch == separator;
        }
        return sb.ToString();*/
    }

    static string ToPascalCase(this string s)
        => ToCase(s, char.ToUpperInvariant, char.ToUpperInvariant);
    static string ToCamelCase(this string s)
        => ToCase(s, char.ToLowerInvariant, char.ToUpperInvariant);

    static string ToLower(string s) => s.ToLowerInvariant();
    static string ToKebabCase(this string s) => ToCaseWithSeparator(s, ToLower);
    static string ToSnakeCase(this string s) => ToCaseWithSeparator(s, ToLower, '_');
    static string ToConstantCase(this string s) => ToSnakeCase(s).ToUpperInvariant();
    static string ToCobolCase(this string s) => ToKebabCase(s).ToUpperInvariant();
    static string ToCamelSnake(this string s) => 
        ApplyFistCharTransform(
            ToCaseWithSeparator(s, StringExtensions.Capitalize, '_'), 
            char.ToLowerInvariant);
    static string ToPascalSnake(this string s) => ToCaseWithSeparator(s, StringExtensions.Capitalize, '_');
    static string ToHTTPHeader(this string s) => ToCaseWithSeparator(s, StringExtensions.Capitalize, '-');

}