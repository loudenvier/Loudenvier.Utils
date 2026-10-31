using System;
using System.Runtime.InteropServices;

namespace Loudenvier.IO;

internal sealed class ParallelDirectoryWorker : IDisposable
{
    private readonly FastFileEnumerator.Configuration _configuration;
    private readonly FastEnumerationBackend _backend;
    private IntPtr _ntBuffer;

    internal ParallelDirectoryWorker(FastFileEnumerator.Configuration configuration,
        FastEnumerationBackend backend)
    {
        _configuration = configuration;
        _backend = backend;
        if (backend == FastEnumerationBackend.WindowsNt)
        {
            _ntBuffer = Marshal.AllocHGlobal(configuration.BufferSize);
            if (_ntBuffer == IntPtr.Zero) throw new OutOfMemoryException();
        }
    }

    internal void Scan(string directory, bool isRoot, FastFileVisitor visitor, Action<string> addChild)
    {
        switch (_backend)
        {
            case FastEnumerationBackend.WindowsNt:
                WindowsNtFileEnumerator.ScanDirectory(_configuration, directory, isRoot,
                    _ntBuffer, visitor, addChild);
                break;
            case FastEnumerationBackend.Bcl:
#if NET6_0_OR_GREATER
                BclFileEnumerator.ScanDirectory(_configuration, directory, visitor, addChild);
                break;
#else
                throw new PlatformNotSupportedException("BCL requires .NET 6+.");
#endif
            case FastEnumerationBackend.Portable:
                PortableFileEnumerator.ScanDirectory(_configuration, directory, isRoot, visitor, addChild);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(_backend));
        }
    }

    public void Dispose()
    {
        if (_ntBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_ntBuffer);
            _ntBuffer = IntPtr.Zero;
        }
    }
}
