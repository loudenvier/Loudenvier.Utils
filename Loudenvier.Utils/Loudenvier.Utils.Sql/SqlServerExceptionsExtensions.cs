using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace Loudenvier.Utils
{
    public static class SqlServerExceptionsExtensions 
    {
       

        /// <summary>Checks if <paramref name="ex"/> an exception with a SQL Error matching 
        /// one of the <paramref name="errors"/> passed to the method.</summary>
        /// <param name="ex">The exception to inspect.</param>
        /// <param name="errors">A list of SQL error codes.</param>
        /// <returns><c>true</c> if the exception <paramref name="ex"/> contains 
        /// one of the <paramref name="errors"/> provided.</returns>
        public static bool IsOneOfTheseSqlErrors(this Exception ex, params int[] errors) => ex switch {
            SqlException sqlEx when sqlEx.Errors
                .OfType<SqlError>()
                .Any(e => e.Number.In(errors)) 
                => true,
            _ => false
        };

        /// <summary>Checks if <paramref name="ex"/> is or contains an inner exception with an
        /// SQL Error matching one of the <paramref name="errors"/> passed to the method.</summary>
        /// <param name="ex">The exception (and inner exceptions) to inspect.</param>
        /// <param name="errors">A list of SQL error codes.</param>
        /// <returns><c>true</c> if the exception <paramref name="ex"/> contains or has itself 
        /// one of the <paramref name="errors"/> provided.</returns>
        public static bool HasOneOfTheseSqlErrors(this Exception ex, params int[] errors) {
            if (ex.IsOneOfTheseSqlErrors(errors)) return true;
            while ((ex = ex!.InnerException) is not null)
                if (ex.IsOneOfTheseSqlErrors(errors))
                    return true;
            return false;
        }

        /// <summary>Checks if <paramref name="ex"/> is or contains an inner exception representing
        /// a duplicate key violation either for a primary key or a unique constraint.</summary>
        /// <remarks>This code only works with MS SQL Server. New engines could be added.</remarks>
        /// <param name="ex">The exception (and inner exceptions) to inspect for key violations.</param>
        /// <returns><c>true</c> if the exception <paramref name="ex"/> contains or is itself a SQL Server key violation.</returns>
        public static bool HasDuplicateKeyViolation(this Exception ex)
            => ex.HasOneOfTheseSqlErrors(SqlErrors.DuplicateKeyViolations);

        /// <summary>Checks if <paramref name="ex"/> is a primary key violation.</summary>
        /// <remarks>This code only works with MS SQL Server. New engines could be added.</remarks>
        /// <param name="ex">The exception (and inner exceptions) to inspect for primary key violation.</param>
        /// <returns><c>true</c> if the exception <paramref name="ex"/> contains or is itself a SQL Server 
        /// primary key violation.</returns>
        public static bool HasPrimaryKeyViolation(this Exception ex)
            => ex.HasOneOfTheseSqlErrors(SqlErrors.PRIMARY_KEY_VIOLATION);

        /// <summary>Checks if <paramref name="ex"/> is a UNIQUE CONSTRAINT violation.</summary>
        /// <remarks>This code only works with MS SQL Server. New engines could be added.</remarks>
        /// <param name="ex">The exception (and inner exceptions) to inspect for UNIQUE CONSTRAINT violation.</param>
        /// <returns><c>true</c> if the exception <paramref name="ex"/> contains or is itself a SQL Server 
        /// UNIQUE CONSTRAINT violation.</returns>
        public static bool HasUniqueConstraintViolation(this Exception ex)
            => ex.HasOneOfTheseSqlErrors(SqlErrors.UNIQUE_CONSTRAINT_VIOLATION);

        static bool TryFindFirstImmediateError(Exception ex, int[] errors, out SqlError? result) {
            result = null;
            if (ex is SqlException sqlEx)
                result = sqlEx.Errors.OfType<SqlError>().FirstOrDefault(e => e.Number.In(errors));
            return result != null;
        }

        public static SqlError? FindFirstError(this Exception ex, params int[] errors) {
            if (TryFindFirstImmediateError(ex, errors, out SqlError? found)) 
                return found;
            while ((ex = ex!.InnerException) is not null)
                if (TryFindFirstImmediateError(ex, errors, out found))
                    return found;
            return null;
        }
    }
}
