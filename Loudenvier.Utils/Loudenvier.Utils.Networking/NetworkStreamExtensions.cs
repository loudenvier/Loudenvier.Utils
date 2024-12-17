using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using System.IO;

namespace Loudenvier.Utils;

public static class NetworkStreamExtensions {

    /// <summary>Efficiently waits for some data to arrive in the NetworkStream. 
    /// Only performs the waiting if <paramref name="timeout"/> is not <c>null</c>).</summary>
    /// <remarks>This method configures the <paramref name="stream"/> <see cref="NetworkStream.ReadTimeout"/> to the 
    /// expected "timeout" and performs a zero-byte read, which actually only waits for data to arrive on the internal 
    /// read buffer, but respects the timeout used. After the read completes it resets the read timeout to its original value</remarks>
    /// <param name="stream">The network stream to wait upon</param>
    /// <param name="timeout">The timeout to wait for data (if its <c>null</c> it will rerturn without waiting at all)</param>
    /// <exception cref="TimeoutException">If a timeout was provided and no data arrived on the <paramref name="stream"/>
    /// in the alloted time</exception>
    public static void WaitForData(this NetworkStream stream, TimeSpan? timeout) {
        if (timeout is null) return;
        int originalReadTimeout = stream.ReadTimeout;
        stream.ReadTimeout = (int)timeout.Value.TotalMilliseconds;
        // performs a zero-byte read, e.g., just waits for the setup read timeout
        _ = stream.Read([], 0, 0);
        stream.ReadTimeout = originalReadTimeout;
        // bellow is the old, inneficient way we did it in the past...
        /*if (timeout.HasValue && !System.Threading.SpinWait.SpinUntil(() => stream.DataAvailable, timeout.Value))
            throw new TimeoutException($"Timeout expired waiting for data. The timeout was {timeout}");*/
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>We don't check how many bytes were actually read at the end! The reading loop can exit if <c>NetworkStream.Read</c> 
    /// returns 0 prior to reaching length, indicating the connection was closed or data transfer was stopped (it won't
    /// exit before reaching length if data was still being received)</remarks>
    /// <param name="stream"></param>
    /// <param name="length"></param>
    /// <param name="startTimeout"></param>
    /// <returns></returns>
    public static ReadOnlySpan<byte> ReadBytes(this NetworkStream stream, int length, TimeSpan? startTimeout = null) {
        stream.WaitForData(startTimeout);
        var writer = new ArrayPoolBufferWriter<byte>();
        int readSize = -1;
        while (readSize != 0 && writer.WrittenCount <= length) {
            var buffer = writer.GetSpan(length - writer.WrittenCount);
            readSize = stream.Read(buffer);
            writer.Advance(readSize);
        }
        return writer.WrittenSpan;
    }

    public static async Task<ReadOnlyMemory<byte>> ReadBytesAsync(this NetworkStream stream, int length, TimeSpan? startTimeout = null) {
        stream.WaitForData(startTimeout);
        var writer = new ArrayPoolBufferWriter<byte>();
        int readSize = -1;
        while (readSize != 0 && writer.WrittenCount <= length) {
            var buffer = writer.GetMemory(length - writer.WrittenCount);
            readSize = await stream.ReadAsync(buffer).ConfigureAwait(false);
            writer.Advance(readSize);
        }
        return writer.WrittenMemory;
    }

    const int DEFAULT_BUFFER_SIZE = 8192; // same as TcpClient, Socket, etc.

    public static ReadOnlySpan<byte> ReadToEOM(this NetworkStream stm, byte EOM, TimeSpan? startTimeout = null, 
        int bufferSize = DEFAULT_BUFFER_SIZE) {
        stm.WaitForData(startTimeout);
        var writer = new ArrayPoolBufferWriter<byte>();
        int readSize = -1, eomPos = -1;
        while (readSize != 0 && eomPos < 0) {
            var buffer = writer.GetSpan(bufferSize);
            readSize = stm.Read(buffer);
            eomPos = buffer.IndexOf(EOM);
            if (eomPos >= 0)
                readSize = eomPos + 1;
            writer.Advance(readSize);
        }
        return writer.WrittenSpan;
    }

    public static ReadOnlySpan<byte> ReadUntilTimeout(this NetworkStream stream, 
        TimeSpan? startTimeout = null,
        TimeSpan? readTimeout = null,
        int bufferSize = DEFAULT_BUFFER_SIZE
        ) {
        stream.ReadTimeout = (int?)readTimeout?.TotalMilliseconds ?? stream.ReadTimeout;
        // if no data arrives within the start timeout a SocketException will be thrown!
        // ReadTimeout is automatically reset inside the wait bellow after it completes
        stream.WaitForData(startTimeout);
        var writer = new ArrayPoolBufferWriter<byte>();
        int readSize = -1;
        try {
            while (readSize != 0) {
                var buffer = writer.GetSpan(bufferSize);
                readSize = stream.Read(buffer);
                writer.Advance(readSize);
            }
        } catch(IOException ioe) when (ioe.InnerException is SocketException soe) {
            // ignores read timeout errors since that's what we want: read until closed or no more data available
            if (soe.SocketErrorCode != SocketError.TimedOut)
                throw soe;
        }
        return writer.WrittenSpan;
    }
}
