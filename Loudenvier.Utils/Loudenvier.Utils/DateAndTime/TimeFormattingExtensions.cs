using System;
using System.Collections.Generic;

namespace Loudenvier.Utils;

/// <summary>
/// Extension methods to format date and time to a few common scenarios and to human-friendly format
/// like <c>8h 30min</c> instead of <c>08:30:00</c> (<see cref="TimeFormattingExtensions.ToFriendlyString(TimeSpan)"/>).
/// </summary>
public static class TimeFormattingExtensions {

    /// <summary>Converts the <see cref="TimeSpan"/> into a string in the format HH:mm (where HH can be greater than 24) 
    /// or "0" if the TimeSpan is zero.</summary>
    /// <param name="time">The <see cref="TimeSpan"/> to convert to string</param>
    /// <returns>A string representation of the <paramref name="time"/> in the expected format</returns>
    public static string ToStringShortTotalHours(this TimeSpan time)
        => time.Minutes == 0 ? time.TotalHours.ToString("0") : time.ToStringTotalHoursAndMinutes();

    static string TimePrefix(TimeSpan time) => time < TimeSpan.Zero && time > -1.Hours() ? "-" : "";

    /// <summary>Converts the <paramref name="time"/> into a string in the format HH:mm (where HH can be greater than 24)</summary>
    /// <param name="time">The <see cref="TimeSpan"/> to convert to string</param>
    /// <returns>A string representation of the <paramref name="time"/> in the expected format</returns>
    public static string ToStringTotalHoursAndMinutes(this TimeSpan time)
        => $"{TimePrefix(time)}{Math.Truncate(time.TotalHours):00}:{time:mm}";
        //string.Format(TimePrefix(time) + "{0:00}:{1:mm}", Math.Truncate(time.TotalHours), time);

    /// <summary>Converts the <paramref name="time"/> into a string in the format HH:mm:ss (where HH can be greater than 24)</summary>
    /// <param name="time">The <see cref="TimeSpan"/> to convert to string</param>
    /// <returns>A string representation of the <paramref name="time"/> in the expected format</returns>
    public static string ToStringTotalHours(this TimeSpan time)
        => $"{TimePrefix(time)}{Math.Truncate(time.TotalHours):00}{time:mm:ss}";
        //string.Format(TimePrefix(time) + "{0:00}:{1:mm}:{1:ss}", Math.Truncate(time.TotalHours), time);

    /// <summary>Converts the <paramref name="time"/> into a string in the format hh:mm rolling over 24h</summary>
    /// <param name="time">The <see cref="TimeSpan"/> to convert to string</param>
    /// <param name="def">A default string value to return in case <paramref name="time"/> is null (defaults to "-")</param>
    /// <returns>The time converted as string in the expected format or the default value if null</returns>
    public static string ToStringHM(this TimeSpan? time, string def = "-")
        => time is null ? def : time.Value.ToString(@"hh\:mm");

    /// <summary>
    /// Converts the <paramref name="time"/> to a "friendly" string representation in the form "12h 25min 30s 450ms" omitting 
    /// zeroed parts (e.g: 01:40:00 yields "1h 40min")
    /// </summary>
    /// <param name="time">The <see cref="TimeSpan"/> to be converted to string</param>
    /// <returns>The <paramref name="time"/> converted to string in the expected format</returns>
    public static string ToFriendlyString(this TimeSpan time) {
        var t = time.Abs();
        var h = Math.Truncate(t.TotalHours);
        var m = t.Minutes;
        var s = t.Seconds;
        var ms = t.Milliseconds;
        var parts = new List<string>();
        if (h != 0) parts.Add(h + "h");
        if (m != 0) parts.Add(m + "min");
        if (s != 0) parts.Add(s + "s");
        if (ms != 0) parts.Add(ms + "ms");
        return (time < TimeSpan.Zero ? "-" : "") + string.Join(" ", parts);
    }

}
