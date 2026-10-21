using System.Net;

namespace BoardGameTracker.Core.Common;

public static class SecureUrlPolicy
{
    private static readonly string[] LocalSuffixes = [".local", ".localhost", ".internal", ".lan", ".home.arpa"];
    private static readonly IPAddress AwsMetadataV4 = IPAddress.Parse("169.254.169.254");
    private static readonly IPAddress AwsMetadataV6 = IPAddress.Parse("fd00:ec2::254");
    private static readonly IPAddress AlibabaMetadataV4 = IPAddress.Parse("100.100.100.200");

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

    public static bool IsLoopbackHost(string host)
    {
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

    public static bool IsMetadataAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.Equals(AwsMetadataV4) || address.Equals(AwsMetadataV6) || address.Equals(AlibabaMetadataV4);
    }

    public static bool IsInternalAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address)
            || address.Equals(IPAddress.Any)
            || address.Equals(IPAddress.IPv6Any)
            || address.Equals(IPAddress.Broadcast)
            || address.IsIPv6Multicast
            || IsPrivate(address))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        return bytes.Length == 4
               && (bytes[0] == 0
                   || bytes[0] >= 224
                   || (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127));
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
