using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Updates;
using BoardGameTracker.Core.Updates.Interfaces;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Tests.Updates;

public class UpdateCheckBackgroundServiceTests
{
    private const string StartedLog = "Update Check Background Service started";
    private const string StoppingLog = "Update Check Background Service is stopping";

    private static readonly TimeSpan SignalTimeout = TimeSpan.FromSeconds(5);

    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ILogger<UpdateCheckBackgroundService>> _loggerMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly TaskCompletionSource _intervalRead = new();

    public UpdateCheckBackgroundServiceTests()
    {
        _loggerMock = new Mock<ILogger<UpdateCheckBackgroundService>>();
        _updateServiceMock = new Mock<IUpdateService>();
        _configRepositoryMock = new Mock<IConfigRepository>();
        _serviceProvider = new ServiceCollection()
            .AddScoped(_ => _updateServiceMock.Object)
            .AddScoped(_ => _configRepositoryMock.Object)
            .BuildServiceProvider();

        SetupConfig(enabled: true, intervalHours: 24);
    }

    private void VerifyNoOtherCalls()
    {
        _loggerMock.VerifyNoOtherCalls();
        _updateServiceMock.VerifyNoOtherCalls();
        _configRepositoryMock.VerifyNoOtherCalls();
    }

    private void SetupConfig(bool enabled, int intervalHours)
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(UpdateConfig.CheckEnabled))
            .ReturnsAsync(enabled);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(UpdateConfig.CheckIntervalHours))
            .Callback(() => _intervalRead.TrySetResult())
            .ReturnsAsync(intervalHours);
    }

    private async Task RunOneCycleAsync(UpdateCheckBackgroundService service)
    {
        await service.StartAsync(CancellationToken.None);
        try
        {
            await _intervalRead.Task.WaitAsync(SignalTimeout);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    private void VerifyCycle(int enabledReads, int hours)
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(UpdateConfig.CheckEnabled), Times.Exactly(enabledReads));
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<int>(UpdateConfig.CheckIntervalHours), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, StartedLog);
        _loggerMock.VerifyLog(LogLevel.Information, $"Next update check in {hours} hours");
        _loggerMock.VerifyLog(LogLevel.Information, StoppingLog);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCheckForUpdates_WhenChecksAreEnabled()
    {
        await RunOneCycleAsync(CreateService());

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        VerifyCycle(enabledReads: 1, hours: 24);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCheckForUpdates_WhenChecksAreDisabled()
    {
        SetupConfig(enabled: false, intervalHours: 24);

        await RunOneCycleAsync(CreateService());

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Never);
        VerifyCycle(enabledReads: 1, hours: 24);
        _loggerMock.VerifyLog(LogLevel.Information, "Update checks are disabled");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteAsync_ShouldPersistDefaultInterval_WhenConfiguredIntervalIsNotPositive(int configured)
    {
        SetupConfig(enabled: true, intervalHours: configured);
        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(UpdateConfig.CheckIntervalHours, 24))
            .Returns(Task.CompletedTask);

        await RunOneCycleAsync(CreateService());

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        VerifyCycle(enabledReads: 1, hours: 24);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(UpdateConfig.CheckIntervalHours, 24), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(12, 12)]
    [InlineData(720, 720)]
    [InlineData(721, 720)]
    [InlineData(100_000, 720)]
    public async Task ExecuteAsync_ShouldWaitTheConfiguredHours_UpToThirtyDays(int configured, int expected)
    {
        SetupConfig(enabled: true, intervalHours: configured);

        await RunOneCycleAsync(CreateService());

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        VerifyCycle(enabledReads: 1, hours: expected);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryAfterTheErrorDelay_WhenUpdateCheckThrows()
    {
        var failure = new InvalidOperationException("boom");
        var attempts = 0;
        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .Returns(() => Interlocked.Increment(ref attempts) == 1 ? Task.FromException(failure) : Task.CompletedTask);

        await RunOneCycleAsync(CreateService());

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Exactly(2));
        VerifyCycle(enabledReads: 2, hours: 24);
        _loggerMock.VerifyLog(LogLevel.Error, "Error in Update Check Background Service", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldEndCancelled_WhenStoppedDuringTheErrorDelay()
    {
        var failure = new InvalidOperationException("boom");
        var attempted = new TaskCompletionSource();
        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .Callback(() => attempted.TrySetResult())
            .Returns(Task.FromException(failure));
        var service = new TestableUpdateCheckBackgroundService(_serviceProvider, _loggerMock.Object, TimeSpan.FromHours(1));

        await service.StartAsync(CancellationToken.None);
        await attempted.Task.WaitAsync(SignalTimeout);
        await service.StopAsync(CancellationToken.None);

        service.ExecuteTask!.IsCanceled.Should().BeTrue();
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(UpdateConfig.CheckEnabled), Times.Once);
        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, StartedLog);
        _loggerMock.VerifyLog(LogLevel.Error, "Error in Update Check Background Service", failure);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldWaitTheStartupDelay_BeforeTheFirstCheck()
    {
        var service = new DelayedStartService(_serviceProvider, _loggerMock.Object);
        using var stopping = new CancellationTokenSource();

        var run = service.RunAsync(stopping.Token);

        run.IsCompleted.Should().BeFalse();
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(UpdateConfig.CheckEnabled), Times.Never);

        await stopping.CancelAsync();
        var action = () => run;

        await action.Should().ThrowAsync<OperationCanceledException>();
        _loggerMock.VerifyLog(LogLevel.Information, StartedLog);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void Delays_ShouldBeOneMinuteAfterStartup_AndOneHourAfterAnError()
    {
        var service = new DefaultDelaysService(_serviceProvider, _loggerMock.Object);

        service.Startup.Should().Be(TimeSpan.FromMinutes(1));
        service.ErrorRetry.Should().Be(TimeSpan.FromHours(1));
        VerifyNoOtherCalls();
    }

    private TestableUpdateCheckBackgroundService CreateService() =>
        new(_serviceProvider, _loggerMock.Object, TimeSpan.FromMilliseconds(10));

    private sealed class DelayedStartService : UpdateCheckBackgroundService
    {
        public DelayedStartService(IServiceProvider serviceProvider, ILogger<UpdateCheckBackgroundService> logger)
            : base(serviceProvider, logger)
        {
        }

        protected override TimeSpan StartupDelay => TimeSpan.FromHours(1);

        public Task RunAsync(CancellationToken stoppingToken) => ExecuteAsync(stoppingToken);
    }

    private sealed class DefaultDelaysService : UpdateCheckBackgroundService
    {
        public DefaultDelaysService(IServiceProvider serviceProvider, ILogger<UpdateCheckBackgroundService> logger)
            : base(serviceProvider, logger)
        {
        }

        public TimeSpan Startup => StartupDelay;

        public TimeSpan ErrorRetry => ErrorRetryDelay;
    }

    private sealed class TestableUpdateCheckBackgroundService : UpdateCheckBackgroundService
    {
        private readonly TimeSpan _errorRetryDelay;

        public TestableUpdateCheckBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<UpdateCheckBackgroundService> logger,
            TimeSpan errorRetryDelay) : base(serviceProvider, logger)
        {
            _errorRetryDelay = errorRetryDelay;
        }

        protected override TimeSpan StartupDelay => TimeSpan.Zero;

        protected override TimeSpan ErrorRetryDelay => _errorRetryDelay;
    }
}
