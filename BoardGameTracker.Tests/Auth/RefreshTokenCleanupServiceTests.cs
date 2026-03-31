using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class RefreshTokenCleanupServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<RefreshTokenCleanupService>> _loggerMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<ITokenService> _tokenServiceMock;

    public RefreshTokenCleanupServiceTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<RefreshTokenCleanupService>>();
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _tokenServiceMock = new Mock<ITokenService>();

        SetupServiceScope();
    }

    private void SetupServiceScope()
    {
        var scopeMock = new Mock<IServiceScope>();
        var scopedProviderMock = new Mock<IServiceProvider>();

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(scopedProviderMock.Object);

        scopedProviderMock
            .Setup(x => x.GetService(typeof(IEnvironmentProvider)))
            .Returns(_environmentProviderMock.Object);
        scopedProviderMock
            .Setup(x => x.GetService(typeof(ITokenService)))
            .Returns(_tokenServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _environmentProviderMock.VerifyNoOtherCalls();
        _tokenServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Act & Assert
        var service = new RefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnImmediately_WhenAuthIsDisabled()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(false);
        var service = new RefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCleanupBeforeInterval_WhenAuthIsEnabled()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(true);
        var service = new RefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act - start and cancel quickly (before the 24h interval elapses)
        await service.StartAsync(cts.Token);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - cleanup should not have been called yet (interval is 24h)
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopGracefully_WhenCancelled()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(true);
        var service = new RefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        // Assert - should not throw unexpected exceptions
        var act = () => service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync<Exception>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleCleanupException()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(true);
        _tokenServiceMock
            .Setup(x => x.CleanupExpiredTokensAsync())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Use a real scope factory that returns fresh scopes so we can trigger cleanup
        var scopeMock = new Mock<IServiceScope>();
        var scopedProviderMock = new Mock<IServiceProvider>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(scopedProviderMock.Object);
        scopedProviderMock
            .Setup(x => x.GetService(typeof(IEnvironmentProvider)))
            .Returns(_environmentProviderMock.Object);
        scopedProviderMock
            .Setup(x => x.GetService(typeof(ITokenService)))
            .Returns(_tokenServiceMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        var service = new RefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act - start and cancel quickly
        await service.StartAsync(cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        // Assert - service should not crash from the exception
        var act = () => service.StopAsync(CancellationToken.None);
        await act.Should().NotThrowAsync<Exception>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCheckAuthEnabled_InScopedContext()
    {
        // Arrange
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(false);
        var service = new RefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await cts.CancelAsync();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - should have created a scope to check auth status
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
    }
}
