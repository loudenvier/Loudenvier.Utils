using System;

namespace Loudenvier.Utils
{
    public static class DateAndTimeFluentBuilders {

        /// <summary>
        /// Extends <see cref="int"/> to allow building a <see cref="DateTime"/> which is <see cref="y"/> years ago from today
        /// with a simple sintax like: <c>DateTime twoYearsAgo = 2.YearsAgo();</c>
        /// </summary>
        /// <param name="y">How many years ago</param>
        /// <returns>The datetime <paramref name="y"/> years ago from today</returns>
        public static DateTime YearsAgo(this int y) => DateTime.Today.AddYears(-y);

        /// <summary>
        /// Converts a <see cref="int"/> into a quantity of years as a <see cref="TimeSpan"/>
        /// </summary>
        /// <param name="y">The quantity of years</param>
        /// <returns>A <see cref="TimeSpan"/> representing <paramref name="y"/> years</returns>
        public static TimeSpan Years(this int y) => new DateTime(y + 1, 1, 1) - default(DateTime);

        /// <summary>Adds an extension method to <see cref="int"></see> to create a Fluent Interface for generating <see cref="TimeSpan"></see>s. 
        /// Using this code you can do: <c>TimeSpan time = 2.Days() + 10.Hours() + 12.Minutes() + 20.Seconds() + 890.Milliseconds();</c>
        /// </summary>
        /// <param name="d">The extended integer to transform into days</param>
        /// <returns>The desired <see cref="TimeSpan"></see></returns>
        public static TimeSpan Days(this int d) => new(d * TimeSpan.TicksPerDay);

        /// <summary>Adds an extension method to <see cref="int"></see> to create a Fluent Interface for generating <see cref="TimeSpan"></see>s. 
        /// Using this code you can do: <c>TimeSpan time = 2.Days() + 10.Hours() + 12.Minutes() + 20.Seconds() + 890.Milliseconds();</c>
        /// </summary>
        /// <param name="h">The extended integer to transform into hours</param>
        /// <returns>The desired <see cref="TimeSpan"></see></returns>
        public static TimeSpan Hours(this int h) => H(h);
        /// <summary>Adds an extension method to <see cref="int"></see> to create a Fluent Interface for generating <see cref="TimeSpan"></see>s. 
        /// Using this code you can do: <c>TimeSpan time = 2.Days() + 10.Hours() + 12.Minutes() + 20.Seconds() + 890.Milliseconds();</c>
        /// </summary>
        /// <param name="h">The extended integer to transform into hours</param>
        /// <returns>The desired <see cref="TimeSpan"></see></returns>
        public static TimeSpan H(this int h) => TimeSpan.FromHours(h);

        /// <summary>Adds an extension method to <see cref="int"></see> to create a Fluent Interface for generating <see cref="TimeSpan"></see>s. 
        /// Using this code you can do: <c>TimeSpan time = 2.Days() + 10.Hours() + 12.Minutes() + 20.Seconds() + 890.Milliseconds();</c>
        /// </summary>
        /// <param name="m">The extended integer to transform into minutes</param>
        /// <returns>The desired <see cref="TimeSpan"></see></returns>
        public static TimeSpan Minutes(this int m) => Min(m);
        /// <summary>Adds an extension method to <see cref="int"></see> to create a Fluent Interface for generating <see cref="TimeSpan"></see>s. 
        /// Using this code you can do: <c>TimeSpan time = 2.Days() + 10.Hours() + 12.Minutes() + 20.Seconds() + 890.Milliseconds();</c>
        /// </summary>
        /// <param name="m">The extended integer to transform into minutes</param>
        /// <returns>The desired <see cref="TimeSpan"></see></returns>
        public static TimeSpan Min(this int m) => TimeSpan.FromMinutes(m);

        /// <summary>Adds an extension method to <see cref="int"></see> to create a Fluent Interface for generating <see cref="TimeSpan"></see>s. 
        /// Using this code you can do: <c>TimeSpan time = 2.Days() + 10.Hours() + 12.Minutes() + 20.Seconds() + 890.Milliseconds();</c>
        /// </summary>
        /// <param name="s">The extended integer to transform into seconds</param>
        /// <returns>The desired <see cref="TimeSpan"></see></returns>
        public static TimeSpan Seconds(this int s) => Sec(s);
        /// <summary>Adds an extension method to <see cref="int"></see> to create a Fluent Interface for generating <see cref="TimeSpan"></see>s. 
        /// Using this code you can do: <c>TimeSpan time = 2.Days() + 10.Hours() + 12.Minutes() + 20.Seconds() + 890.Milliseconds();</c>
        /// </summary>
        /// <param name="s">The extended integer to transform into seconds</param>
        /// <returns>The desired <see cref="TimeSpan"></see></returns>
        public static TimeSpan Sec(this int s) => TimeSpan.FromSeconds(s);

        /// <summary>Adds an extension method to <see cref="int"></see> to create a Fluent Interface for generating <see cref="TimeSpan"></see>s. 
        /// Using this code you can do: <c>TimeSpan time = 2.Days() + 10.Hours() + 12.Minutes() + 20.Seconds() + 890.Milliseconds();</c>
        /// </summary>
        /// <param name="ms">The extended integer to transform into milliseconds</param>
        /// <returns>The desired <see cref="TimeSpan"></see></returns>
        public static TimeSpan Milliseconds(this int ms) => Milli(ms);
        /// <summary>Adds an extension method to <see cref="int"></see> to create a Fluent Interface for generating <see cref="TimeSpan"></see>s. 
        /// Using this code you can do: <c>TimeSpan time = 2.Days() + 10.Hours() + 12.Minutes() + 20.Seconds() + 890.Milliseconds();</c>
        /// </summary>
        /// <param name="ms">The extended integer to transform into milliseconds</param>
        /// <returns>The desired <see cref="TimeSpan"></see></returns>
        public static TimeSpan Milli(this int ms) => TimeSpan.FromMilliseconds(ms);

        /// <summary>Returns the <paramref name="date"/>'s previous day (considers only the date component)</summary>
        /// <param name="date">The date</param>
        /// <returns>The day before the given date without considering any time component</returns>
        public static DateTime PreviousDay(this DateTime date) => date.Date.AddDays(-1);

        /// <summary>Gets the first day of the month after the given <paramref name="date"/></summary>
        /// <param name="date">Reference date</param>
        /// <returns>A <see cref="DateTime"/> representing the zero (00:00) hour of the first day of the month after <paramref name="date"/></returns>
        public static DateTime NextMonth(this DateTime date) => date.FirstDayOfMonth().AddMonths(1);

        /// <summary>Gets the <see cref="DateTime"/> of the first day of the month prior to <paramref name="date"/></summary>
        /// <param name="date">Reference date</param>
        /// <returns>A <see cref="DateTime"/> representing the zero (00:00) hour of the first day of the month prior to <paramref name="date"/></returns>
        public static DateTime PreviousMonth(this DateTime date) => date.FirstDayOfMonth().AddMonths(-1);

        /// <summary>Gets the last day of the month for the reference <paramref name="date"/></summary>
        /// <param name="date">Reference date</param>
        /// <returns>A <see cref="DateTime"/> representing the zero (00:00) hour of the last day of the month for the reference <paramref name="date"/></returns>
        public static DateTime LastDayOfMonth(this DateTime date) => date.NextMonth().AddDays(-1);

        /// <summary>Gets the count of days in the month referenced by <paramref name="date"/></summary>
        /// <param name="date">Reference date</param>
        /// <returns>Count of days in the month</returns>
        public static int DaysInMonth(this DateTime date) => date.LastDayOfMonth().Day;

        /// <summary>Gets the first day of the month referenced by <paramref name="date"/></summary>
        /// <param name="date">Reference date</param>
        /// <returns>A <see cref="DateTime"/> representing the zero (00:00) hour of the first day of the month for the reference <paramref name="date"/></returns>
        public static DateTime FirstDayOfMonth(this DateTime date) => new(date.Year, date.Month, 1);

        /// <summary>Gets the first day of the year referenced by <paramref name="date"/></summary>
        /// <param name="date">Reference date</param>
        /// <returns>A <see cref="DateTime"/> representing the zero (00:00) hour of the first day of the year for the reference <paramref name="date"/></returns>
        public static DateTime StartOfTheYear(this DateTime date) => new(date.Year, 1, 1);

        /// <summary> Obtêm as datas de início de fim da primeira semana do mês de dada data </summary>
        /// <param name="date">Data que fornecerá o mês para o qual quero obtyer a primeira semana</param>
        /// <param name="weekStart">Primeiro dia da semana</param>
        /// <returns>Tupla contendo data de início e fim da semana</returns>
        public static (DateTime start, DateTime end) FirstWeekOfMonth(this DateTime date, DayOfWeek weekStart = DayOfWeek.Sunday) {
            DateTime startOfMonth = date.FirstDayOfMonth();
            int dif = startOfMonth.DayOfWeek - weekStart;
            DateTime start = startOfMonth.AddDays(-dif);
            DateTime end = start.AddDays(6);
            return (start, end);
        }

        /// <summary>Returns the first day of the week for the reference <paramref name="date"/> considering 
        /// the starting day of the week passed in <paramref name="weekStart"/></summary>
        /// <param name="date">Reference date</param>
        /// <param name="weekStart">The day the week starts (Sunday thru Saturday). Defaults to Sunday.</param>
        /// <returns>A <see cref="DateTime"/> representing the first day of the week referenced by <paramref name="date"/></returns>
        /// <remarks>Same as <see cref="FirstDayOfWeek(DateTime, DayOfWeek)"/></remarks>
        public static DateTime StartOfWeek(this DateTime date, DayOfWeek weekStart = DayOfWeek.Sunday) {
            int diff = date.DayOfWeek - weekStart;
            if (diff < 0)
                diff += 7;
            return date.AddDays(-diff).Date;
        }
        /// <summary>Returns the first day of the week for the reference <paramref name="date"/> considering 
        /// the starting day of the week passed in <paramref name="weekStart"/></summary>
        /// <param name="date">Reference date</param>
        /// <param name="weekStart">The day the week starts (Sunday thru Saturday). Defaults to Sunday.</param>
        /// <returns>A <see cref="DateTime"/> representing the first day of the week referenced by <paramref name="date"/></returns>
        /// <remarks>Same as <see cref="StartOfWeek(DateTime, DayOfWeek)"/></remarks>
        public static DateTime FirstDayOfWeek(this DateTime date, DayOfWeek weekStart = DayOfWeek.Sunday) => StartOfWeek(date, weekStart);

        /// <summary>Returns the last day of the week for the reference <paramref name="date"/> considering 
        /// the starting day of the week passed in <paramref name="weekStart"/></summary>
        /// <param name="date">Reference date</param>
        /// <param name="weekStart">The day the week starts (Sunday thru Saturday). Defaults to Sunday.</param>
        /// <returns>A <see cref="DateTime"/> representing the last day of the week referenced by <paramref name="date"/></returns>
        public static DateTime LastDayOfWeek(this DateTime date, DayOfWeek weekStart = DayOfWeek.Sunday) {
            date = date.Date;
            int dif = date.DayOfWeek - weekStart;
            var start = date.AddDays(-dif);
            var end = start.AddDays(6);
            return end;
        }

        /// <summary>Returns a <see cref="DateTime"/> representing the day and time before the reference date (<paramref name="dt"/>)</summary>
        /// <param name="dt">Reference date</param>
        /// <returns>A <see cref="DateTime"/> representing the day before <paramref name="dt"/></returns>
        /// <remarks>The time component is not stripped, so <c>2023/12/15 10:50:00</c> will result in <c>2023/12/14 10:50:00</c></remarks>
        public static DateTime Previous(this DateTime dt) => dt.Date.AddDays(-1);
        /// <summary>Returns a <see cref="DateTime"/> representing the day and time after the reference date (<paramref name="dt"/>)</summary>
        /// <param name="dt">Reference date</param>
        /// <returns>A <see cref="DateTime"/> representing the day after<paramref name="dt"/></returns>
        /// <remarks>The time component is not stripped, so <c>2023/12/15 10:50:00</c> will result in <c>2023/12/16 10:50:00</c></remarks>
        public static DateTime Next(this DateTime dt) => dt.Date.AddDays(1);

    }
}
