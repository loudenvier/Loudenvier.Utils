using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Loudenvier.Utils.Testing
{
    /// <summary>
    /// Implements a simple TCP/IP server that can be used in unit testing scenarios
    /// involving TCP/IP protocols. By default it creates a server that listens on 
    /// the specified <see cref="EndPoint"/> and responds to a connected client 
    /// with a stream of bytes of the specified <see cref="Length"/>, 
    /// starting with the specified <see cref="BOM"/> byte and ending with the 
    /// specified <see cref="EOM"/> byte, and then closes the connection to that client.
    /// <para>
    /// It accepts clients in parallel and does not block when writing the response
    /// to each client. This allows the server to be instantiated once for all unit tests, 
    /// but it can also be instantiated on every test since it implements <see cref="IDisposable"/>
    /// (if you wrap it in a <c>using</c> clause).
    /// </para>
    /// <para>
    /// You can customize what bytes to send by providing a custom <see cref="GetBytesToSend"/> function. 
    /// You can be notified when data is sent by providing a custom <see cref="DataSent"/> action.
    /// You can also be notified of errors by providing custom actions to <see cref="ErrorAcceptingClient"/>
    /// and <see cref="ErrorSending"/> (these actions and functions are not .NET events, because
    /// it is expected that only one single "subscriber" exists per server instance).
    /// </para>
    /// </summary>
    public class TcpTestServer : IDisposable {
        /// <summary>
        /// Creates a new TCP test server listening on the provided <paramref name="endpoint"/> which
        /// will respond with an array of bytes of the specified <paramref name="length"/>, starting
        /// with <paramref name="eom"/> and ending with <paramref name="bom"/> (which by default are zeroes,
        /// meaning no End of Message or Beginning of Message bytes at all).
        /// </summary>
        /// <param name="endpoint">The IP endpoint to listen for incoming connections</param>
        /// <param name="length">The default length of the array of bytes to send to each connected client.</param>
        /// <param name="eom">An End of Message byte to send as the last byte in the array (0 means disabled).</param>
        /// <param name="bom">A Beginning of Message byte to send as the first byte in the array (0 means disabled).</param>
        public TcpTestServer(IPEndPoint endpoint, int length = 1024, byte eom = 0, byte bom = 0) {
            EndPoint = endpoint;
            Length = length;
            EOM = eom;
            BOM = bom;
            GetBytesToSend = () => {
                bufferToSend ??= new byte[Length];
                bufferToSend[0] = BOM;
                bufferToSend[^1] = EOM;
                return bufferToSend;
            };
        }

        /// <summary>Stops the TCP Test Server (automatically called when disposed)</summary>
        public void Stop() {
            if (serveTask == null) return;
            server?.Stop();
            cts?.Cancel();
            serveTask.Wait();
        }

        TcpListener? server;

        /// <summary>
        /// Starts the TCP Test Server by listening for incoming connections on the configured <see cref="EndPoint"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void Start() {
            if (disposedValue) throw new ObjectDisposedException(GetType().Name);
            if (serveTask != null) return;
            var ct = cts!.Token;
            server = new TcpListener(EndPoint);
            server.Start();
            serveTask = Task.Run(() => {
                try {
                    // don't bother throwing OperationCancelledExceptions here, just exit the loop!
                    while (!ct.IsCancellationRequested) {
                        try {
                            var client = server.AcceptTcpClient();
                            // offloads sending data to another thread (accepts TcpClient as soon as possible)
                            Task.Run(() => {
                                try {
                                    var clientStream = client.GetStream();
                                    if (ct.IsCancellationRequested) return;
                                    var bytes = GetBytesToSend?.Invoke();
                                    if (bytes == null) return;
                                    clientStream.Write(bytes, 0, bytes.Length);
                                    DataSent?.Invoke(bytes);
                                    if (ct.IsCancellationRequested) return;
                                    clientStream.Flush();
                                } catch (Exception ex) {
                                    ErrorSending?.Invoke(ex);
                                } finally {
                                    try { client.Close(); } catch { }
                                }
                            }, ct);
                        } catch (Exception ex) {
                            ErrorAcceptingClient?.Invoke(ex);
                        }
                    }
                } finally {
                    try { server.Stop(); } catch { }
                }
            }, ct);
        }

        /// <summary>The default lenght of the byte array to send to connected clients</summary>
        public int Length { get; }
        /// <summary>The End of Message byte to send as the last byte in the array sent to connected clients.</summary>
        public byte EOM { get; }
        /// <summary>The Beginning of Message byte to send as the first byte in the array sent to connected clintes.</summary>
        public byte BOM { get; }
        /// <summary>The IP EndPoint the TCP Test Server will listen for incoming connections.</summary>
        public IPEndPoint EndPoint { get; }

        byte[]? bufferToSend;

        // Hooks for test customization purposes
        public Func<byte[]>? GetBytesToSend { get; set; }
        public Action<byte[]>? DataSent { get; set; }
        public Action<Exception>? ErrorSending { get; set; }
        public Action<Exception>? ErrorAcceptingClient { get; set; }


        CancellationTokenSource? cts = new();
        Task? serveTask;

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) 
                    Stop();
                disposedValue = true;
                cts = null;
                serveTask = null;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
