using System;
#if NETFRAMEWORK
using System.Reflection;
#endif

namespace Loudenvier.FastFileEnumerator.Benchmarking
{
    /// <summary>
    /// .NET Framework 4.7.2 does not expose GC.GetAllocatedBytesForCurrentThread
    /// in its reference assemblies. If the process is running on a newer CLR
    /// that has the API, discover it once; otherwise report N/A rather than
    /// presenting the unrelated GC.GetTotalMemory delta as allocated bytes.
    /// </summary>
    internal static class AllocationCounter
    {
#if NETFRAMEWORK
        private static readonly Func<long>? ReadBytes = FindCounter();

        private static Func<long>? FindCounter()
        {
            MethodInfo? method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",
                BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (method == null) return null;
            return (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
        }

        public static long Read() => ReadBytes == null ? -1 : ReadBytes();
#else
        public static long Read() => GC.GetAllocatedBytesForCurrentThread();
#endif
    }
}
