using System.Net;
using BoardGameTracker.Core.Email.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BoardGameTracker.Api.Infrastructure;

public static class OidcTransportPolicy
{
    public static bool IsAllowed(HttpRequest request)
    {
        return request.IsHttps || IsLoopbackHost(request.Host.Host);
    }

    public static bool IsAllowed(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && IsLoopbackHost(uri.Host));
    }

    public static async Task<bool> IsAllowedAsync(HttpRequest request, IPublicUrlBuilder publicUrlBuilder)
    {
        return IsAllowed(request) && IsAllowed((await PublicBaseUrl.ResolveAsync(publicUrlBuilder, request)).BaseUrl);
    }

    public static bool IsLoopbackHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var name = host.Trim().TrimEnd('.');
        if (name.StartsWith('[') && name.EndsWith(']'))
        {
            name = name[1..^1];
        }

        if (name.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(name, out var address) && IPAddress.IsLoopback(address);
    }
}
