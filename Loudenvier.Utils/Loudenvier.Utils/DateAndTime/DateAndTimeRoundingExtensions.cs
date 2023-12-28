using System;

namespace Loudenvier.Utils
{
    public static class DateAndTimeRoundingExtensions {

        /// <summary>Rounds <paramref name="ticks"/> of time at arbitrary time <paramref name="interval"/>s</summary>
        /// <param name="ticks">The ticks of time to round (10.000.000 ticks equals a millisecond)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for rounding</param>
        /// <returns>The rounded ticks</returns>
        /// <remarks>You can use any interval to round values at/to. If its half way up to the next interval, it will
        /// round up, or else it will round down. For example:
        /// <code>
        /// // Rounding 24.000.000 ticks (just bellow 2.5seconds) at 1 second intervals returns 20.000.000 (2 seconds):
        /// _ = 24_000_000L.Round(TimeSpan.FromSeconds(1)) == 20_000_000; // is true
        /// //
        /// // Rounding 25.000.000 ticks (2.5 seconds) at 1 second intervals return 30.000.000 ticks (3 seconds):
        /// _ = 25_000_000L.Round(TimeSpan.FromSeconds(1)) == 30_000_000; // is true
        /// </code></remarks>
        public static long Round(this long ticks, TimeSpan interval) =>
            interval.Ticks * ((ticks + (interval.Ticks / 2)) / interval.Ticks);

        /// <summary>Rounds <paramref name="ticks"/> of time at arbitrary time <paramref name="interval"/>s applying a
        /// specific <paramref name="grace"/> period. If it's above the grace period it will round up, or else it will round down.</summary>
        /// <param name="ticks">The ticks of time to round (10.000.000 ticks equals a millisecond)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for rounding</param>
        /// <param name="grace">The grace period to apply to the rounding</param>
        /// <returns>The rounded ticks</returns>
        /// <remarks>You can use any interval to round values at/to. If its within the grace period (non exclusive) it will round down,
        /// or else round up. For example:
        /// <code>
        /// // Rounding 26.000.000 ticks (2600 millis) at 1 second intervals and 601 millis grace returns 20.000.000 (2 seconds):
        /// _ = 26_000_000L.Round(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(601)) == 20_000_000; // is true
        /// //
        /// // Rounding 26.000.000 ticks (2600 millis) at 1 second intervals and 600 millis grace return 30.000.000 ticks (3 seconds):
        /// _ = 26_000_000L.Round(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(600)) == 30_000_000; // is true
        /// </code></remarks>
        public static long Round(this long ticks, TimeSpan interval, TimeSpan grace) =>
            interval.Ticks * ((ticks + (interval.Ticks - grace.Ticks)) / interval.Ticks);

        /// <summary>Floors (rounds down) <paramref name="ticks"/> at arbitrary time <paramref name="interval"/>s</summary>
        /// <param name="ticks">The ticks of time to floor (round down)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for flooring</param>
        /// <returns>The floored ticks</returns>
        public static long Floor(this long ticks, TimeSpan interval) =>
            interval.Ticks * (ticks / interval.Ticks);

        /// <summary>Ceils (rounds up) <paramref name="ticks"/> at arbitrary time <paramref name="interval"/>s</summary>
        /// <param name="ticks">The ticks of time to ceil (round up)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for ceiling</param>
        /// <returns>The ceiled ticks</returns>
        public static long Ceil(this long ticks, TimeSpan interval) =>
            interval.Ticks * ((ticks + interval.Ticks - 1) / interval.Ticks);

        /// <summary>Rounds the <paramref name="date"/> and time value at arbitrary time <paramref name="interval"/>s. It rounds up
        /// if the date and time is half way up to the next interval, otherwise it rounds down.</summary>
        /// <param name="date">The date and time to round</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as the interval for rounding</param>
        /// <returns>The rounded <see cref="DateTime"/> value</returns>
        /// <remarks>You can use any interval to round values at/to. If its half way up the next interval it rounds up, otherwise
        /// it rounds down. For example:
        /// <code>
        /// new DateTime(2023, 3, 31, 8, 30, 0).Round(TimeSpan.FromHours(1)); // results in 2023/3/31 09:00:00
        /// new DateTime(2023, 3, 31, 8, 29, 59).Round(TimeSpan.FromHours(1)); // results in 2023/3/31 08:00:00
        /// </code>
        /// </remarks>
        public static DateTime Round(this DateTime date, TimeSpan interval) => new(date.Ticks.Round(interval));

        /// <summary>Rounds the <paramref name="date"/> and time value at arbitrary time <paramref name="interval"/>s 
        /// applying a specific <paramref name="grace"/> period. It rounds down if it's within the grace period (non inclusive),
        /// otherwise it rounds up.</summary>
        /// <param name="date">The date and time to round</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as the interval for rounding</param>
        /// <returns>The rounded <see cref="DateTime"/> value</returns>
        /// <remarks>You can use any interval to round values at/to. If its within the grace period it rounds down, otherwise
        /// it rounds up. For example:
        /// <code>
        /// new DateTime(2023, 3, 31, 8, 35, 0).Round(TimeSpan.FromHours(1), TimeSpan.FromMinutes(40)); // results in 2023/3/31 08:00:00
        /// new DateTime(2023, 3, 31, 8, 40, 1).Round(TimeSpan.FromHours(1), TimeSpan.FromMinutes(40)); // results in 2023/3/31 09:00:00
        /// </code>
        /// </remarks>
        public static DateTime Round(this DateTime date, TimeSpan interval, TimeSpan grace) => new(date.Ticks.Round(interval, grace));

        /// <summary>Floors (rounds down) the <paramref name="date"/> and time at arbitrary time <paramref name="interval"/>s</summary>
        /// <param name="date">The date and time to floor (round down)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for flooring</param>
        /// <returns>The floored date and time</returns>
        public static DateTime Floor(this DateTime date, TimeSpan interval) => new(date.Ticks.Floor(interval));

        /// <summary>Ceils (rounds up) the <paramref name="date"/> and time at arbitrary time <paramref name="interval"/>s</summary>
        /// <param name="date">The date and time to floor (round down)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for flooring</param>
        /// <returns>The floored date and time</returns>
        public static DateTime Ceil(this DateTime date, TimeSpan interval) => new(date.Ticks.Ceil(interval));

        /// <summary>Rounds the <paramref name="date"/> and time offset value at arbitrary time <paramref name="interval"/>s. It rounds up
        /// if the date and time is half way up to the next interval, otherwise it rounds down.</summary>
        /// <param name="date">The date and time to round</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as the interval for rounding</param>
        /// <returns>The rounded <see cref="DateTime"/> value</returns>
        /// <remarks>You can use any interval to round values at/to. If its half way up the next interval it rounds up, otherwise
        /// it rounds down. For example:
        /// <code>
        /// new DateTimeOffset(2023, 3, 31, 8, 30, 0, TimeSpan.FromHours(1)).Round(TimeSpan.FromHours(1)); // results in 2023/3/31 09:00:00 01:00
        /// new DateTimeOffset(2023, 3, 31, 8, 29, 59).Round(TimeSpan.FromHours(1), TimeSpan.FromHours(1)); // results in 2023/3/31 08:00:00 01:00
        /// </code>
        /// </remarks>
        public static DateTimeOffset Round(this DateTimeOffset date, TimeSpan interval) => new(date.Ticks.Round(interval), date.Offset);

        /// <summary>Rounds the <paramref name="date"/> and time offset value at arbitrary time <paramref name="interval"/>s 
        /// applying a specific <paramref name="grace"/> period. It rounds down if it's within the grace period (non inclusive),
        /// otherwise it rounds up.</summary>
        /// <param name="date">The date and time to round</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as the interval for rounding</param>
        /// <returns>The rounded <see cref="DateTime"/> value</returns>
        /// <remarks>You can use any interval to round values at/to. If its within the grace period it rounds down, 
        /// otherwise it rounds up. For example:
        /// <code>
        /// new DateTimeOffset(2023, 3, 31, 8, 40, 0, TimeSpan.FromHours(1)).Round(TimeSpan.FromHours(1), TimeSpan.FromMinutes(40)); // results in 2023/3/31 09:00:00 01:00
        /// new DateTimeOffset(2023, 3, 31, 8, 39, 59).Round(TimeSpan.FromHours(1), TimeSpan.FromHours(1), TimeSpan.FromMinutes(40)); // results in 2023/3/31 08:00:00 01:00
        /// </code>
        /// </remarks>
        public static DateTimeOffset Round(this DateTimeOffset date, TimeSpan interval, TimeSpan grace) => new(date.Ticks.Round(interval, grace), date.Offset);

        /// <summary>Floors (rounds down) the <paramref name="date"/> and time offset at arbitrary time <paramref name="interval"/>s</summary>
        /// <param name="date">The date and time to floor (round down)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for flooring</param>
        /// <returns>The floored date and time</returns>
        public static DateTimeOffset Floor(this DateTimeOffset date, TimeSpan interval) => new(date.Ticks.Floor(interval), date.Offset);

        /// <summary>Ceils (rounds up) the <paramref name="date"/> and time at arbitrary time <paramref name="interval"/>s</summary>
        /// <param name="date">The date and time to floor (round down)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for flooring</param>
        /// <returns>The floored date and time</returns>
        public static DateTimeOffset Ceil(this DateTimeOffset date, TimeSpan interval) => new(date.Ticks.Ceil(interval), date.Offset);

        /// <summary>Rounds the <paramref name="time"/> span at arbitrary time <paramref name="interval"/>s. It rounds up
        /// if the time is half way up to the next interval, otherwise it rounds down.</summary>
        /// <param name="date">The time span to round</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as the interval for rounding</param>
        /// <returns>The rounded <see cref="TimeSpan"/> value</returns>
        /// <remarks>You can use any interval to round values at/to. If its half way up the next interval it rounds up, otherwise
        /// it rounds down. For example:
        /// <code>
        /// TimeSpan.FromMinutes(30).Round(TimeSpan.FromHours(1)); // results in 01:00:00
        /// TimeSpan.FromMinutes(29).Round(TimeSpan.FromHours(1)); // results in 00:00:00
        /// </code>
        /// </remarks>        public static TimeSpan Round(this TimeSpan time, TimeSpan interval) => new(time.Ticks.Round(interval));
        public static TimeSpan Round(this TimeSpan time, TimeSpan interval, TimeSpan grace) => new(time.Ticks.Round(interval, grace));

        /// <summary>Floors (rounds down) the <paramref name="time"/> span at arbitrary time <paramref name="interval"/>s</summary>
        /// <param name="date">The time span to floor (round down)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for flooring</param>
        /// <returns>The floored <see cref="TimeSpan"/></returns>
        public static TimeSpan Floor(this TimeSpan time, TimeSpan interval) => new(time.Ticks.Floor(interval));

        /// <summary>Ceils (rounds up) the <paramref name="time"/> span at arbitrary time <paramref name="interval"/>s</summary>
        /// <param name="date">The time span to ceil (round up)</param>
        /// <param name="interval">The <see cref="TimeSpan"/> to use as an interval for ceiling</param>
        /// <returns>The ceiled <see cref="TimeSpan"/></returns>
        public static TimeSpan Ceil(this TimeSpan time, TimeSpan interval) => new(time.Ticks.Ceil(interval));

        /// <summary>Returns the absolute value for the <paramref name="time"/> (e.g.: -00:20:00 results in 00:20:00)</summary>
        /// <param name="time">The time to get the absolute value</param>
        /// <returns>The absolute value of <paramref name="time"/></returns>
        public static TimeSpan Abs(this TimeSpan time) => new(Math.Abs(time.Ticks));
    }
}
