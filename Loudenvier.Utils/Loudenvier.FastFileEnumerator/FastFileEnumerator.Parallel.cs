using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Loudenvier.IO;

public static partial class FastFileEnumerator
{
    /// <summary>
    /// Enumerates independent directories on multiple workers. The visitor can be
    /// invoked concurrently: it must be thread-safe. Spans are valid only during
    /// the callback on the current worker. Visit order is unspecified.
    /// A degree of 1 calls <see cref="Enumerate"/> directly (no scheduler).
    /// Degree 0 selects up to eight logical processors.
    /// </summary>
    public static void EnumerateParallel(string rootPath, FastFileVisitor visitor,
        FastEnumerationOptions? options = null, int maxDegreeOfParallelism = 0)
    {
        if (visitor == null) throw new ArgumentNullException(nameof(visitor));
        // Common P1 case: literally the existing path, no factory allocation.
        if (maxDegreeOfParallelism == 1 ||
            (maxDegreeOfParallelism == 0 && Environment.ProcessorCount == 1))
        {
            Enumerate(rootPath, visitor, options);
            return;
        }
        EnumerateParallel(rootPath, _ => visitor, options, maxDegreeOfParallelism);
    }

    /// <summary>
    /// A visitor factory invoked once per worker on the calling thread before
    /// enumeration begins. Use it to create independent accumulators without a
    /// per-entry lock or atomic operation. Worker indexes are 0..degree-1.
    /// The supplied visitor must not retain entry spans past its invocation.
    /// </summary>
    public static void EnumerateParallel(string rootPath, Func<int, FastFileVisitor> visitorFactory,
        FastEnumerationOptions? options = null, int maxDegreeOfParallelism = 0)
    {
        if (visitorFactory == null) throw new ArgumentNullException(nameof(visitorFactory));
        if (maxDegreeOfParallelism < 0 || maxDegreeOfParallelism > 256)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism),
                "Degree must be 0 (automatic) or between 1 and 256.");

        // The one-worker case is deliberately identical to the existing public
        // path: no worker queue, no Task, no additional buffer or per-entry checks.
        int degree = maxDegreeOfParallelism == 0
            ? Math.Min(Environment.ProcessorCount, 8) : maxDegreeOfParallelism;
        if (degree == 1)
        {
            FastFileVisitor single = visitorFactory(0) ?? throw new InvalidOperationException("Visitor factory returned null.");
            Enumerate(rootPath, single, options);
            return;
        }

        Configuration config = Prepare(rootPath, options, out FastEnumerationBackend backend);
        if (backend == FastEnumerationBackend.WindowsNt &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("The Windows NT backend requires Windows.");
#if !NET6_0_OR_GREATER
        if (backend == FastEnumerationBackend.Bcl)
            throw new PlatformNotSupportedException("The BCL backend requires .NET 6 or later.");
#endif
        // A nonrecursive scan has only one directory to schedule.
        if (!config.Recursive)
        {
            FastFileVisitor single = visitorFactory(0) ?? throw new InvalidOperationException("Visitor factory returned null.");
            EnumerateCore(config, single, backend);
            return;
        }

        var visitors = new FastFileVisitor[degree];
        for (int i = 0; i < degree; i++)
            visitors[i] = visitorFactory(i) ?? throw new InvalidOperationException("Visitor factory returned null.");
        ParallelDirectoryScheduler.Run(config, backend, visitors);
    }
}
