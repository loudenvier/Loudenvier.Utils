using System.Linq;
using System.Runtime.CompilerServices;

namespace Loudenvier.Utils
{
    /// <summary>Provides <c>{T}.In</c> methods for various types</summary>
    public static class IsOneOfExtensions {
        /// <summary>
        /// This extension method checks if <paramref name="item"/> is one of the <paramref name="items"/>
        /// passed by using the type <typeparamref name="T"/>'s default equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to test.</typeparam>
        /// <param name="item">The item to check.</param>
        /// <param name="items">A list of items to check.</param>
        /// <returns><c>true</c> if the item exists in the list of items passed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        public static bool In<T>(this T item, params T[] items) => items.Contains(item);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotIn<T>(this T item, params T[] items) => !items.Contains(item);
    }
}
