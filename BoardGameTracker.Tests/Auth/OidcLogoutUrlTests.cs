using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcLogoutUrlTests
{
    [Theory]
    [InlineData("https://games.example.com", "https://games.example.com/login")]
    [InlineData("https://games.example.com/", "https://games.example.com/login")]
    [InlineData("http://localhost:5476", "http://localhost:5476/login")]
    public void PostLogoutRedirectUri_ShouldBeTheLoginPageOfThePublicUrl(string publicBaseUrl, string expected)
    {
        OidcLogoutUrl.PostLogoutRedirectUri(publicBaseUrl).Should().Be(expected);
    }

    [Fact]
    public void Build_ShouldEscapeEveryParameter()
    {
        var url = OidcLogoutUrl.Build("https://sso.example.com/logout", "a.b+c/d", "bgt client", "https://games.example.com");

        url.Should().Be("https://sso.example.com/logout?id_token_hint=a.b%2Bc%2Fd&post_logout_redirect_uri=https%3A%2F%2Fgames.example.com%2Flogin&client_id=bgt%20client");
    }

    [Fact]
    public void Build_ShouldAppendToAnEndpointThatAlreadyHasAQuery()
    {
        var url = OidcLogoutUrl.Build("https://sso.example.com/logout?tenant=1", "t", "bgt", "https://games.example.com");

        url.Should().StartWith("https://sso.example.com/logout?tenant=1&id_token_hint=t&");
    }
}
