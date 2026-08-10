using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Updates;
using BoardGameTracker.Core.Updates.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Tests.Services;

public class UpdateCheckBackgroundServiceTests
{
    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<UpdateCheckBackgroundService>> _loggerMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly Mock<IConfigRepository> _configRepositoryMock;

    public UpdateCheckBackgroundServiceTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<UpdateCheckBackgroundService>>();
        _updateServiceMock = new Mock<IUpdateService>();
        _configRepositoryMock = new Mock<IConfigRepository>();

        SetupServiceProvider();
        SetupConfig(enabled: true, intervalHours: 24);
    }

    private void SetupServiceProvider()
    {
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopedProviderMock = new Mock<IServiceProvider>();

        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(scopedProviderMock.Object);

        scopedProviderMock
            .Setup(x => x.GetService(typeof(IUpdateService)))
            .Returns(_updateServiceMock.Object);
        scopedProviderMock
            .Setup(x => x.GetService(typeof(IConfigRepository)))
            .Returns(_configRepositoryMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);
    }

    private void SetupConfig(bool enabled, int intervalHours)
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(UpdateConfig.CheckEnabled))
            .ReturnsAsync(enabled);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(UpdateConfig.CheckIntervalHours))
            .ReturnsAsync(intervalHours);
    }

    private static async Task RunUntilAsync(UpdateCheckBackgroundService service, Task signal)
    {
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
    public async Task ExecuteAsync_ShouldCheckForUpdates_WhenChecksAreEnabled()
    {
        var checkStarted = new TaskCompletionSource();
        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .Callback(() => checkStarted.TrySetResult())
            .Returns(Task.CompletedTask);

        await RunUntilAsync(CreateService(), checkStarted.Task);

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCheckForUpdates_WhenChecksAreDisabled()
    {
        var enabledRead = new TaskCompletionSource();
        SetupConfig(enabled: false, intervalHours: 24);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(UpdateConfig.CheckEnabled))
            .Callback(() => enabledRead.TrySetResult())
            .ReturnsAsync(false);

        await RunUntilAsync(CreateService(), enabledRead.Task);

        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(UpdateConfig.CheckEnabled), Times.AtLeastOnce);
        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistDefaultInterval_WhenConfiguredIntervalIsNotPositive()
    {
        var defaultPersisted = new TaskCompletionSource();
        SetupConfig(enabled: true, intervalHours: 0);
        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(UpdateConfig.CheckIntervalHours, 24))
            .Callback(() => defaultPersisted.TrySetResult())
            .Returns(Task.CompletedTask);

        await RunUntilAsync(CreateService(), defaultPersisted.Task);

        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckIntervalHours, 24), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotPersistDefaultInterval_WhenConfiguredIntervalIsPositive()
    {
        var intervalRead = new TaskCompletionSource();
        SetupConfig(enabled: true, intervalHours: 12);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(UpdateConfig.CheckIntervalHours))
            .Callback(() => intervalRead.TrySetResult())
            .ReturnsAsync(12);

        await RunUntilAsync(CreateService(), intervalRead.Task);

        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepRunning_WhenUpdateCheckThrows()
    {
        var secondAttempt = new TaskCompletionSource();
        var attempts = 0;
        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .Callback(() =>
            {
                if (Interlocked.Increment(ref attempts) >= 2)
                {
                    secondAttempt.TrySetResult();
                }
            })
            .ThrowsAsync(new InvalidOperationException("boom"));

        await RunUntilAsync(CreateService(), secondAttempt.Task);

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.AtLeast(2));
    }

    private TestableUpdateCheckBackgroundService CreateService() =>
        new(_serviceProviderMock.Object, _loggerMock.Object);

    private sealed class TestableUpdateCheckBackgroundService : UpdateCheckBackgroundService
    {
        public TestableUpdateCheckBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<UpdateCheckBackgroundService> logger) : base(serviceProvider, logger)
        {
        }

        protected override TimeSpan StartupDelay => TimeSpan.Zero;

        protected override TimeSpan ErrorRetryDelay => TimeSpan.FromMilliseconds(10);
    }
}
