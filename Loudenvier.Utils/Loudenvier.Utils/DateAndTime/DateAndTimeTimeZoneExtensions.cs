using System;
using System.Collections.Generic;
using System.Linq;

namespace Loudenvier.Utils;

public static class DateAndTimeTimeZoneExtensions {

    static readonly Lazy<IEnumerable<TimeZoneInfo>> tmzs
        = new(() => TimeZoneInfo.GetSystemTimeZones());
    static IEnumerable<TimeZoneInfo> TimeZones => tmzs.Value;

    public static TimeZoneInfo ToSystemTimeZone(this string standardName) {
        if (string.IsNullOrWhiteSpace(standardName))
            return TimeZoneInfo.Local;
        var tzi = TimeZones.FirstOrDefault(t => t.Id == standardName || t.StandardName == standardName);
        return tzi ?? TimeZoneInfo.Local;
    }

    public static DateTime GetNow(this TimeZoneInfo tzi) => TimeZoneInfo.ConvertTime(DateTime.Now, tzi);

    public static DateTime ChangeOffset(this DateTime dt, TimeSpan? utcOffset) {
        if (utcOffset == null)
            return dt;
        var utc = dt - DateTimeOffset.Now.Offset;
        return utc + utcOffset.Value;
    }

    public static string ToDateTimeStr(this DateTime time, string fmt, string def) {
        if (def != null && time == default)
            return def;
        return time.ToString(fmt);
    }

}
