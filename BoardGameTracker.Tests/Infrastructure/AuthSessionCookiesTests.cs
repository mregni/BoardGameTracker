using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class AuthSessionCookiesTests
{
    private readonly Mock<IProfileImageTicketService> _imageTicketsMock = new();
    private readonly AuthSessionCookies _cookies;

    public AuthSessionCookiesTests()
    {
        _imageTicketsMock.Setup(x => x.Issue(It.IsAny<string>())).Returns("image-ticket");
        _imageTicketsMock.Setup(x => x.Lifetime).Returns(TimeSpan.FromDays(14));
        _cookies = new AuthSessionCookies(_imageTicketsMock.Object, Options.Create(new JwtOptions { RefreshTokenExpiryDays = 14 }));
    }

    private void VerifyNoOtherCalls()
    {
        _imageTicketsMock.VerifyNoOtherCalls();
    }

    private static string RefreshCookie(HttpContext context) =>
        context.Response.Headers.SetCookie.Single(c => c!.StartsWith(AuthSessionCookies.RefreshCookieName + "=", StringComparison.Ordinal))!;

    [Theory]
    [InlineData("http", false)]
    [InlineData("https", true)]
    public void Issue_ShouldWriteAnHttpOnlyLaxRefreshCookieScopedToTheAuthApi_WithSecureFollowingTheRequest(string scheme, bool secure)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;

        _cookies.Issue(context, "refresh-value", "user-1");

        var cookie = RefreshCookie(context);
        cookie.Should().Be($"bgt_refresh=refresh-value; max-age=1209600; path=/api/auth; {(secure ? "secure; " : string.Empty)}samesite=lax; httponly");
        context.Response.Headers.SetCookie.Should().Contain(c => c!.StartsWith($"{ProfileImageCookie.Name}=image-ticket", StringComparison.Ordinal));
        _imageTicketsMock.Verify(x => x.Issue("user-1"), Times.Once);
        _imageTicketsMock.Verify(x => x.Lifetime, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Clear_ShouldExpireBothCookiesOnTheirOwnPaths()
    {
        var context = new DefaultHttpContext();

        AuthSessionCookies.Clear(context);

        var cookies = context.Response.Headers.SetCookie.ToArray();
        cookies.Should().HaveCount(2);
        RefreshCookie(context).Should().Contain("expires=Thu, 01 Jan 1970").And.Contain("path=/api/auth;").And.Contain("httponly");
        cookies.Should().Contain(c => c!.StartsWith($"{ProfileImageCookie.Name}=;", StringComparison.Ordinal) && c.Contains("path=/images/profile"));
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("bgt_refresh=", null)]
    [InlineData("bgt_refresh=%20", null)]
    [InlineData("other=1", null)]
    [InlineData("bgt_refresh=abc", "abc")]
    public void ReadRefreshToken_ShouldOnlyReturnANonBlankCookie(string? cookieHeader, string? expected)
    {
        var context = new DefaultHttpContext();
        if (cookieHeader != null)
        {
            context.Request.Headers.Cookie = cookieHeader;
        }

        AuthSessionCookies.ReadRefreshToken(context.Request).Should().Be(expected);
        VerifyNoOtherCalls();
    }
}
