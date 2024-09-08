using System;
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


    }
}
