using System.Security.Claims;
using System.Threading.Tasks;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class ProfileImageAccessMiddlewareTests
{
    private readonly Mock<IProfileImageTicketService> _tickets = new();
    private bool _nextCalled;
    private readonly ProfileImageAccessMiddleware _middleware;

    public ProfileImageAccessMiddlewareTests()
    {
        _middleware = new ProfileImageAccessMiddleware(_ =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task InvokeAsync_ShouldPass_WhenTheRequestIsAuthenticated()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u1")], "Bearer")) };

        await _middleware.InvokeAsync(context, _tickets.Object);

        _nextCalled.Should().BeTrue();
        _tickets.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_ShouldPass_WhenTheImageCookieIsValid()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{ProfileImageCookie.Name}=ticket-1";
        _tickets.Setup(x => x.IsValid("ticket-1")).Returns(true);

        await _middleware.InvokeAsync(context, _tickets.Object);

        _nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAnswer401_WhenNeitherIsPresent()
    {
        var context = new DefaultHttpContext();
        _tickets.Setup(x => x.IsValid(null)).Returns(false);

        await _middleware.InvokeAsync(context, _tickets.Object);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
    }
}
