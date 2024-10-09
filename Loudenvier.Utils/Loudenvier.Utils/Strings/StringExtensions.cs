using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Text;

namespace Loudenvier.Utils

{
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a new string in which the <paramref name="prefix"/> is removed, using the supplied string <paramref name="comparison"/>.
        /// </summary>
        /// <remarks>If no prefix is found (or the string is null) it will return the actual unadulterated string</remarks>
        /// <param name="s">The string to (possibly) remove the prefix</param>
        /// <param name="prefix">The prefix to remove</param>
        /// <param name="comparison">An option <see cref="StringComparison"/> (defaults to <see cref="StringComparison.Ordinal"/>)</param>
        /// <returns>A new string with the prefix removed if it was found on the original</returns>
        public static string? TrimStart(this string s, string prefix, StringComparison comparison = StringComparison.Ordinal) 
            => s?.StartsWith(prefix, comparison) == true ? s[prefix.Length..] : s;

        /// <summary>
        /// Tries remove one prefix out of an ordered list of <paramref name="prefixes"/> from the supplied string. The prefixes are
        /// tested in order and the method returns as soon as prefix is successfully removed. If no prefix is match the original 
        /// string is returned.
        /// </summary>
        /// <remarks>This method calls <see cref="TrimStart(string, string, StringComparison)"/> in a loop 
        /// over the <paramref name="prefixes"/>. See that method for more information.</remarks>
        /// <param name="s">The string to (possibly) remove one of the <paramref name="prefixes"/></param>
        /// <param name="prefixes">An ordered list of prefixes to try to remove from the original string</param>
        /// <param name="comparison">An option <see cref="StringComparison"/> (defaults to <see cref="StringComparison.Ordinal"/>)</param>
        /// <returns>A new string with one of the prefixes removed if it was found on the original</returns>
        public static string? TrimStart(this string s, string[] prefixes, StringComparison comparison = StringComparison.Ordinal) {
            if (s is null) return null;
            foreach(var prefix in prefixes) { 
                var removed = s.TrimStart(prefix, comparison);
                if (removed != s)
                    return removed;
            }
            return s;
        }
            
        public static string ToString(this bool v, string yes, string no) => v ? yes : no;

        /// <summary>
        /// Converts a string in the format: key {one of the <paramref name="separators"/>} [value], into 
        /// a (string, string) tuple with the key in the first item and the value in the second item.
        /// </summary>
        /// <remarks>The value may be omitted, in which case an empty string is returned in it's place.
        /// The separator may appear in the value itself.</remarks>
        /// <param name="s">The string in the key-value format.</param>
        /// <param name="separators">A list of separators that could be used to separate the key from the value.</param>
        /// <returns>A tuple with the key in the first item and the value in the second.</returns>
        public static (string key, string value) ToKeyValuePair(this string s, params char[] separators)
            => ToKeyValuePair(s, normalizeKey: false, trimValue: false, separators);

        /// <summary>
        /// Converts a string in the format: key {one of the <paramref name="separators"/>} [value], into 
        /// a (string, string) tuple with the key in the first item and the value in the second item, 
        /// optionally normalizing (trimming and lowercasing it) the key and trimming the value.
        /// </summary>
        /// <remarks>The value may be omitted, in which case an empty string is returned in it's place.
        /// The separator may appear in the value itself.</remarks>
        /// <param name="s">The string in the key-value format.</param>
        /// <param name="normalizeKey">If true applies both <see cref="string.Trim()"/> and <see cref="string.ToLowerInvariant"/> to the key.</param>
        /// <param name="trimValue">If true applies <see cref="string.Trim()"/> to the value.</param>
        /// <param name="separators">A list of separators that could be used to separate the key from the value.</param>
        /// <returns>A tuple with the (possibly normalized) key in the first item and the (possibly trimmed) value in the second.</returns>
        public static (string key, string value) ToKeyValuePair(this string s, bool normalizeKey = false, bool trimValue = false, params char[] separators) {
            if (string.IsNullOrWhiteSpace(s))
                return default;
            if (separators.Length == 0)
                separators = [':'];
            var pos = s.IndexOfAny(separators);
            if (pos == -1)
                pos = s.Length;
            string key = s[..pos];
            string value = pos < s.Length ? s[(pos + 1)..] : "";
            return (
                normalizeKey ? key.Trim().ToLowerInvariant() : key, 
                trimValue ? value.Trim() : value
            );
        }

        /// <summary>
        /// Indents a possibly multiline string with <paramref name="count"/> <paramref name="indentChar"/> characters.
        /// </summary>
        /// <param name="lines">The single or multiline string to indent.</param>
        /// <param name="count">The number of characters for the indentation.</param>
        /// <param name="indentChar">The indentation characater.</param>
        /// <returns>The indented single or multiline string.</returns>
        public static string Indent(this string lines, int count, char indentChar = ' ') {
            var indent = new string(indentChar, count);
            return $"{indent}{lines.Replace(Environment.NewLine, $"{Environment.NewLine}{indent}")}";
        }

        /// <summary>
        /// Finds the <paramref name="nth"/> occurrence of the character <paramref name="ch"/> inside
        /// the string <paramref name="str"/> starting at the index denoted by <paramref name="startIndex"/>.
        /// This method is nullsafe (returns -1) and is as fast as possible: https://stackoverflow.com/a/74964725/285678
        /// </summary>
        /// <param name="str">The string being searched.</param>
        /// <param name="ch">A unicode character to seek.</param>
        /// <param name="nth">Which occurence of the character to seek.</param>
        /// <param name="startIndex">The search starting position.</param>
        /// <returns>The zero-based index position of the <paramref name="nth"/> occurrence of <paramref name="ch"/>
        /// inside the string, or -1 if its not found.</returns>
        public static int IndexOfNth(this string? str, char ch, int nth, int startIndex = 0) {
            if (str == null)
                return -1;
            var idx = str.IndexOf(ch, startIndex);
            while (idx >= 0 && --nth > 0)
                idx = str.IndexOf(ch, startIndex + idx + 1);
            return idx;
        }

        /// <summary>
        /// Removes the number of lines denoted by <paramref name="linesToStrip"/> from the string. 
        /// If the string has fewer lines it will return an empty string.
        /// </summary>
        /// <param name="s">The string being stripped of lines.</param>
        /// <param name="linesToStrip">The count of lines to strip/remove from the string.</param>
        /// <returns>The string stripped of the lines if there are more than <paramref name="linesToStrip"/> 
        /// lines in it, otherwise an empty string.</returns>
        public static string RemoveLines(this string s, int linesToStrip) {
            if (linesToStrip == 0)
                return s;
            var linePos = s.IndexOfNth('\n', linesToStrip);
            return linePos switch {
                -1 => string.Empty,
                _ => s[(linePos + 1)..]
            };
        }

        /// <summary>
        /// Strips all occurrences of any <paramref name="chars"/> inside the string <paramref name="s"/>.
        /// The method is null safe and returns an empty string if provided with a null string.
        /// </summary>
        /// <param name="s">The string being stripped of chars.</param>
        /// <param name="chars">The characters to strip from the string.</param>
        /// <returns>A string stripped of the provided characters.</returns>
        public static string StripChars(this string? s, params char[] chars) {
            if (s is null) return string.Empty; 
            if (chars.Length == 0) return s;
            var sb = new StringBuilder(s.Length);
            foreach(var c in s) {
                if (!c.In(chars))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Capitalizes the first (and only the first) letter of the given string <paramref name="s"/>.
        /// This call is null safe and will return an empty string if provided with a null or empty string.
        /// </summary>
        /// <remarks>This method is very fast and has one less allocation than most versions out there!
        /// Although this code is my own it is identical to what's being discussed here: 
        /// https://stackoverflow.com/questions/4135317/make-first-letter-of-a-string-upper-case-with-maximum-performance
        /// </remarks>
        /// <param name="s">The string to be capitalized.</param>
        /// <returns>The capitalized string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        public static string Capitalize(this string s) {
            if (s is null || s.Length == 0) 
                return string.Empty;
            var array = s.ToCharArray();
            array[0] = char.ToUpperInvariant(array[0]);
            return new string(array);
        }
        
        public static IEnumerable<string> SplitOnUpperOrSeparator(this string s, params char[] seps)
            => SplitOnUpperOrSeparator(s, false, seps);

        public static IEnumerable<string> SplitOnUpperOrSeparator(this string s, bool keepSeparators, params char[] seps) {
            bool isUpper(char c) => char.IsUpper(c);
            bool isSep(char c) => seps.Length > 0 && c.In(seps);

            var chars = s.ToCharArray();
            List<string> parts = [];
            int l = 0;
            for (int i = 1; i < chars.Length; i++) {
                void advance() => l = i;
                var ch = chars[i];
                var prev = s[i - 1];
                if (isUpper(ch) && isUpper(prev))
                    // keeps ALLcaps together
                    continue;
                if (isSep(ch) && isSep(prev)) {
                    // ignores repeating separators ---
                    if (!keepSeparators) 
                        advance();
                    continue;
                }
                if (isSep(ch) && !isSep(prev)) {
                    // reached a new separator: found a part!
                    parts.Add(new string(chars, l, i - l));
                    advance();
                } else if (!isSep(ch) && isSep(prev)) {
                    // starting a string after a sep
                    if (keepSeparators)
                        parts.Add(new string(chars, l, i - l));
                    advance();
                } else if (isUpper(ch) && !isUpper(prev)) {
                    // reached a new uppercase: found a part!
                    parts.Add(new string(chars, l, i - l));
                    advance();
                } 
            }
            if (l < chars.Length && (keepSeparators || !isSep(chars[l])))
                parts.Add(new string(chars, l, chars.Length - l));
            return parts;
        }
#if NETSTANDARD2_1_OR_GREATER
    // No need for the following "Replaces" because they're already baked-in and faster
#elif NET6_0_OR_GREATER
    // No need for the following "Replaces" because they're already baked-in and faster
#else
        public static string Replace(this string searchSpace, string oldValue, string newValue, bool ignoreCase) 
            => Replace(searchSpace, oldValue, newValue, options: ignoreCase ? CompareOptions.OrdinalIgnoreCase : CompareOptions.None);

        // Lifted and backported from .NET String.Manipulation.cs source code
        public static string Replace(this string searchSpace, string oldValue, string newValue, 
            CompareInfo? compareInfo = null, CompareOptions? options = null) {
            if (string.IsNullOrEmpty(oldValue)) 
                return searchSpace;
            compareInfo ??= CultureInfo.InvariantCulture.CompareInfo;
            options ??= CompareOptions.OrdinalIgnoreCase;

            var result = new StringBuilder(searchSpace.Length);
            bool hasDoneAnyReplacements = false;
            int pos = 0;
            while (pos < searchSpace.Length) {
                var index = compareInfo.IndexOf(searchSpace, oldValue, pos, options.Value);
                if (index < 0) 
                    break;

                // append the unmodified portion of search space
                result.Append(searchSpace[pos..index]);
                // append the replacement
                result.Append(newValue);

                pos = index + oldValue.Length;
                hasDoneAnyReplacements = true;
            }

            // if we didn't replace anything returns the original string
            if (!hasDoneAnyReplacements)
                return searchSpace;

            // Append what remains of the search space, then allocate the new string.
            if (pos < searchSpace.Length)
                result.Append(searchSpace[pos..]);
            return result.ToString();
        }
#endif
    }
}
