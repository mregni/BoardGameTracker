using System.Net;
using BoardGameTracker.Core.Common;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Common;

public class ClientAddressKeyTests
{
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    [InlineData("2001:db8:1:2:aaaa:bbbb:cccc:dddd", "2001:db8:1:2::/64")]
    [InlineData("2001:db8:1:2::1", "2001:db8:1:2::/64")]
    public void From_ShouldKeyIPv4ByAddressAndIPv6ByItsSlash64(string address, string expected)
    {
        ClientAddressKey.From(IPAddress.Parse(address)).Should().Be(expected);
    }

    [Fact]
    public void From_ShouldGiveEveryAddressOfOneIPv6Slash64TheSameKey()
    {
        var first = ClientAddressKey.From(IPAddress.Parse("2001:db8:1:2::1"));
        var rotated = ClientAddressKey.From(IPAddress.Parse("2001:db8:1:2:ffff:ffff:ffff:fffe"));
        var otherNetwork = ClientAddressKey.From(IPAddress.Parse("2001:db8:1:3::1"));

        rotated.Should().Be(first);
        otherNetwork.Should().NotBe(first);
    }

    [Fact]
    public void From_ShouldUseAPlaceholder_WhenTheAddressIsUnknown()
    {
        ClientAddressKey.From(null).Should().Be("unknown");
    }
}
