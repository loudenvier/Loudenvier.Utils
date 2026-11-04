using System;
using System.Reflection;

namespace Loudenvier.IO;

/// <summary>
/// Internal, opt-in scheduler diagnostics. No instrumentation is attached to normal scans.
/// Ticks are Stopwatch ticks, not TimeSpan ticks. Allocation measurements cover only
/// each worker thread while it is running the measured worker loop.
/// </summary>
internal sealed class ParallelDirectoryMetrics
{
    internal readonly ParallelWorkerMetrics[] Workers;
    internal int PeakQueuedDirectories;
    internal int PeakActiveWorkers;
    internal long TotalEnqueued;
    internal long SchedulerStartTimestamp;

    internal ParallelDirectoryMetrics(int degree)
    {
        if (degree < 2) throw new ArgumentOutOfRangeException(nameof(degree));
        Workers = new ParallelWorkerMetrics[degree];
        for (int i = 0; i < degree; i++) Workers[i] = new ParallelWorkerMetrics(i);
        PeakQueuedDirectories = 1;
        TotalEnqueued = 1;
    }

    private static readonly Func<long>? AllocationReader = CreateReader();

    private static Func<long>? CreateReader()
    {
#if NET6_0_OR_GREATER
        return GC.GetAllocatedBytesForCurrentThread;
#else
        MethodInfo? method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",
            BindingFlags.Static | BindingFlags.Public, null, Type.EmptyTypes, null);
        return method == null ? null : (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
#endif
    }

    internal static long ThreadAllocationBytes() => AllocationReader == null ? -1 : AllocationReader();
}

internal sealed class ParallelWorkerMetrics
{
    internal readonly int WorkerIndex;
    internal int ManagedThreadId;
    internal long Files;
    internal long DirectoryEntries;
    internal long DirectoriesScanned;
    internal long StartDelayTicks;
    internal long LifetimeTicks;
    internal long ScanTicks;
    internal long WaitTicks;
    internal long ThreadAllocatedBytes = -1;
    internal long ReservedNativeBytes;

    internal ParallelWorkerMetrics(int index) => WorkerIndex = index;
}
