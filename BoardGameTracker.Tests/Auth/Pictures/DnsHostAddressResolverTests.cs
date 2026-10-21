using System.Net;
using BoardGameTracker.Core.Auth.Pictures;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth.Pictures;

public class DnsHostAddressResolverTests
{
    private readonly DnsHostAddressResolver _resolver = new();

    [Fact]
    public async Task ResolveAsync_ShouldReturnAnAddressLiteralAsIs()
    {
        var addresses = await _resolver.ResolveAsync("203.0.113.7", TestContext.Current.CancellationToken);

        addresses.Should().Equal(IPAddress.Parse("203.0.113.7"));
    }

    [Fact]
    public async Task ResolveAsync_ShouldLookUpAHostName()
    {
        var addresses = await _resolver.ResolveAsync("localhost", TestContext.Current.CancellationToken);

        addresses.Should().NotBeEmpty().And.OnlyContain(a => IPAddress.IsLoopback(a));
    }
}
