using System;

namespace Loudenvier.Utils
{
    public static class DateAndTimeConversionExtensions {

        /// <summary>Same as UNIX and Java's EPOCH</summary>
        static readonly DateTime epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Converts a <see cref="long"/> integral value since Unix/Java's epoch (1970/1/1 00:0)) in a .NET <see cref="DateTime"/>
        /// in the GMT time reference</summary>
        /// <param name="millisecondsSince12AM1970">Milliseconds since Unix/Java's epoch (1/1/1970 00:00)</param>
        /// <returns>A <see cref="DateTime"/> representing the Unix/Java time</returns>
        /// <remarks>Java represents milliseconds since epoch, while UNIX normally represents seconds since epoch</remarks>
        public static DateTime JavaTimeToUtcDateTime(this long millisecondsSince12AM1970)
            => epoch + TimeSpan.FromMilliseconds(millisecondsSince12AM1970);

        /// <summary>Converts a <see cref="long"/> integral value since Unix/Java's epoch (1970/1/1 00:0)) in a .NET <see cref="DateTime"/>
        /// in the Local time reference</summary>
        /// <param name="millisecondsSince12AM1970">Milliseconds since Unix/Java's epoch (1/1/1970 00:00)</param>
        /// <returns>A <see cref="DateTime"/> representing the Unix/Java time</returns>
        /// <remarks>Java represents milliseconds since epoch, while UNIX normally represents seconds since epoch</remarks>
        public static DateTime JavaTimeToLocalDateTime(this long millisecondsSince12AM1970)
            => millisecondsSince12AM1970.JavaTimeToUtcDateTime().ToLocalTime();

        /// <summary>Converts a <see cref="long"/> integral value since Unix/Java's epoch (1970/1/1 00:0)) in a .NET <see cref="DateTime"/>
        /// in the GMT time reference</summary>
        /// <param name="secondsSince12AM1970">Seconds since Unix/Java's epoch (1/1/1970 00:00)</param>
        /// <returns>A <see cref="DateTime"/> representing the Unix/Java time</returns>
        /// <remarks>Java represents milliseconds since epoch, while UNIX normally represents seconds since epoch</remarks>
        public static DateTime UNIXTimeToUtcDateTime(this long secondsSince12AM1970)
            => (secondsSince12AM1970 * 1000).JavaTimeToUtcDateTime();

        /// <summary>Converts a <see cref="long"/> integral value since Unix/Java's epoch (1970/1/1 00:0)) in a .NET <see cref="DateTime"/>
        /// in the Local time reference</summary>
        /// <param name="secondsSince12AM1970">Seconds since Unix/Java's epoch (1/1/1970 00:00)</param>
        /// <returns>A <see cref="DateTime"/> representing the Unix/Java time</returns>
        /// <remarks>Java represents milliseconds since epoch, while UNIX normally represents seconds since epoch</remarks>
        public static DateTime UNIXTimeToLocalDateTime(this long secondsSince12AM1970) => secondsSince12AM1970.UNIXTimeToUtcDateTime().ToLocalTime();

        /// <summary>Converts a <see cref="DateTime"/> to it's UNIX time representation (seconds since 1970)</summary>
        /// <param name="dth">The <see cref="DateTime"/> to be converted</param>
        /// <returns>A UNIX time equivalent to <paramref name="dht"/> (seconds since 1970)</returns>
        public static long ToUNIXTime(this DateTime dth) {
            if (dth.Kind == DateTimeKind.Local)
                dth = dth.ToUniversalTime();
            return (long)(dth - epoch).TotalSeconds;
        }

        /// <summary>Converts a <see cref="DateTimeOffset"/> to it's UNIX time representation (seconds since 1970)</summary>
        /// <param name="dth">The <see cref="DateTimeOffset"/> to be converted</param>
        /// <returns>A UNIX time equivalent to <paramref name="dht"/> (seconds since 1970)</returns>
        public static long ToUNIXTime(this DateTimeOffset dth) => (long)(dth - epoch).TotalSeconds;

        /// <summary>Converts a <see cref="DateTime"/> to it's Java's time representation (milliseconds since 1970)
        /// </summary>
        /// <param name="dth">The <see cref="DateTime"/> to be converted</param>
        /// <returns>A Java's time equivalent to <paramref name="dht"/> (milliseconds since 1970)</returns>
        public static long ToJavaTime(this DateTime dth) {
            if (dth.Kind == DateTimeKind.Local)
                dth = dth.ToUniversalTime();
            return (long)(dth - epoch).TotalMilliseconds;
        }

    }
}
