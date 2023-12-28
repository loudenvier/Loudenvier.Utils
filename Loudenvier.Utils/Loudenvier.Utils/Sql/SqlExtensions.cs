using System;
using System.Data.Common;

namespace Codax.Util
{
    /// <summary>
    /// Simple helpers to work with Sql returned data, handling Sql exceptions, connection strings, etc.
    /// </summary>
    public static class SqlExtensions
    {
        /// <summary>Checks if <paramref name="ex"/> is or contains an inner exception representing
        /// a duplicate key violation (either a primary key or a 
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static bool IsDuplicateKeyException(this Exception ex)
        {
            return (ex.Message.Contains("duplicate key"));
        }

        /// <summary>Converts a value to <typeparamref name="T"/> considering <see cref="null"/>, 
        /// <see cref="DBNull"/> and <see cref="string.Empty"/> as null values which results in <paramref name="defaultValue"/>
        /// </summary>
        /// <typeparam name="T">The type to convert <paramref name="o"/> to</typeparam>
        /// <param name="o">The object/value to be converted</param>
        /// <param name="defaultValue">The default value to use in case the object is either null, <c>DbNull</c> an empty string</param>
        /// <returns>The converted value of the default value passed if its considered null</returns>
        public static T ConvertTo<T>(this object o, T defaultValue=default) {
            if (o is null || o == DBNull.Value || string.Empty.Equals(o))
                return defaultValue;
            Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(o, t);
        }

        /// <summary>Converts a value to <typeparamref name="T"/> considering <see cref="null"/>, 
        /// <see cref="DBNull"/> and <see cref="string.Empty"/> as null values which results in <paramref name="defaultValue"/>
        /// </summary>
        /// <remarks>This is a shorthand for <see cref="ConvertTo{T}(object, T)"/></remarks>
        /// <typeparam name="T">The type to convert <paramref name="o"/> to</typeparam>
        /// <param name="o">The object/value to be converted</param>
        /// <param name="defaultValue">The default value to use in case the object is either null, <c>DbNull</c> an empty string</param>
        /// <returns>The converted value of the default value passed if its considered null</returns>
        public static T To<T>(this object o, T defaultValue = default) => ConvertTo(o, defaultValue);

        /// <summary>Redacts a password from a connection string with a provided <paramref name="redact"/> (defaults to "*****").
        /// This is useful for logging connections strings without passwords.</summary>
        /// <param name="connStr">The connection string which passwords should be redacted</param>
        /// <param name="redact">The value to use for redacting the password</param>
        /// <returns>A connection string with any password redacted</returns>
        public static string RemovePassword(this string connStr, string redact = "*****") {
            var builder = new DbConnectionStringBuilder { ConnectionString = connStr };
            if (builder.ContainsKey("password"))
                builder["password"] = redact;
            if (builder.ContainsKey("pwd"))
                builder["pwd"] = redact;
            return builder.ToString();
        }

    }
}
