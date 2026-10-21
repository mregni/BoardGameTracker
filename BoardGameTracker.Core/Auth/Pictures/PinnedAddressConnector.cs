using System.Net;
using System.Net.Sockets;

namespace BoardGameTracker.Core.Auth.Pictures;

public static class PinnedAddressConnector
{
    public static readonly HttpRequestOptionsKey<IPAddress[]> PinnedAddresses = new("bgt-pinned-addresses");

    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        if (!context.InitialRequestMessage.Options.TryGetValue(PinnedAddresses, out var addresses) || addresses.Length == 0)
        {
            throw new HttpRequestException("The request carries no vetted address to connect to.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
