using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Rag.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Rag;

public class ModelProvisioningBackgroundServiceTests
{
    private const string Available = "AI models are available";

    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _scopedProviderMock = new();
    private readonly Mock<IAiClientFactory> _aiClientFactoryMock = new();
    private readonly Mock<ILogger<ModelProvisioningBackgroundService>> _loggerMock = new();

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
            _loggerMock.Object,
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

    private void VerifyRetryLogged(int attempt, TimeSpan delay, Exception failure)
    {
        _loggerMock.VerifyLog(LogLevel.Warning, $"Could not ensure AI models are available (attempt {attempt}), retrying in {delay}", failure);
    }

    private void VerifyNoOtherCalls()
    {
        _scopeFactoryMock.VerifyNoOtherCalls();
        _scopeMock.VerifyNoOtherCalls();
        _scopedProviderMock.VerifyNoOtherCalls();
        _aiClientFactoryMock.VerifyNoOtherCalls();
        _loggerMock.VerifyNoOtherCalls();
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
        service.RequestedDelays.Should().BeEmpty();
        _loggerMock.VerifyLog(LogLevel.Information, Available);
        VerifyProvisioningAttempts(Times.Once());
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetry_WhenOllamaIsNotReachableYet()
    {
        var failure = new InvalidOperationException("ollama not reachable");
        _aiClientFactoryMock
            .SetupSequence(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.RequestedDelays.Should().Equal(TimeSpan.FromMilliseconds(10));
        VerifyRetryLogged(1, TimeSpan.FromMilliseconds(10), failure);
        _loggerMock.VerifyLog(LogLevel.Information, Available);
        VerifyProvisioningAttempts(Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepRetrying_UntilStopped_WhenEveryAttemptFails()
    {
        var attempts = 0;
        var failure = new InvalidOperationException("ollama not reachable");
        var fifthAttempt = new TaskCompletionSource();
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (++attempts == 5)
                {
                    fifthAttempt.TrySetResult();
                }

                return Task.FromException(failure);
            });

        var service = CreateService();
        await service.StartAsync(CancellationToken.None);
        await fifthAttempt.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        attempts.Should().BeGreaterThanOrEqualTo(5);
        VerifyRetryLogged(1, TimeSpan.FromMilliseconds(10), failure);
        for (var attempt = 2; attempt <= attempts; attempt++)
        {
            VerifyRetryLogged(attempt, TimeSpan.FromMilliseconds(20), failure);
        }

        VerifyProvisioningAttempts(Times.Exactly(attempts));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDoubleTheDelay_BetweenFailedAttempts_UpToTheMaximum()
    {
        var attempts = 0;
        var failure = new InvalidOperationException("ollama not reachable");
        var fifthAttempt = new TaskCompletionSource();
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                if (++attempts < 5)
                {
                    throw failure;
                }

                fifthAttempt.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            });

        var service = new TestableModelProvisioningBackgroundService(
            _scopeFactoryMock.Object,
            _loggerMock.Object,
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(60),
            skipDelays: true);
        await service.StartAsync(CancellationToken.None);
        await fifthAttempt.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.RequestedDelays.Should().Equal(
            TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
        VerifyRetryLogged(1, TimeSpan.FromSeconds(15), failure);
        VerifyRetryLogged(2, TimeSpan.FromSeconds(30), failure);
        VerifyRetryLogged(3, TimeSpan.FromSeconds(60), failure);
        VerifyRetryLogged(4, TimeSpan.FromSeconds(60), failure);
        VerifyProvisioningAttempts(Times.Exactly(5));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStartAt15Seconds_AndGrowToFiveMinutes_ByDefault()
    {
        var attempts = 0;
        var failure = new InvalidOperationException("ollama not reachable");
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Returns(() => ++attempts < 8 ? Task.FromException(failure) : Task.CompletedTask);

        var service = new DefaultDelaysService(_scopeFactoryMock.Object, _loggerMock.Object);
        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.RequestedDelays.Should().Equal(
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(120),
            TimeSpan.FromSeconds(240),
            TimeSpan.FromSeconds(300),
            TimeSpan.FromSeconds(300));
        for (var attempt = 1; attempt <= 7; attempt++)
        {
            VerifyRetryLogged(attempt, service.RequestedDelays[attempt - 1], failure);
        }

        _loggerMock.VerifyLog(LogLevel.Information, Available);
        VerifyProvisioningAttempts(Times.Exactly(8));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopWithoutRetrying_WhenTheServiceStopsDuringAnAttempt()
    {
        var started = new TaskCompletionSource();
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            });

        var service = CreateService(retryDelayMs: 10);
        await service.StartAsync(CancellationToken.None);
        await started.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        service.RequestedDelays.Should().BeEmpty();
        VerifyProvisioningAttempts(Times.Once());
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetry_WhenAnAttemptTimesOut()
    {
        var failure = new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout", new TimeoutException());
        _aiClientFactoryMock
            .SetupSequence(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.RequestedDelays.Should().Equal(TimeSpan.FromMilliseconds(10));
        VerifyRetryLogged(1, TimeSpan.FromMilliseconds(10), failure);
        _loggerMock.VerifyLog(LogLevel.Information, Available);
        VerifyProvisioningAttempts(Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopCleanly_WhenServiceIsStoppedDuringRetryDelay()
    {
        var failure = new InvalidOperationException("ollama not reachable");
        var firstAttempt = new TaskCompletionSource();
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Callback(() => firstAttempt.TrySetResult())
            .ThrowsAsync(failure);

        var service = CreateService(retryDelayMs: 300000);
        await service.StartAsync(CancellationToken.None);
        await firstAttempt.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask!.IsCompletedSuccessfully.Should().BeTrue();
        VerifyRetryLogged(1, TimeSpan.FromMilliseconds(300000), failure);
        VerifyProvisioningAttempts(Times.Once());
        VerifyNoOtherCalls();
    }

    private sealed class DefaultDelaysService : ModelProvisioningBackgroundService
    {
        public DefaultDelaysService(IServiceScopeFactory scopeFactory, ILogger<ModelProvisioningBackgroundService> logger)
            : base(scopeFactory, logger)
        {
        }

        public List<TimeSpan> RequestedDelays { get; } = [];

        protected override Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            RequestedDelays.Add(delay);
            return Task.CompletedTask;
        }
    }

    private sealed class TestableModelProvisioningBackgroundService : ModelProvisioningBackgroundService
    {
        private readonly TimeSpan _retryDelay;
        private readonly TimeSpan _maxRetryDelay;
        private readonly bool _skipDelays;

        public TestableModelProvisioningBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ModelProvisioningBackgroundService> logger,
            TimeSpan retryDelay,
            TimeSpan maxRetryDelay,
            bool skipDelays = false) : base(scopeFactory, logger)
        {
            _retryDelay = retryDelay;
            _maxRetryDelay = maxRetryDelay;
            _skipDelays = skipDelays;
        }

        public List<TimeSpan> RequestedDelays { get; } = [];

        protected override TimeSpan RetryDelay => _retryDelay;
        protected override TimeSpan MaxRetryDelay => _maxRetryDelay;

        protected override Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            RequestedDelays.Add(delay);
            return _skipDelays ? Task.CompletedTask : base.DelayAsync(delay, cancellationToken);
        }
    }
}
