using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Loudenvier.Utils
{
    /// <summary>Provides <c>{T}.IsOneOf</c> methods for various types</summary>
    public static class IsOneOfExtensions {
        /// <summary>
        /// This extension method checks if <paramref name="item"/> is of of the <paramref name="items"/>
        /// passed by using the type <typeparamref name="T"/>'s default equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to test.</typeparam>
        /// <param name="item">The item to check.</param>
        /// <param name="items">A list of items to check.</param>
        /// <returns><c>true</c> if the item is one of the items passed.</returns>
        public static bool IsOneOf<T>(this T item, params T[] items) => items.Contains(item);
    }
}
