using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class AuthDisabledMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldSetAdminIdentity_WhenNotAuthenticated()
    {
        var middleware = new AuthDisabledMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        context.User.Identity!.IsAuthenticated.Should().BeTrue();
        context.User.Identity.AuthenticationType.Should().Be("AuthDisabled");
        context.User.Claims.Should().HaveCount(4);
        context.User.FindFirstValue(ClaimTypes.Name).Should().Be("admin");
        context.User.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("auth-disabled-admin-id");
        context.User.FindFirstValue("display_name").Should().Be("Admin");
        context.User.IsInRole(Constants.AuthRoles.Admin).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldNotOverrideIdentityAndShouldCallNext_WhenAlreadyAuthenticated()
    {
        var nextCalled = false;
        var middleware = new AuthDisabledMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
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

        await middleware.InvokeAsync(context);

        context.User.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("existing-user");
        context.User.FindFirstValue(ClaimTypes.Name).Should().Be("existing");
        context.User.Identity!.AuthenticationType.Should().Be("TestAuth");
        context.User.IsInRole(Constants.AuthRoles.Admin).Should().BeFalse();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ShouldSetAdminIdentity_WhenThePrincipalHasNoIdentity()
    {
        var middleware = new AuthDisabledMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal() };

        await middleware.InvokeAsync(context);

        context.User.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("auth-disabled-admin-id");
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNextOnceWithTheAdminIdentityInPlace()
    {
        var calls = new List<string?>();
        var middleware = new AuthDisabledMiddleware(next =>
        {
            calls.Add(next.User.FindFirstValue(ClaimTypes.NameIdentifier));
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        calls.Should().Equal("auth-disabled-admin-id");
    }
}
