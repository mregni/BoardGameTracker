using System.Net;
using BoardGameTracker.Core.Auth.Pictures;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth.Pictures;

public class IdentityProviderPicturePolicyTests
{
    private static readonly Uri PublicAuthority = new("https://idp.example.com/application/o/bgt/");
    private static readonly Uri LoopbackAuthority = new("http://127.0.0.1:8080/realms/bgt");
    private static readonly Uri LanAuthority = new("http://auth.lan:9000/");

    [Theory]
    [InlineData("https://cdn.example.com/a.png")]
    [InlineData("https://idp.example.com/media/avatar.png?size=128")]
    public void Validate_ShouldAcceptHttpsAddresses(string url)
    {
        IdentityProviderPicturePolicy.Validate(url, PublicAuthority).Should().Be(new Uri(url));
    }

    [Theory]
    [InlineData("http://cdn.example.com/a.png")]
    [InlineData("http://idp.example.com/a.png")]
    [InlineData("ftp://cdn.example.com/a.png")]
    [InlineData("file:///etc/passwd")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=")]
    [InlineData("/relative/a.png")]
    [InlineData("not a url")]
    [InlineData("https://user:pass@cdn.example.com/a.png")]
    public void Validate_ShouldRefuseAnythingButPlainHttps(string url)
    {
        IdentityProviderPicturePolicy.Validate(url, PublicAuthority).Should().BeNull();
    }

    [Theory]
    [InlineData("https://localhost/a.png")]
    [InlineData("https://127.0.0.1/a.png")]
    [InlineData("https://[::1]/a.png")]
    [InlineData("https://app.localhost/a.png")]
    public void Validate_ShouldRefuseLoopback_WhenTheAuthorityIsNotLoopback(string url)
    {
        IdentityProviderPicturePolicy.Validate(url, PublicAuthority).Should().BeNull();
    }

    [Theory]
    [InlineData("http://127.0.0.1:8080/avatar.png")]
    [InlineData("http://localhost:9000/avatar.png")]
    public void Validate_ShouldAcceptLoopback_WhenTheAuthorityIsLoopback(string url)
    {
        IdentityProviderPicturePolicy.Validate(url, LoopbackAuthority).Should().NotBeNull();
    }

    [Fact]
    public void Validate_ShouldAcceptPlainHttp_OnlyOnTheOriginOfAnHttpAuthority()
    {
        IdentityProviderPicturePolicy.Validate("http://auth.lan:9000/media/a.png", LanAuthority).Should().NotBeNull();
        IdentityProviderPicturePolicy.Validate("http://auth.lan:9001/media/a.png", LanAuthority).Should().BeNull();
        IdentityProviderPicturePolicy.Validate("http://other.lan:9000/media/a.png", LanAuthority).Should().BeNull();
    }

    [Fact]
    public void Validate_ShouldDropTheFragment()
    {
        IdentityProviderPicturePolicy.Validate("https://cdn.example.com/a.png?x=1#frag", PublicAuthority)!.AbsoluteUri
            .Should().Be("https://cdn.example.com/a.png?x=1");
    }

    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.3.4")]
    [InlineData("192.168.1.20")]
    [InlineData("169.254.10.10")]
    [InlineData("100.64.0.1")]
    [InlineData("fd12:3456::1")]
    [InlineData("fe80::1")]
    [InlineData("::ffff:192.168.1.20")]
    public void IsAllowedAddress_ShouldRefuseInternalAddresses_ForAnotherHost(string address)
    {
        IdentityProviderPicturePolicy.IsAllowedAddress(IPAddress.Parse(address), false, false).Should().BeFalse();
    }

    [Theory]
    [InlineData("10.0.0.5")]
    [InlineData("192.168.1.20")]
    public void IsAllowedAddress_ShouldAcceptInternalAddresses_OfTheAuthorityHost(string address)
    {
        IdentityProviderPicturePolicy.IsAllowedAddress(IPAddress.Parse(address), true, false).Should().BeTrue();
    }

    [Theory]
    [InlineData("169.254.169.254")]
    [InlineData("fd00:ec2::254")]
    [InlineData("100.100.100.200")]
    [InlineData("0.0.0.0")]
    [InlineData("::")]
    [InlineData("255.255.255.255")]
    [InlineData("ff02::1")]
    public void IsAllowedAddress_ShouldAlwaysRefuseMetadataAndUnroutableAddresses(string address)
    {
        IdentityProviderPicturePolicy.IsAllowedAddress(IPAddress.Parse(address), true, true).Should().BeFalse();
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void IsAllowedAddress_ShouldAcceptLoopback_OnlyForALoopbackAuthority(string address)
    {
        IdentityProviderPicturePolicy.IsAllowedAddress(IPAddress.Parse(address), true, false).Should().BeFalse();
        IdentityProviderPicturePolicy.IsAllowedAddress(IPAddress.Parse(address), false, true).Should().BeTrue();
    }

    [Fact]
    public void IsAllowedAddress_ShouldAcceptPublicAddresses()
    {
        IdentityProviderPicturePolicy.IsAllowedAddress(IPAddress.Parse("93.184.216.34"), false, false).Should().BeTrue();
        IdentityProviderPicturePolicy.IsAllowedAddress(IPAddress.Parse("2606:2800:220:1::1"), false, false).Should().BeTrue();
    }

    [Fact]
    public void IsAuthorityHost_ShouldCompareHostsIgnoringCase()
    {
        IdentityProviderPicturePolicy.IsAuthorityHost(new Uri("https://IDP.example.com/x"), PublicAuthority).Should().BeTrue();
        IdentityProviderPicturePolicy.IsAuthorityHost(new Uri("https://cdn.example.com/x"), PublicAuthority).Should().BeFalse();
    }

    [Fact]
    public void OidcPictureClaim_ShouldReadTheDefaultClaim_AndSkipBlankValues()
    {
        var provider = new BoardGameTracker.Common.Entities.Auth.OidcProvider("IdP", "https://idp.example.com", "bgt");

        OidcPictureClaim.Read(provider, BoardGameTracker.Core.Auth.OidcClaims.Parse("""{"picture":" https://idp.example.com/a.png "}"""))
            .Should().Be("https://idp.example.com/a.png");
        OidcPictureClaim.Read(provider, BoardGameTracker.Core.Auth.OidcClaims.Parse("""{"picture":"  "}""")).Should().BeNull();
        OidcPictureClaim.Read(provider, BoardGameTracker.Core.Auth.OidcClaims.Parse("""{"name":"Jane"}""")).Should().BeNull();
    }

    [Fact]
    public void OidcPictureClaim_ShouldReadTheConfiguredClaim()
    {
        var provider = new BoardGameTracker.Common.Entities.Auth.OidcProvider("IdP", "https://idp.example.com", "bgt");
        provider.SetPictureClaimType(" avatar ");

        provider.PictureClaimType.Should().Be("avatar");
        OidcPictureClaim.Read(provider, BoardGameTracker.Core.Auth.OidcClaims.Parse("""{"picture":"https://a/1.png","avatar":"https://a/2.png"}"""))
            .Should().Be("https://a/2.png");

        provider.SetPictureClaimType("   ");
        provider.PictureClaimType.Should().BeNull();
    }
}
