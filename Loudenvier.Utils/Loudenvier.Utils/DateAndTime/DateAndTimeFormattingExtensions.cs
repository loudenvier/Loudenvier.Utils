using System;
using System.Globalization;
using System.Linq;

namespace Loudenvier.Utils;

public static class DateAndTimeFormattingExtensions {

    /// <summary>Converts the date (ignoring the time component) to it's ISO date format (yyyy-MM-dd)</summary>
    /// <param name="date">The date and time to format</param>
    /// <returns>The date formatted as an ISO date string</returns>
    public static string ToDateISO(this DateTime date) => date.ToString("yyyy-MM-dd");

    /// <summary>Converts a textual date in the format 'M-yy' or 'M-yyyy' into a <see cref="DateTime"/></summary>
    /// <param name="d">The date text in Month-Year format</param>
    /// <returns>The converted <see cref="DateTime"/> or default if it's not properly formatted</returns>
    public static DateTime FromMonthYear(this string d) {
        if (string.IsNullOrWhiteSpace(d))
            return default;
        if (DateTime.TryParseExact(d, "M-yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            return dt;
        if (DateTime.TryParseExact(d, "M-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;
        return default;
    }

    /// <summary>Parses the textual date represented by <paramref name="date"/> using the 
    /// first acceptable format passed in <paramref name="formats"/></summary>
    /// <param name="date">The textual representation of the date to parse</param>
    /// <param name="formats">An array of formats to try to parse the date</param>
    /// <returns>The parsed date as <see cref="DateTime"/></returns>
    /// <remarks>The parsing uses <see cref="CultureInfo.InvariantCulture"/> and <see cref="DateTimeStyles.None"/> for parsing</remarks>
    /// <exception cref="FormatException">If <paramref name="date"/> doesn't conform to any <paramref name="formats"/> passed</exception>
    public static DateTime FromDateFormats(this string date, params string[] formats) {
        foreach (string format in formats)
            if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
        throw new FormatException($"The date [{date}] didn't conform to any of the provided formats: {string.Join(", ", formats)}");
    }

    static readonly int[] weights = [60 * 60 * 1000, 60 * 1000, 1000, 1];
    /// <summary> Parses a string to a TimeSpan value allowing natural language or any value in each component (e.g.: 32h20min or 32:20:0 </summary>
    /// <param name="s">String to transform into TimeSpan</param>
    /// <param name="languageCode">An option language code to derive parsing patterns from (defaults to "pt" - portuguese)</param>
    /// <returns>The resulting <see cref="TimeSpan"/> or <see cref="TimeSpan.Zero"/> if string is empty or white space</returns>
    public static TimeSpan ToTimeSpan(this string s, string languageCode = "pt") {
        if (string.IsNullOrWhiteSpace(s))
            return TimeSpan.Zero;
        try {
            return s.ToTimeSpanFromNaturalLanguage(languageCode);
        } catch (FormatException) {
            s = s.Trim();
            var negative = s.StartsWith("-");
            if (negative)
                s = s.Replace("-", "");
            var time = TimeSpan.FromMilliseconds(s.Split('.', ':').Zip(weights, (d, w) => Convert.ToInt64(d) * w).Sum());
            if (negative)
                time = time.Negate();
            return time;
        }
    }
    /// <summary>
    /// Calls <see cref="ToTimeSpan(string)"/> but returns a nullable <see cref="TimeSpan"/> instead of <see cref="TimeSpan.Zero"/>
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static TimeSpan? ToNullableTimeSpan(this string s)
        => string.IsNullOrWhiteSpace(s) ? null : s.ToTimeSpan();

    /// <summary>
    /// Converts a time string in natural language (e.g.: 34h22m) into <see cref="TimeSpan"/> in a somewhat forgiving manner
    /// </summary>
    /// <param name="s">The textual representation of the time</param>
    /// <param name="patterns">An optional <see cref="NaturalLanguageTimeParser.TimePatterns"/> to use for parsing</param>
    /// <returns>The converted text to <see cref="TimeSpan"/></returns>
    public static TimeSpan ToTimeSpanFromNaturalLanguage(this string s, TimePatterns? patterns = null)
        => new NaturalLanguageTimeParser(patterns ?? TimePatterns.Default).Execute(s);

    /// <summary>
    /// Converts a time string in natural language (e.g.: 34h22m) into a <see cref="TimeSpan"/> in a somewhat forgiving manner
    /// </summary>
    /// <param name="s">The textual representation of the time</param>
    /// <param name="languageCode">An optional language code to extract patterns from (defaults to "pt" - portuguese)</param>
    /// <returns>The converted text to <see cref="TimeSpan"/></returns>
    public static TimeSpan ToTimeSpanFromNaturalLanguage(this string s, string languageCode = "pt")
        => new NaturalLanguageTimeParser(languageCode).Execute(s);

}
