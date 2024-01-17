using System;
using System.Data.Common;

namespace Loudenvier.Utils
{
    /// <summary>
    /// Simple helpers to work with Sql returned data, handling Sql exceptions, connection strings, etc.
    /// </summary>
    public static class SqlExtensions
    {
      
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
