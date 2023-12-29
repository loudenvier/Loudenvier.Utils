using System;

namespace Loudenvier.Utils
{
    public static class ConvertExtensions
    {
        /// <summary>
        /// Converts the <see cref="object"/> passed in <paramref name="o"/> into the type specified by <typeparamref name="T"/>,
        /// taking into account the possibilities of <see cref="null"/>, <see cref="DBNull"/> or <see cref="string.Empty"/> 
        /// (somewhat useful in data reading contexts)
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to convert to</typeparam>
        /// <param name="o">The object to convert</param>
        /// <returns>The <paramref name="o"/> converted to type <typeparamref name="T"/></returns>
        public static T? ConvertTo<T>(this object o, T? defaultValue = default) {
            if (o is null || o == DBNull.Value || string.Empty.Equals(o))
                return defaultValue;
            Type t = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(o, t);
        }

        /// <summary>
        /// Converts the <see cref="object"/> passed in <paramref name="o"/> into the type specified by <typeparamref name="T"/>,
        /// taking into account the possibilities of <see cref="null"/>, <see cref="DBNull"/> or <see cref="string.Empty"/> 
        /// (somewhat useful in data reading contexts)
        /// </summary>
        /// <remarks>This is a shorthand for <see cref="ConvertTo{T}(object, T)"/></remarks>
        /// <typeparam name="T">The <see cref="Type"/> to convert to</typeparam>
        /// <param name="o">The object to convert</param>
        /// <returns>The <paramref name="o"/> converted to type <typeparamref name="T"/></returns>
        public static T? To<T>(this object o, T? defaultValue = default) => ConvertTo(o, defaultValue);

    }
}
