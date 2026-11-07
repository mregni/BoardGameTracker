using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class RefreshTokenCleanupServiceTests
{
    private const string CompletedMessage = "Refresh token cleanup completed";
    private const string FailedMessage = "Error during refresh token cleanup";
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _scopedProviderMock = new();
    private readonly Mock<ILogger<RefreshTokenCleanupService>> _loggerMock = new();
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IOidcService> _oidcServiceMock = new();

    public RefreshTokenCleanupServiceTests()
    {
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_scopedProviderMock.Object);
        _scopedProviderMock.Setup(x => x.GetService(typeof(IEnvironmentProvider))).Returns(_environmentProviderMock.Object);
        _scopedProviderMock.Setup(x => x.GetService(typeof(ITokenService))).Returns(_tokenServiceMock.Object);
        _scopedProviderMock.Setup(x => x.GetService(typeof(IOidcService))).Returns(_oidcServiceMock.Object);
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(true);
        _oidcServiceMock.Setup(x => x.CleanupExpiredSessionsAsync()).Returns(Task.CompletedTask);
    }

    private void VerifyNoOtherCalls()
    {
        _scopeFactoryMock.VerifyNoOtherCalls();
        _scopeMock.VerifyNoOtherCalls();
        _scopedProviderMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
        _tokenServiceMock.VerifyNoOtherCalls();
        _oidcServiceMock.VerifyNoOtherCalls();
    }

    private void VerifyAuthCheck()
    {
        _environmentProviderMock.Verify(x => x.AuthEnabled, Times.Once);
        _scopedProviderMock.Verify(x => x.GetService(typeof(IEnvironmentProvider)), Times.Once);
    }

    private void VerifyScopes(int count)
    {
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Exactly(count));
        _scopeMock.Verify(x => x.Dispose(), Times.Exactly(count));
    }

    private async Task RunUntilAsync(Task signal, TimeSpan? interval = null)
    {
        var service = new TestableRefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object, interval);
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
    public async Task ExecuteAsync_ShouldCleanUpTokensAndOidcSessionsRightAtStartup_InAScopeOfItsOwn()
    {
        var cleanupStarted = new TaskCompletionSource();
        _tokenServiceMock
            .Setup(x => x.CleanupExpiredTokensAsync())
            .Callback(() => cleanupStarted.TrySetResult())
            .Returns(Task.CompletedTask);

        await RunUntilAsync(cleanupStarted.Task, TimeSpan.FromHours(1));

        VerifyAuthCheck();
        VerifyScopes(2);
        _scopeMock.Verify(x => x.ServiceProvider, Times.Exactly(3));
        _scopedProviderMock.Verify(x => x.GetService(typeof(ITokenService)), Times.Once);
        _scopedProviderMock.Verify(x => x.GetService(typeof(IOidcService)), Times.Once);
        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.Once);
        _oidcServiceMock.Verify(x => x.CleanupExpiredSessionsAsync(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, CompletedMessage);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNeverCleanUp_WhenAuthIsDisabled()
    {
        _environmentProviderMock.Setup(x => x.AuthEnabled).Returns(false);
        var service = new TestableRefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue();
        VerifyAuthCheck();
        VerifyScopes(1);
        _scopeMock.Verify(x => x.ServiceProvider, Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Auth disabled, skipping refresh token cleanup service");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepRunning_WhenTheTokenCleanupThrows()
    {
        var secondAttempt = new TaskCompletionSource();
        var failure = new InvalidOperationException("database unavailable");
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
            .ThrowsAsync(failure);

        await RunUntilAsync(secondAttempt.Task);

        attempts.Should().BeGreaterThanOrEqualTo(2);
        VerifyAuthCheck();
        VerifyScopes(attempts + 1);
        _scopeMock.Verify(x => x.ServiceProvider, Times.Exactly(attempts + 1));
        _scopedProviderMock.Verify(x => x.GetService(typeof(ITokenService)), Times.Exactly(attempts));
        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.Exactly(attempts));
        _loggerMock.VerifyLog(LogLevel.Error, FailedMessage, failure, Times.Exactly(attempts));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepRunning_WhenTheOidcCleanupThrows()
    {
        var secondAttempt = new TaskCompletionSource();
        var failure = new InvalidOperationException("database unavailable");
        var attempts = 0;
        _tokenServiceMock.Setup(x => x.CleanupExpiredTokensAsync()).Returns(Task.CompletedTask);
        _oidcServiceMock
            .Setup(x => x.CleanupExpiredSessionsAsync())
            .Callback(() =>
            {
                if (Interlocked.Increment(ref attempts) >= 2)
                {
                    secondAttempt.TrySetResult();
                }
            })
            .ThrowsAsync(failure);

        await RunUntilAsync(secondAttempt.Task);

        attempts.Should().BeGreaterThanOrEqualTo(2);
        VerifyAuthCheck();
        VerifyScopes(attempts + 1);
        _scopeMock.Verify(x => x.ServiceProvider, Times.Exactly((2 * attempts) + 1));
        _scopedProviderMock.Verify(x => x.GetService(typeof(ITokenService)), Times.Exactly(attempts));
        _scopedProviderMock.Verify(x => x.GetService(typeof(IOidcService)), Times.Exactly(attempts));
        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.Exactly(attempts));
        _oidcServiceMock.Verify(x => x.CleanupExpiredSessionsAsync(), Times.Exactly(attempts));
        _loggerMock.VerifyLog(LogLevel.Error, FailedMessage, failure, Times.Exactly(attempts));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopWithoutLoggingAnError_WhenTheCleanupIsCancelled()
    {
        _tokenServiceMock.Setup(x => x.CleanupExpiredTokensAsync()).ThrowsAsync(new OperationCanceledException());
        var service = new TestableRefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);

        await service.StartAsync(CancellationToken.None);
        var act = () => service.ExecuteTask!.WaitAsync(SignalTimeout);

        await act.Should().ThrowAsync<OperationCanceledException>();
        VerifyAuthCheck();
        VerifyScopes(2);
        _scopeMock.Verify(x => x.ServiceProvider, Times.Exactly(2));
        _scopedProviderMock.Verify(x => x.GetService(typeof(ITokenService)), Times.Once);
        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.Once);
        VerifyNoOtherCalls();
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

        cleanups.Should().BeGreaterThanOrEqualTo(2);
        VerifyAuthCheck();
        VerifyScopes(cleanups + 1);
        _scopeMock.Verify(x => x.ServiceProvider, Times.Exactly((2 * cleanups) + 1));
        _scopedProviderMock.Verify(x => x.GetService(typeof(ITokenService)), Times.Exactly(cleanups));
        _scopedProviderMock.Verify(x => x.GetService(typeof(IOidcService)), Times.Exactly(cleanups));
        _tokenServiceMock.Verify(x => x.CleanupExpiredTokensAsync(), Times.Exactly(cleanups));
        _oidcServiceMock.Verify(x => x.CleanupExpiredSessionsAsync(), Times.Exactly(cleanups));
        _loggerMock.VerifyLog(LogLevel.Information, CompletedMessage, Times.Exactly(cleanups));
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Interval_ShouldBeOneDay()
    {
        var service = new TestableRefreshTokenCleanupService(_scopeFactoryMock.Object, _loggerMock.Object);

        service.DefaultInterval.Should().Be(TimeSpan.FromHours(24));
        VerifyNoOtherCalls();
    }

    private sealed class TestableRefreshTokenCleanupService : RefreshTokenCleanupService
    {
        private readonly TimeSpan _interval;

        public TestableRefreshTokenCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<RefreshTokenCleanupService> logger,
            TimeSpan? interval = null) : base(scopeFactory, logger)
        {
            _interval = interval ?? TimeSpan.FromMilliseconds(10);
        }

        public TimeSpan DefaultInterval => base.Interval;

        protected override TimeSpan Interval => _interval;
    }
}
