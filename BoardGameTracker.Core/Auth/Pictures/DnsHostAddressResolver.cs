using System.Net;

namespace BoardGameTracker.Core.Auth.Pictures;

public class DnsHostAddressResolver : IHostAddressResolver
{
    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        return IPAddress.TryParse(host, out var address)
            ? Task.FromResult(new[] { address })
            : Dns.GetHostAddressesAsync(host, cancellationToken);
    }
}
