using System;

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
            



    }
}
