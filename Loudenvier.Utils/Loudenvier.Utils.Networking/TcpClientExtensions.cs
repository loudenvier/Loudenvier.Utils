using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Loudenvier.Utils;

public static class TcpClientExtensions
{
    /// <summary>
    /// Connects the client to the specified TCP port on the specified host with the defined <paramref name="timeout"/>
    /// and an optional <see cref="CancellationToken"/> (<paramref name="cts"/>).
    /// </summary>
    /// <param name="client">The TCP client</param>
    /// <param name="timeout">The time to wait for the connection to complete</param>
    /// <param name="host">The DNS name of the remote host you wish to connect</param>
    /// <param name="port">The port number of the remote host you wish to connect</param>
    /// <param name="cts">The optional cancellation token that allows cancelling the connection prior to <paramref name="timeout"/></param>
    /// <returns>true if the connection was estabelished within the specified timeout, otherwise false</returns>
    public static bool Connect(this TcpClient client, TimeSpan timeout, string host, int port, CancellationToken? cts = null) {
        // this used to be a lot more complicated https://stackoverflow.com/questions/17118632/how-to-set-the-timeout-for-a-tcpclient
        // but with ConnectAsync it's really easy now, so I've changed this code to use it
        var completed = client.ConnectAsync(host, port)
            .Wait((int)timeout.TotalMilliseconds, cts ?? CancellationToken.None);

        // even if complete is true, client.Connected may still be false
        return completed && client.Connected;
    }

    public static void WaitForData(this TcpClient client, TimeSpan? timeout)
        => client.GetStream().WaitForData(timeout);

    public static ReadOnlySpan<byte> ReadBytes(this TcpClient client, int length, TimeSpan? startTimeout = null)
        => client.GetStream().ReadBytes(length, startTimeout);

    public static async Task<ReadOnlyMemory<byte>> ReadBytesAsync(this TcpClient client, int length, TimeSpan? startTimeout = null)
        => await client.GetStream().ReadBytesAsync(length, startTimeout).ConfigureAwait(false);

    public static ReadOnlySpan<byte> ReadToEOM(this TcpClient client, byte EOM, TimeSpan? startTimeout = null)
        => client.GetStream().ReadToEOM(EOM, startTimeout, client.ReceiveBufferSize);

    public static ReadOnlySpan<byte> ReadUntilTimeout(
        this TcpClient client,
        TimeSpan? startTimeout = null,
        TimeSpan? readTimeout = null)
        => client.GetStream().ReadUntilTimeout(startTimeout, readTimeout, client.ReceiveBufferSize);

}
