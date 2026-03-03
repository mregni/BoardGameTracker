using System.Security.Claims;
using System.Threading.Tasks;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class AuthBypassMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldSetAdminIdentity_WhenNotAuthenticated()
    {
        // Arrange
        var middleware = new AuthBypassMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.User.Identity!.IsAuthenticated.Should().BeTrue();
        context.User.Identity.AuthenticationType.Should().Be("AuthBypass");
        context.User.FindFirstValue(ClaimTypes.Name).Should().Be("admin");
        context.User.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("bypass-admin-id");
        context.User.IsInRole(Constants.AuthRoles.Admin).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotOverrideIdentity_WhenAlreadyAuthenticated()
    {
        // Arrange
        var middleware = new AuthBypassMiddleware(_ => Task.CompletedTask);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "existing-user"),
            new Claim(ClaimTypes.Name, "existing")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.User.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("existing-user");
        context.User.FindFirstValue(ClaimTypes.Name).Should().Be("existing");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new AuthBypassMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldIncludeDisplayNameClaim()
    {
        // Arrange
        var middleware = new AuthBypassMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.User.FindFirstValue("display_name").Should().Be("Admin (Bypass)");
    }
}
