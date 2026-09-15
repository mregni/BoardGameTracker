using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class ModelProvisioningBackgroundServiceTests
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _scopedProviderMock = new();
    private readonly Mock<IAiClientFactory> _aiClientFactoryMock = new();

    public ModelProvisioningBackgroundServiceTests()
    {
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_scopedProviderMock.Object);
        _scopedProviderMock
            .Setup(x => x.GetService(typeof(IAiClientFactory)))
            .Returns(_aiClientFactoryMock.Object);
    }

    private TestableModelProvisioningBackgroundService CreateService(int retryDelayMs = 10, int maxRetryDelayMs = 20) =>
        new(_scopeFactoryMock.Object,
            Mock.Of<ILogger<ModelProvisioningBackgroundService>>(),
            TimeSpan.FromMilliseconds(retryDelayMs),
            TimeSpan.FromMilliseconds(maxRetryDelayMs));

    private void VerifyProvisioningAttempts(Times times)
    {
        _scopeFactoryMock.Verify(x => x.CreateScope(), times);
        _scopeMock.VerifyGet(x => x.ServiceProvider, times);
        _scopeMock.Verify(x => x.Dispose(), times);
        _scopedProviderMock.Verify(x => x.GetService(typeof(IAiClientFactory)), times);
        _aiClientFactoryMock.Verify(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()), times);
    }

    private void VerifyNoOtherCalls()
    {
        _scopeFactoryMock.VerifyNoOtherCalls();
        _scopeMock.VerifyNoOtherCalls();
        _scopedProviderMock.VerifyNoOtherCalls();
        _aiClientFactoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldEnsureModelsExactlyOnceAndStop_WhenFirstAttemptSucceeds()
    {
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(retryDelayMs: 300000);
        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue();
        VerifyProvisioningAttempts(Times.Once());
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetry_WhenOllamaIsNotReachableYet()
    {
        var secondAttempt = new TaskCompletionSource();
        var attempts = 0;
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                if (Interlocked.Increment(ref attempts) >= 2)
                {
                    secondAttempt.TrySetResult();
                }
            })
            .ThrowsAsync(new InvalidOperationException("ollama not reachable"));

        var service = CreateService();
        await service.StartAsync(CancellationToken.None);
        await secondAttempt.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        VerifyProvisioningAttempts(Times.AtLeast(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepRetrying_UntilStopped_WhenEveryAttemptFails()
    {
        var attempts = 0;
        var fifthAttempt = new TaskCompletionSource();
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (++attempts == 5)
                {
                    fifthAttempt.TrySetResult();
                }

                return Task.FromException(new InvalidOperationException("ollama not reachable"));
            });

        var service = CreateService();
        await service.StartAsync(CancellationToken.None);
        await fifthAttempt.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        VerifyProvisioningAttempts(Times.AtLeast(5));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBackOff_BetweenFailedAttempts()
    {
        var timestamps = new System.Collections.Generic.List<DateTime>();
        var fourthAttempt = new TaskCompletionSource();
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                timestamps.Add(DateTime.UtcNow);
                if (timestamps.Count == 4)
                {
                    fourthAttempt.TrySetResult();
                }

                return Task.FromException(new InvalidOperationException("ollama not reachable"));
            });

        var service = CreateService(retryDelayMs: 50, maxRetryDelayMs: 1000);
        await service.StartAsync(CancellationToken.None);
        await fourthAttempt.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        var gaps = new[] { timestamps[1] - timestamps[0], timestamps[2] - timestamps[1], timestamps[3] - timestamps[2] };
        gaps[1].Should().BeGreaterThan(gaps[0]);
        gaps[2].Should().BeGreaterThan(gaps[1]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopWithoutRetrying_WhenEnsuringModelsIsCancelled()
    {
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var service = CreateService(retryDelayMs: 300000);
        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue();
        VerifyProvisioningAttempts(Times.Once());
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopCleanly_WhenServiceIsStoppedDuringRetryDelay()
    {
        var firstAttempt = new TaskCompletionSource();
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Callback(() => firstAttempt.TrySetResult())
            .ThrowsAsync(new InvalidOperationException("ollama not reachable"));

        var service = CreateService(retryDelayMs: 300000);
        await service.StartAsync(CancellationToken.None);
        await firstAttempt.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        VerifyProvisioningAttempts(Times.Once());
        VerifyNoOtherCalls();
    }

    private sealed class TestableModelProvisioningBackgroundService : ModelProvisioningBackgroundService
    {
        private readonly TimeSpan _retryDelay;
        private readonly TimeSpan _maxRetryDelay;

        public TestableModelProvisioningBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ModelProvisioningBackgroundService> logger,
            TimeSpan retryDelay,
            TimeSpan maxRetryDelay) : base(scopeFactory, logger)
        {
            _retryDelay = retryDelay;
            _maxRetryDelay = maxRetryDelay;
        }

        protected override TimeSpan RetryDelay => _retryDelay;
        protected override TimeSpan MaxRetryDelay => _maxRetryDelay;
    }
}
