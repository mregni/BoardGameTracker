using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class RefreshTokenCleanupServiceTests
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

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
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(true);
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

    private async Task RunUntilAsync(Task signal)
    {
        var service = new TestableRefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);
        try
        {
            await signal.WaitAsync(SignalTimeout);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCleanUpExpiredTokens_WhenAuthIsEnabled()
    {
        var cleanupStarted = new TaskCompletionSource();
        _tokenServiceMock
            .Setup(x => x.CleanupExpiredTokensAsync())
            .Callback(() => cleanupStarted.TrySetResult())
            .Returns(Task.CompletedTask);

        await RunUntilAsync(cleanupStarted.Task);

        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNeverCleanUp_WhenAuthIsDisabled()
    {
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(false);
        var service = new TestableRefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepRunning_WhenCleanupThrows()
    {
        var secondAttempt = new TaskCompletionSource();
        var attempts = 0;
        _tokenServiceMock
            .Setup(x => x.CleanupExpiredTokensAsync())
            .Callback(() =>
            {
                if (Interlocked.Increment(ref attempts) >= 2)
                {
                    secondAttempt.TrySetResult();
                }
            })
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        await RunUntilAsync(secondAttempt.Task);

        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateAScopePerCleanup()
    {
        var secondCleanup = new TaskCompletionSource();
        var cleanups = 0;
        _tokenServiceMock
            .Setup(x => x.CleanupExpiredTokensAsync())
            .Callback(() =>
            {
                if (Interlocked.Increment(ref cleanups) >= 2)
                {
                    secondCleanup.TrySetResult();
                }
            })
            .Returns(Task.CompletedTask);

        await RunUntilAsync(secondCleanup.Task);

        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.AtLeast(3));
    }

    private sealed class TestableRefreshTokenCleanupService : RefreshTokenCleanupService
    {
        public TestableRefreshTokenCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<RefreshTokenCleanupService> logger) : base(scopeFactory, logger)
        {
        }

        protected override TimeSpan Interval => TimeSpan.FromMilliseconds(10);
    }
}
