using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Loudenvier.Utils
{
    public static class RegexpExtensions

    {
        /// <summary>
        /// Converts the <paramref name="matches"/> collection into an <see cref="IEnumerable{string}"/>
        /// with the results from the matches/captures as strings instead of <see cref="Match"/> which 
        /// is what you want most of the time.
        /// </summary>
        /// <param name="matches">A collection of <see cref="Regex"/> matches.</param>
        /// <returns>The results of the <paramref name="matches"/> as strings.</returns>
        public static IEnumerable<string> AsStrings(this MatchCollection matches)
#if NETCOREAPP3_0_OR_GREATER
            => matches.Select(m => m.ToString());
#else
            => matches.Cast<Match>().Select(m => m.ToString());
#endif

    }
}
