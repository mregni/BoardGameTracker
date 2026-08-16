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
    private readonly Mock<IAiClientFactory> _aiClientFactoryMock = new();

    public ModelProvisioningBackgroundServiceTests()
    {
        var scopeMock = new Mock<IServiceScope>();
        var scopedProviderMock = new Mock<IServiceProvider>();

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(scopedProviderMock.Object);
        scopedProviderMock
            .Setup(x => x.GetService(typeof(IAiClientFactory)))
            .Returns(_aiClientFactoryMock.Object);
    }

    private async Task RunUntilAsync(Task signal)
    {
        var service = new TestableModelProvisioningBackgroundService(
            _scopeFactoryMock.Object,
            Mock.Of<ILogger<ModelProvisioningBackgroundService>>());
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
    public async Task ExecuteAsync_ShouldEnsureModelsAvailable_OnStartup()
    {
        var ensured = new TaskCompletionSource();
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Callback(() => ensured.TrySetResult())
            .Returns(Task.CompletedTask);

        await RunUntilAsync(ensured.Task);

        _aiClientFactoryMock.Verify(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopRetrying_OnceModelsAreEnsured()
    {
        var attempts = 0;
        _aiClientFactoryMock
            .Setup(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref attempts))
            .Returns(Task.CompletedTask);

        await RunUntilAsync(Task.Delay(200));

        attempts.Should().Be(1);
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

        await RunUntilAsync(secondAttempt.Task);

        _aiClientFactoryMock.Verify(x => x.EnsureModelsAvailableAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    private sealed class TestableModelProvisioningBackgroundService : ModelProvisioningBackgroundService
    {
        public TestableModelProvisioningBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ModelProvisioningBackgroundService> logger) : base(scopeFactory, logger)
        {
        }

        protected override TimeSpan RetryDelay => TimeSpan.FromMilliseconds(10);
    }
}
