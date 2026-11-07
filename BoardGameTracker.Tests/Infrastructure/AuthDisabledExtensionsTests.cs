using System.Security.Claims;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Core.Configuration.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class AuthDisabledExtensionsTests
{
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly ApplicationBuilder _appBuilder;

    public AuthDisabledExtensionsTests()
    {
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _appBuilder = new ApplicationBuilder(new ServiceCollection()
            .AddSingleton(_environmentProviderMock.Object)
            .BuildServiceProvider());
    }

    private void VerifyNoOtherCalls()
    {
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UseAuthDisabledMiddleware_ShouldRegisterMiddleware_WhenAuthIsDisabled()
    {
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(false);
        var context = new DefaultHttpContext();

        var result = _appBuilder.UseAuthDisabledMiddleware();
        await _appBuilder.Build()(context);

        result.Should().BeSameAs(_appBuilder);
        context.User.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("auth-disabled-admin-id");
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UseAuthDisabledMiddleware_ShouldNotRegisterMiddleware_WhenAuthIsEnabled()
    {
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(true);
        var context = new DefaultHttpContext();

        var result = _appBuilder.UseAuthDisabledMiddleware();
        await _appBuilder.Build()(context);

        result.Should().BeSameAs(_appBuilder);
        context.User.Identity!.IsAuthenticated.Should().BeFalse();
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }
}
