using System.Net;
using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class UserRateLimitKeyTests
{
    [Fact]
    public void From_ShouldUseTheUserId_ForASignedInUser()
    {
        var context = Context("Bearer");

        UserRateLimitKey.From(context).Should().Be("user:user-1");
    }

    [Fact]
    public void From_ShouldUseTheClientAddress_ForAnAnonymousRequest()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");

        UserRateLimitKey.From(context).Should().Be("192.0.2.10");
    }

    [Fact]
    public void From_ShouldUseTheClientAddress_WhenAuthenticationIsDisabled()
    {
        var context = Context(AuthDisabledMiddleware.AuthenticationType);

        UserRateLimitKey.From(context).Should().Be("192.0.2.10");
    }

    private static DefaultHttpContext Context(string authenticationType)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], authenticationType)),
        };
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
        return context;
    }
}
