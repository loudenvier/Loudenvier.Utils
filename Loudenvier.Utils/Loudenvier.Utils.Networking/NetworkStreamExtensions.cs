using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;

namespace Loudenvier.Utils
{
    public static class NetworkStreamExtensions {

        /// <summary>
        /// Wait for some data to arrive in the NetworkStream if a <paramref name="timeout"/> was provided.
        /// </summary>
        /// <param name="stream">The network stream to wait upon</param>
        /// <param name="timeout">The timeout to wait for data (if its <c>null</c> it won't wait nor fail)</param>
        /// <exception cref="TimeoutException">If the timeout was provided and no data arrived on the <paramref name="stream"/></exception>
        public static void WaitForData(this NetworkStream stream, TimeSpan? timeout) {
            if (timeout.HasValue && !System.Threading.SpinWait.SpinUntil(() => stream.DataAvailable, timeout.Value))
                throw new TimeoutException($"Timeout expired waiting for data. The timeout was {timeout}");
        }

        public static ReadOnlySpan<byte> ReadBytes(this NetworkStream stm, int len, TimeSpan? startTimeout = null) {
            stm.WaitForData(startTimeout.Value);
            var writer = new ArrayPoolBufferWriter<byte>();
            int readSize = -1;
            while (readSize > 0 && writer.WrittenCount <= len) {
                var buffer = writer.GetSpan(len);
                readSize = stm.Read(buffer);
                writer.Advance(readSize);
            }
            // We don't check how many bytes were read! Read loop could have exited if stm.Read returned 0
            // prior to reaching len indicating the connection was closed or data transfer was stopped
            return writer.WrittenSpan;
        }

        public static async Task<ReadOnlyMemory<byte>> ReadBytesAsync(this NetworkStream stm, int len, TimeSpan? startTimeout = null) {
            // wait for some initial data to arrive in the NetworkStream if a timeout was provided
            if (startTimeout.HasValue)
                stm.WaitForData(startTimeout.Value);
            var writer = new ArrayPoolBufferWriter<byte>();
            int readSize = -1;
            while (readSize > 0 && writer.WrittenCount <= len) {
                var buffer = writer.GetMemory(len);
                readSize = await stm.ReadAsync(buffer);
                writer.Advance(readSize);
            }
            // We don't check how many bytes were read! Read loop could have exited if stm.Read returned 0
            // prior to reaching len indicating the connection was closed or data transfer was stopped
            return writer.WrittenMemory;
        }

        const int BUFFER_SIZE = 32768;
        public static byte[] ReadUntilNoMoreData(this NetworkStream stm, 
            int timeoutMillis = 500,
            int firstPacketTimeout = 5000
            ) {
            byte[] buffer = new byte[BUFFER_SIZE];
            var msg = new List<byte[]>();
            int readSize = -1;
            int bytesReceived = 0;
            bool firstPacket = true;
            while (readSize != 0) {
                var timeout = firstPacket ? firstPacketTimeout : timeoutMillis;
                firstPacket = false;
                if (!System.Threading.SpinWait.SpinUntil(() => stm.DataAvailable, timeout))
                    break;
                readSize = stm.Read(buffer, 0, buffer.Length);
                if (readSize > 0) {
                    bytesReceived += readSize;
                    var msgPart = new byte[readSize];
                    Buffer.BlockCopy(buffer, 0, msgPart, 0, readSize);
                    msg.Add(msgPart);
                }
            }
            return msg.Combine();
        }

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
            var endpoint = new IPEndPoint(ipOrHost.ToIPAddress(), port);
            return endpoint.CheckPortState(timeout);
        }

        public static PortState CheckPortState(this IPEndPoint endpoint, TimeSpan? timeout = null) {
            timeout = timeout ?? TimeSpan.FromSeconds(5);
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

    }
}
