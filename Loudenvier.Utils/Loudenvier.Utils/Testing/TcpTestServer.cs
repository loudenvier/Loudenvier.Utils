using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loudenvier.Utils.Testing
{
    public class TcpTestServer : IDisposable
    {
        public TcpTestServer(IPEndPoint endpoint, int fixedLengthSize, byte eom) {
            this.endpoint = endpoint;
            bufferToSend = new byte[fixedLengthSize];
            bufferToSend[^1] = eom; 
        }

        public void Stop() {
            if (serveTask == null) return;
            cts?.Cancel();
            serveTask.Wait();
        }

        public void Start() {
            if (disposedValue) throw new ObjectDisposedException(GetType().Name);   
            if (serveTask != null) return;
            var ct = cts.Token;
            var server = new TcpListener(endpoint);
            server.Start();
            serveTask = Task.Run(() => {
                try {
                    // don't bother throwing OperationCancelledExceptions here, just exit the loop!
                    while (!ct.IsCancellationRequested) {
                        try {
                            var client = server.AcceptTcpClient();
                            // offloads sending data in another thread (accepts TcpClient as soon as possible)
                            Task.Run(() => {
                                try {
                                    var clientStream = client.GetStream();
                                    if (ct.IsCancellationRequested) return;
                                    var bytes = GetBytesToSend();
                                    clientStream.Write(bytes, 0, bytes.Length);
                                    if (ct.IsCancellationRequested) return;
                                    clientStream.Flush();
                                } catch (Exception ex) {
                                } finally {
                                    try { client.Close(); } catch { }
                                }
                            }, ct);
                        } catch (Exception ex) {
                        }
                    }
                } finally {
                    try { server.Stop(); } catch { }
                }
            }, ct);
        }

        readonly byte[] bufferToSend;
        byte[] GetBytesToSend() => bufferToSend;

        IPEndPoint endpoint;
        CancellationTokenSource cts = new();
        Task serveTask;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
                cts = null;
                serveTask = null;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TcpTestServer()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
