using System.Net;

namespace BoardGameTracker.Core.Common;

public sealed record TrustedProxyList(
    IReadOnlyList<IPAddress> Proxies,
    IReadOnlyList<IPNetwork> Networks,
    IReadOnlyList<string> Invalid)
{
    public bool HasEntries => Proxies.Count + Networks.Count > 0;

    public static TrustedProxyList Parse(IEnumerable<string> entries)
    {
        var proxies = new List<IPAddress>();
        var networks = new List<IPNetwork>();
        var invalid = new List<string>();

        foreach (var entry in entries)
        {
            if (IPAddress.TryParse(entry, out var address))
            {
                proxies.Add(address);
            }
            else if (IPNetwork.TryParse(entry, out var network))
            {
                networks.Add(network);
            }
            else
            {
                invalid.Add(entry);
            }
        }

        return new TrustedProxyList(proxies, networks, invalid);
    }
}
