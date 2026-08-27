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

    private TestableModelProvisioningBackgroundService CreateService(int retryDelayMs = 10, int maxAttempts = 40) =>
        new(_scopeFactoryMock.Object,
            Mock.Of<ILogger<ModelProvisioningBackgroundService>>(),
            TimeSpan.FromMilliseconds(retryDelayMs),
            maxAttempts);

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
    public async Task ExecuteAsync_ShouldGiveUpAndStop_AfterMaxAttemptsAllFail()
    {
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ollama not reachable"));

        var service = CreateService(maxAttempts: 3);
        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask.IsCompletedSuccessfully.Should().BeTrue();
        VerifyProvisioningAttempts(Times.Exactly(3));
        VerifyNoOtherCalls();
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
        private readonly int _maxAttempts;

        public TestableModelProvisioningBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ModelProvisioningBackgroundService> logger,
            TimeSpan retryDelay,
            int maxAttempts) : base(scopeFactory, logger)
        {
            _retryDelay = retryDelay;
            _maxAttempts = maxAttempts;
        }

        protected override TimeSpan RetryDelay => _retryDelay;
        protected override int MaxAttempts => _maxAttempts;
    }
}
