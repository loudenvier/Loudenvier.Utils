using System;
using System.Data.SqlClient;
using System.Linq;

namespace Loudenvier.Utils
{
    public static class SqlServerExtensions 
    {
        const int MSSQL_PRIMARY_KEY_VIOLATION = 2601;
        const int MSSQL_UNIQUE_CONSTRAINT_VIOLATION = 2627;

        /// <summary>
        /// Check to see if an exception represents a unique key violation (either unique key, unique constraint).
        /// </summary>
        /// <param name="ex"></param>
        /// <returns><c>true</c> if the exception <paramref name="ex"/> represents a unique key violation, false otherwise.</returns>
        static bool IsDupKeyExInternal(Exception ex) => ex switch {
            SqlException sqlEx when sqlEx.Errors.OfType<SqlError>()
                .Any(e => e.Number.IsOneOf(
                    MSSQL_PRIMARY_KEY_VIOLATION,
                    MSSQL_UNIQUE_CONSTRAINT_VIOLATION))
                => true,
            _ => false
        };

        /// <summary>Checks if <paramref name="ex"/> is or contains an inner exception representing
        /// a duplicate key violation either for a primary key or a unique constraint.</summary>
        /// <remarks>This code only works with MS SQL Server. New engines could be added</remarks>
        /// <param name="ex">The exception (and inner exceptions) to inspect for key violations</param>
        /// <returns><c>true</c> if the exception <paramref name="ex"/> contains or is itself a SQL Server key violation</returns>
        public static bool IsDuplicateKeyException(this Exception ex) {
            if (IsDupKeyExInternal(ex))
                return true;
            while ((ex = ex!.InnerException) is not null)
                if (IsDupKeyExInternal(ex))
                    return true;
            return false;
        }

    }
}
