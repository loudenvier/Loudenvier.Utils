using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Loudenvier.Utils.Testing
{
    public class TcpTestServer : IDisposable {
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

        public void Stop() {
            if (serveTask == null) return;
            server?.Stop();
            cts?.Cancel();
            serveTask.Wait();
        }

        TcpListener? server;

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

        public int Length { get; }
        public byte EOM { get; }
        public byte BOM { get; }
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
