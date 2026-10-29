using System;

namespace Loudenvier.Utils.IO;

/// <summary>Allocation-free, identical simple glob semantics on every backend.</summary>
internal static class FastNameMatcher
{
    internal static bool Matches(ReadOnlySpan<char> pattern, ReadOnlySpan<char> name, bool ignoreCase)
    {
        int p = 0, n = 0, lastStar = -1, resumeName = 0;
        while (n < name.Length)
        {
            if (p < pattern.Length && pattern[p] == '*')
            {
                lastStar = p++;
                resumeName = n;
            }
            else if (p < pattern.Length &&
                (pattern[p] == '?' || Equal(pattern[p], name[n], ignoreCase)))
            {
                p++;
                n++;
            }
            else if (lastStar != -1)
            {
                p = lastStar + 1;
                n = ++resumeName;
            }
            else return false;
        }
        while (p < pattern.Length && pattern[p] == '*') p++;
        return p == pattern.Length;
    }

    private static bool Equal(char a, char b, bool ignoreCase) =>
        a == b || (ignoreCase && char.ToUpperInvariant(a) == char.ToUpperInvariant(b));
}
