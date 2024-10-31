using System;
using System.Net;
using System.Net.Sockets;

namespace Loudenvier.Utils;

public static class NetworkStreamExtensions { 
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

}
