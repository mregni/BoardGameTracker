using System.Net;
using BoardGameTracker.Core.Common;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Common;

public class TrustedProxyListTests
{
    [Fact]
    public void Parse_ShouldSplitAddressesAndNetworks()
    {
        var result = TrustedProxyList.Parse(["10.0.0.5", "172.16.0.0/12", "2001:db8::/32"]);

        result.Proxies.Should().ContainSingle().Which.Should().Be(IPAddress.Parse("10.0.0.5"));
        result.Networks.Should().HaveCount(2);
        result.Invalid.Should().BeEmpty();
        result.HasEntries.Should().BeTrue();
    }

    [Theory]
    [InlineData("traefik")]
    [InlineData("172.16.0.0/33")]
    [InlineData("proxy.example.com")]
    public void Parse_ShouldReportEntriesThatAreNotAddresses_AndTrustNothing(string entry)
    {
        var result = TrustedProxyList.Parse([entry]);

        result.Invalid.Should().ContainSingle().Which.Should().Be(entry);
        result.HasEntries.Should().BeFalse();
    }

    [Fact]
    public void Parse_ShouldKeepTheValidEntries_WhenSomeAreInvalid()
    {
        var result = TrustedProxyList.Parse(["traefik", "172.18.0.0/16"]);

        result.Networks.Should().ContainSingle();
        result.Invalid.Should().Equal("traefik");
        result.HasEntries.Should().BeTrue();
    }
}
