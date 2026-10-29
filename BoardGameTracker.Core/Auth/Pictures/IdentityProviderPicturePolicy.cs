using System.Net;
using BoardGameTracker.Core.Common;

namespace BoardGameTracker.Core.Auth.Pictures;

public static class IdentityProviderPicturePolicy
{
    public static Uri? Validate(string pictureUrl, Uri authority)
    {
        if (!Uri.TryCreate(pictureUrl, UriKind.Absolute, out var candidate)
            || (candidate.Scheme != Uri.UriSchemeHttps && candidate.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(candidate.UserInfo)
            || string.IsNullOrEmpty(candidate.IdnHost))
        {
            return null;
        }

        if (candidate.Scheme == Uri.UriSchemeHttp
            && authority.Scheme == Uri.UriSchemeHttps
            && candidate.IsDefaultPort
            && IsAuthorityHost(candidate, authority))
        {
            candidate = new UriBuilder(candidate) { Scheme = Uri.UriSchemeHttps, Port = authority.Port }.Uri;
        }

        var authorityIsLoopback = SecureUrlPolicy.IsLoopbackHost(authority.IdnHost);
        var targetIsLoopback = SecureUrlPolicy.IsLoopbackHost(candidate.IdnHost);
        if (targetIsLoopback && !authorityIsLoopback)
        {
            return null;
        }

        if (candidate.Scheme == Uri.UriSchemeHttp && !targetIsLoopback && !IsAuthorityOrigin(candidate, authority))
        {
            return null;
        }

        var builder = new UriBuilder(candidate.Scheme, candidate.IdnHost, candidate.Port, candidate.AbsolutePath)
        {
            Query = candidate.Query.TrimStart('?'),
        };
        return builder.Uri;
    }

    public static bool IsAuthorityHost(Uri target, Uri authority)
    {
        return string.Equals(target.IdnHost, authority.IdnHost, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAllowedAddress(IPAddress address, bool targetIsAuthorityHost, bool authorityIsLoopback)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (SecureUrlPolicy.IsMetadataAddress(address))
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return authorityIsLoopback;
        }

        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.Broadcast) || address.IsIPv6Multicast)
        {
            return false;
        }

        return targetIsAuthorityHost || !SecureUrlPolicy.IsInternalAddress(address);
    }

    private static bool IsAuthorityOrigin(Uri target, Uri authority)
    {
        return target.Scheme == authority.Scheme && target.Port == authority.Port && IsAuthorityHost(target, authority);
    }
}
