using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Loudenvier.Utils;

public static class NetworkStreamExtensions
{
    public struct PortState
    {
        public PortState(bool ok, string msg, IPEndPoint endpoint) : this() {
            Open = ok;
            CheckMessage = msg;
            EndPoint = endpoint;
        }
        public bool Open { get; private set; }
        public string CheckMessage { get; private set; }
        public IPEndPoint EndPoint { get; private set; }
    }

    public static PortState CheckPortState(string ipOrHost, int port, TimeSpan? timeout = null) {
        var ip = ipOrHost.ToIPAddress() ?? throw new ArgumentException(nameof(ipOrHost), "Invalid IP or Host: " + ipOrHost);
        var endpoint = new IPEndPoint(ip, port);
        return endpoint.CheckPortState(timeout);
    }

    public static PortState CheckPortState(this IPEndPoint endpoint, TimeSpan? timeout = null) {
        timeout ??= TimeSpan.FromSeconds(5);
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        try {
            var result = socket.BeginConnect(endpoint, null, socket);
            try {
                var ok = result.AsyncWaitHandle.WaitOne(timeout.Value);
                if (ok) {
                    socket.EndConnect(result);
                    return new PortState(true, string.Format("Sucesso conectando a [{0}]", endpoint), endpoint);
                }
                return new PortState(false,
                    string.Format("Tempo máximo de conexão ({1}) excedido ao conectar a [{0}]", endpoint, timeout.Value), endpoint);
            } finally {
                try { socket.Close(); } catch { }
            }
        } catch (SocketException e) {
            string errorMsg = string.Format("{0} ({1})", e.Message, e.SocketErrorCode.ToString());
            if (e.SocketErrorCode == SocketError.ConnectionRefused)
                errorMsg = "A porta do servidor aparentemente está fechada. " + errorMsg;
            return new PortState(false, string.Format(
                "Falha conectando a [{0}]: {1} ", endpoint, errorMsg),
                endpoint);
        }
    }

    /// <summary>
    /// Reads exactly <paramref name="len"/> bytes from the <see cref="NetworkStream"/> using a
    /// per "chunk" sliding timeout of <paramref name="timeoutMillis"/> milliseconds, employing
    /// a very efficient algorithm that prevents all common pitfalls of TCP/IP communication. 
    /// </summary>
    /// <remarks>
    /// If data arrives in more than one chunk the timeout gets renewed at each read and only 
    /// affects the initial arrival of bytes, since the socket will return whatever amount of data 
    /// it has already read from the network stack almost immediately.
    /// This is in contrast with <seealso cref="Stream.ReadExactlyAsync"/> (available from
    /// .NET 7 onwards) that applies the timeout to the entire read operation, which may not be 
    /// the most efficient way to handle binary and legacy TCP protocols.
    /// This is also a better approach than using blocking reads with Receive Timeouts.
    /// </remarks>
    /// <param name="stm">The underlying stream to read data from</param>
    /// <param name="len">The number of bytes expected to be read from the stream</param>
    /// <param name="timeoutMillis">The sliding, per "chunk", read timeout in milliseconds</param>
    /// <param name="cancellationToken">An optional cancellation token to allow external cancellation of the operation</param>
    /// <returns>A byte array containing exactly the requested number of bytes.</returns>
    /// <exception cref="EndOfStreamException">The stream was closed before reading all the expected bytes (partial reads are failures)</exception>
    /// <exception cref="TimeoutException">The sliding timeout expired before reading all the expected bytes</exception>
    public static async Task<byte[]> ReadBytesAsync(
        this NetworkStream stm,
        int len,
        int timeoutMillis = 500,
        CancellationToken cancellationToken = default) {
        byte[] buffer = new byte[len];
        int bytesReceived = 0;
        
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try {
            while (bytesReceived < len) {
                // Restart the timeout at each chunk
                linkedCts.CancelAfter(timeoutMillis);
                // ReadAsync returns as soon as the first byte arrives and reads up to the bytes requested (may return sooner!)
                int readSize = await stm.ReadAsync(buffer, bytesReceived, len - bytesReceived, linkedCts.Token).ConfigureAwait(false);
                if (readSize == 0)
                    throw new EndOfStreamException($"Stream closed unexpectedly. Expected {len} bytes, received {bytesReceived}.");
                bytesReceived += readSize;
            }
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            // If the caller didn't cancel, then it was an idle timeout
            throw new TimeoutException($"Timeout waiting for data ({timeoutMillis}ms).");
        }
        return buffer;
    }

    /// <summary>
    /// Implements the best possible asynchronous algorithm to read bytes from a <see cref="NetworkStream"/>
    /// when the protocol doesn't define STX/ETX chars, nor payload length. 
    /// </summary>  
    /// <remarks>
    /// You should avoid this whenever possible as it will take at best the shorter <paramref name="timeoutMillis"/> 
    /// wait time to detect "end of transmission" scenarios and at worst the longer <paramref name="firstPacketTimeout"/> 
    /// wait time to detect a "no data arrived" scenario. In case it's impossible to avoid such protocols
    /// this method makes the best effort to be as performant as possible and to bail out efficiently when data
    /// stops arriving over the network (end of transmission).
    /// </remarks>
    /// <param name="stm">The underlying stream to read data from</param>
    /// <param name="timeoutMillis">The inter-chunk timeout (should be as small as possibly allowed by the underlying hardware/protocol)</param>
    /// <param name="firstPacketTimeout">The time to wait for data to start arriving (should be larger, but sensible)</param>
    /// <param name="cancellationToken">An optional cancellation token to allow external cancellation of the operation</param>
    /// <returns>A byte array with the data read. The length of the array matches exactly the number of bytes read from the network.</returns>
    public static async Task<byte[]> ReadUntilNoMoreDataAsync(
        this NetworkStream stm,
        int timeoutMillis = 500,
        int firstPacketTimeout = 5000,
        CancellationToken cancellationToken = default) {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(32768);

        using var ms = new MemoryStream();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try {
            bool firstPacket = true;
            while (true) {
                // Uses long (waiting hardware to wakeup/start transmission) or short (inter-chunks, silence 'is' ETX)
                int currentTimeout = firstPacket ? firstPacketTimeout : timeoutMillis;
                linkedCts.CancelAfter(currentTimeout);

                int readSize = await stm.ReadAsync(buffer, 0, buffer.Length, linkedCts.Token).ConfigureAwait(false);

                if (readSize == 0) 
                    // Remote end closed Stream gracefully (also considered ETX)
                    break;

                ms.Write(buffer, 0, readSize);
                firstPacket = false;
            }
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            // If caller didn't cancel then the read timeout expired. This 'is' ETX (End of Transmission)
            // It's a success condition: we swallow the exception!
            // We may have read 0 bytes which will result in an empty array.
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return ms.ToArray();
    }
}
