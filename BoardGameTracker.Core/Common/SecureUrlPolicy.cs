using System.Net;

namespace BoardGameTracker.Core.Common;

public static class SecureUrlPolicy
{
    private static readonly string[] LocalSuffixes = [".local", ".localhost", ".internal", ".lan", ".home.arpa"];

    public static bool IsAcceptable(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        return uri.Scheme == Uri.UriSchemeHttp && IsLocalHost(uri);
    }

    public static bool IsPublicWebAddress(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
               && !IsLocalHost(uri);
    }

    private static bool IsLocalHost(Uri uri)
    {
        if (uri.IsLoopback)
        {
            return true;
        }

        if (uri.HostNameType == UriHostNameType.Dns)
        {
            return !uri.Host.Contains('.') || LocalSuffixes.Any(suffix => uri.Host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }

        return IPAddress.TryParse(uri.DnsSafeHost, out var address) && IsPrivate(address);
    }

    private static bool IsPrivate(IPAddress address)
    {
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal)
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return false;
        }

        return bytes[0] == 10
               || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
               || (bytes[0] == 192 && bytes[1] == 168)
               || (bytes[0] == 169 && bytes[1] == 254);
    }
}
