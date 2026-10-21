using System.Net;

namespace BoardGameTracker.Core.Auth.Pictures;

public interface IHostAddressResolver
{
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken);
}
