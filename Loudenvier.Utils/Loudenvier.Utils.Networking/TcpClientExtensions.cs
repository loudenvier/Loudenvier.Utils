using System;
using System.Net.Sockets;

namespace Loudenvier.Utils
{
    public static class TcpClientExtensions {

        public static ReadOnlySpan<byte> ReadUntilTimeout(
            this TcpClient client,
            TimeSpan? startTimeout = null, 
            TimeSpan ? readTimeout = null) 
            => client.GetStream().ReadUntilTimeout(startTimeout, readTimeout, client.ReceiveBufferSize);

    }
}
