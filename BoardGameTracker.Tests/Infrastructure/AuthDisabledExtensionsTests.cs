using System;
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
    private readonly Mock<IApplicationBuilder> _appBuilderMock;

    public AuthDisabledExtensionsTests()
    {
        _environmentProviderMock = new Mock<IEnvironmentProvider>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnvironmentProvider)))
            .Returns(_environmentProviderMock.Object);

        _appBuilderMock = new Mock<IApplicationBuilder>();
        _appBuilderMock
            .Setup(x => x.ApplicationServices)
            .Returns(serviceProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void UseAuthDisabledMiddleware_ShouldRegisterMiddleware_WhenAuthIsDisabled()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(false);
        _appBuilderMock
            .Setup(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(_appBuilderMock.Object);

        // Act
        var result = _appBuilderMock.Object.UseAuthDisabledMiddleware();

        // Assert
        result.Should().BeSameAs(_appBuilderMock.Object);
        _appBuilderMock.Verify(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void UseAuthDisabledMiddleware_ShouldNotRegisterMiddleware_WhenAuthIsEnabled()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(true);

        // Act
        var result = _appBuilderMock.Object.UseAuthDisabledMiddleware();

        // Assert
        result.Should().BeSameAs(_appBuilderMock.Object);
        _appBuilderMock.Verify(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Never);
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void UseAuthDisabledMiddleware_ShouldReturnSameAppBuilder()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(true);

        // Act
        var result = _appBuilderMock.Object.UseAuthDisabledMiddleware();

        // Assert
        result.Should().BeSameAs(_appBuilderMock.Object);
    }
}
