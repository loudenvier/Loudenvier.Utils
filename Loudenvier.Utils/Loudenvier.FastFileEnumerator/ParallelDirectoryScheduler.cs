using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Loudenvier.IO;

/// <summary>
/// Bounded number of long-lived worker loops; one queued task per directory,
/// not per file. The calling thread is worker zero. No locks on the per-file hot
/// path. A directory's owner exclusively owns its enumeration buffer.
/// </summary>
internal sealed class ParallelDirectoryScheduler
{
    private readonly FastFileEnumerator.Configuration _config;
    private readonly FastEnumerationBackend _backend;
    private readonly FastFileVisitor[] _visitors;
    private readonly Queue<Pending> _queue = new();
    private readonly object _sync = new();
    private int _pending = 1; // includes queued and in-flight directories
    private ExceptionDispatchInfo? _failure;
    private readonly ParallelDirectoryMetrics? _metrics;
    private int _active;

    private readonly struct Pending
    {
        internal readonly string Path;
        internal readonly bool IsRoot;
        internal Pending(string path, bool isRoot) { Path = path; IsRoot = isRoot; }
    }

    private ParallelDirectoryScheduler(FastFileEnumerator.Configuration config,
        FastEnumerationBackend backend, FastFileVisitor[] visitors, ParallelDirectoryMetrics? metrics)
    {
        _config = config;
        _backend = backend;
        _visitors = visitors;
        _metrics = metrics;
        _queue.Enqueue(new Pending(config.Root, true));
    }

    internal static void Run(FastFileEnumerator.Configuration config,
        FastEnumerationBackend backend, FastFileVisitor[] visitors)
    {
        Run(config, backend, visitors, null);
    }

    internal static void Run(FastFileEnumerator.Configuration config, FastEnumerationBackend backend,
        FastFileVisitor[] visitors, ParallelDirectoryMetrics? metrics)
    {
        var engine = new ParallelDirectoryScheduler(config, backend, visitors, metrics);
        if (metrics != null) metrics.SchedulerStartTimestamp = Stopwatch.GetTimestamp();
        var tasks = new Task[visitors.Length - 1];
        int started = 0;
        try
        {
            for (int i = 1; i < visitors.Length; i++)
            {
                int worker = i;
                tasks[started++] = Task.Run(() => engine.Work(worker));
            }
            engine.Work(0);
        }
        catch (Exception ex)
        {
            engine.Fail(ex);
        }
        finally
        {
            // All worker callbacks must finish before a public call can return.
            for (int i = 0; i < started; i++)
                tasks[i].GetAwaiter().GetResult();
        }
        engine._failure?.Throw();
    }

    private void Work(int workerIndex)
    {
        long workerStart = _metrics == null ? 0 : Stopwatch.GetTimestamp();
        ParallelWorkerMetrics? metricForWorker = _metrics?.Workers[workerIndex];
        if (metricForWorker != null)
            metricForWorker.StartDelayTicks = workerStart - _metrics!.SchedulerStartTimestamp;
        try
        {
            FastFileVisitor visitor = _visitors[workerIndex];
            Action<string> addChild = EnqueueChild;
            // Workers without an assigned directory allocate no native buffer.
            // Once active, each worker reuses the same buffer for every directory.
            ParallelDirectoryWorker? scanner = null;
            try
            {
                ParallelWorkerMetrics? metric = _metrics?.Workers[workerIndex];
                if (metric != null) metric.ManagedThreadId = Thread.CurrentThread.ManagedThreadId;
                long allocationStart = metric == null ? -1 : ParallelDirectoryMetrics.ThreadAllocationBytes();
                FastFileVisitor effectiveVisitor = metric == null ? visitor :
                    (in FastFileEntry entry) => {
                        if (entry.IsDirectory) metric.DirectoryEntries++; else metric.Files++;
                        visitor(in entry);
                    };
                try
                {
                    while (TryTake(out Pending work, metric))
                    {
                        try
                        {
                            scanner ??= new ParallelDirectoryWorker(_config, _backend);
                            if (metric != null)
                            {
                                metric.DirectoriesScanned++;
                                if (_backend == FastEnumerationBackend.WindowsNt)
                                    metric.ReservedNativeBytes = _config.BufferSize;
                                long start = Stopwatch.GetTimestamp();
                                try { scanner.Scan(work.Path, work.IsRoot, effectiveVisitor, addChild); }
                                finally { metric.ScanTicks += Stopwatch.GetTimestamp() - start; }
                            }
                            else scanner.Scan(work.Path, work.IsRoot, visitor, addChild);
                        }
                        finally { Finished(); }
                    }
                }
                finally
                {
                    if (metric != null)
                    {
                        long allocated = ParallelDirectoryMetrics.ThreadAllocationBytes();
                        metric.ThreadAllocatedBytes = allocationStart < 0 || allocated < 0 ? -1 : allocated - allocationStart;
                    }
                }
            }
            finally { scanner?.Dispose(); }
        }
        catch (Exception ex) { Fail(ex); }
        finally
        {
            if (metricForWorker != null)
                metricForWorker.LifetimeTicks = Stopwatch.GetTimestamp() - workerStart;
        }
    }

    private bool TryTake(out Pending work, ParallelWorkerMetrics? metric)
    {
        lock (_sync)
        {
            while (_queue.Count == 0 && _pending != 0 && _failure == null)
            {
                if (metric == null) Monitor.Wait(_sync);
                else
                {
                    long start = Stopwatch.GetTimestamp();
                    Monitor.Wait(_sync);
                    metric.WaitTicks += Stopwatch.GetTimestamp() - start;
                }
            }
            if (_failure != null || _pending == 0)
            {
                work = default;
                return false;
            }
            work = _queue.Dequeue();
            if (_metrics != null)
            {
                _active++;
                if (_active > _metrics.PeakActiveWorkers)
                    _metrics.PeakActiveWorkers = _active;
            }
            return true;
        }
    }

    private void EnqueueChild(string path)
    {
        lock (_sync)
        {
            if (_failure != null) return;
            _pending++;
            _queue.Enqueue(new Pending(path, false));
            if (_metrics != null)
            {
                _metrics.TotalEnqueued++;
                if (_queue.Count > _metrics.PeakQueuedDirectories)
                    _metrics.PeakQueuedDirectories = _queue.Count;
            }
            Monitor.Pulse(_sync);
        }
    }

    private void Finished()
    {
        lock (_sync)
        {
            if (_metrics != null) _active--;
            if (--_pending == 0) Monitor.PulseAll(_sync);
        }
    }

    private void Fail(Exception error)
    {
        lock (_sync)
        {
            if (_failure == null) _failure = ExceptionDispatchInfo.Capture(error);
            Monitor.PulseAll(_sync);
        }
    }
}
