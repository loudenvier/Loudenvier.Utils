using System;

namespace Loudenvier.Utils
{
    public static class Time
    {
        /// <summary>
        /// Returns the smaller of two time spans (null values will coalesce to <see cref="TimeSpan.Zero"/>.
        /// </summary>
        /// <param name="a">The first of two time spans to compare.</param>
        /// <param name="b">The second of two time spans to compare.</param>
        /// <returns>Parameter <paramref name="a"/> or <paramref name="b"/>, whichever is smaller.</returns>
        public static TimeSpan Min(TimeSpan? a, TimeSpan? b) =>
            ((a ?? default) <= (b ?? default) ? a : b) ?? default;

        /// <summary>
        /// Returns the larger of two time spans (null values will coalesce to <see cref="TimeSpan.Zero"/>.
        /// </summary>
        /// <param name="a">The first of two time spans to compare.</param>
        /// <param name="b">The second of two time spans to compare.</param>
        /// <returns>Parameter <paramref name="a"/> or <paramref name="b"/>, whichever is larger.</returns>
        public static TimeSpan Max(TimeSpan? a, TimeSpan? b) =>
            ((a ?? default) >= (b ?? default) ? a : b) ?? default;
    }
}
