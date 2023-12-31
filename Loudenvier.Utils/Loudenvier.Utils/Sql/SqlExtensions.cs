using System;
using System.Data.Common;

namespace Codax.Util
{
    /// <summary>
    /// Simple helpers to work with Sql returned data, handling Sql exceptions, connection strings, etc.
    /// </summary>
    public static class SqlExtensions
    {
        /// <summary>
        /// Check to see if an exception represents a unique key violation (either unique key, unique constraint).
        /// </summary>
        /// <param name="ex"></param>
        /// <returns><c>true</c> if the exception <paramref name="ex"/> represents a unique key violation, false otherwise.</returns>
        static bool IsDupKeyExInternal(Exception ex) {
            return ex switch {
                _ => false
            };
        }

        /// <summary>Checks if <paramref name="ex"/> is or contains an inner exception representing
        /// a duplicate key violation (either a primary key or a unique constraint)
        /// </summary>
        /// <remarks>This code only works with MS SQL Server. New engines could be added</remarks>
        /// <param name="ex">The exception (and inner exceptions) to inspect for key violations</param>
        /// <returns><c>true</c> if the exception <paramref name="ex"/> contains or is itself a SQL Server key violation</returns>
        public static bool IsDuplicateKeyException(this Exception ex)
        {
            if (IsDupKeyExInternal(ex)) 
                return true;
            while((ex = ex!.InnerException) is not null) 
               if (IsDupKeyExInternal(ex)) 
                    return true;
            return false;
        }

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
