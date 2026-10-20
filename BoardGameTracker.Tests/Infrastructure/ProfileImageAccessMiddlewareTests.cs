using System.Security.Claims;
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
    private readonly Mock<IUserAccessCheck> _accessCheck = new();
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

    private void VerifyNoOtherCalls()
    {
        _tickets.VerifyNoOtherCalls();
        _accessCheck.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_ShouldPass_WhenTheRequestIsAuthenticated()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u1")], "Bearer")) };

        await _middleware.InvokeAsync(context, _tickets.Object, _accessCheck.Object);

        _nextCalled.Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_ShouldPass_WhenTheImageCookieBelongsToAnActiveUser()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{ProfileImageCookie.Name}=ticket-1";
        _tickets.Setup(x => x.ReadUserId("ticket-1")).Returns("u1");
        _accessCheck.Setup(x => x.CanSignInAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _middleware.InvokeAsync(context, _tickets.Object, _accessCheck.Object);

        _nextCalled.Should().BeTrue();
        _tickets.Verify(x => x.ReadUserId("ticket-1"), Times.Once);
        _accessCheck.Verify(x => x.CanSignInAsync("u1", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAnswer401_WhenTheImageCookieBelongsToADisabledUser()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"{ProfileImageCookie.Name}=ticket-1";
        _tickets.Setup(x => x.ReadUserId("ticket-1")).Returns("u1");
        _accessCheck.Setup(x => x.CanSignInAsync("u1", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _middleware.InvokeAsync(context, _tickets.Object, _accessCheck.Object);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _tickets.Verify(x => x.ReadUserId("ticket-1"), Times.Once);
        _accessCheck.Verify(x => x.CanSignInAsync("u1", It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAnswer401_WhenNeitherIsPresent()
    {
        var context = new DefaultHttpContext();
        _tickets.Setup(x => x.ReadUserId(null)).Returns((string?)null);

        await _middleware.InvokeAsync(context, _tickets.Object, _accessCheck.Object);

        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Headers.CacheControl.ToString().Should().Be("no-store");
        _tickets.Verify(x => x.ReadUserId(null), Times.Once);
        VerifyNoOtherCalls();
    }
}
