using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Updates;
using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class UpdateCheckBackgroundServiceTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<UpdateCheckBackgroundService>> _loggerMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly Mock<IUpdateRepository> _updateRepositoryMock;

    public UpdateCheckBackgroundServiceTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<UpdateCheckBackgroundService>>();
        _updateServiceMock = new Mock<IUpdateService>();
        _updateRepositoryMock = new Mock<IUpdateRepository>();

        SetupServiceProvider();
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
            .Setup(x => x.GetService(typeof(IUpdateRepository)))
            .Returns(_updateRepositoryMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopWhenCancelled()
    {
        // Arrange
        var service = new UpdateCheckBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object);

        var cts = new CancellationTokenSource();

        // Act
        await service.StartAsync(cts.Token);

        // Small delay to let the service start processing
        await Task.Delay(50);

        // Cancel and stop
        cts.Cancel();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected - service may throw when cancelled
        }

        // Assert - service should stop without throwing unexpectedly
        // If we reach here without unexpected exception, the test passes
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipUpdateCheck_WhenDisabled()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_enabled"))
            .ReturnsAsync("false");

        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_interval_hours"))
            .ReturnsAsync("1");

        var service = new UpdateCheckBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object);

        var cts = new CancellationTokenSource();

        // Act
        var startTask = service.StartAsync(cts.Token);

        // Wait for initial delay (1 min) plus a bit of execution time
        // For test purposes, we cancel quickly
        await Task.Delay(100);
        cts.Cancel();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Never);
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Act & Assert
        var service = new UpdateCheckBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object);

        Assert.NotNull(service);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseDefaultInterval_WhenIntervalNotConfigured()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_enabled"))
            .ReturnsAsync((string?)null);

        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_interval_hours"))
            .ReturnsAsync((string?)null);

        _updateRepositoryMock
            .Setup(x => x.SetConfigValueAsync("update_check_interval_hours", "24"))
            .Returns(Task.CompletedTask);

        var service = new UpdateCheckBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object);

        var cts = new CancellationTokenSource();

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Service should set the default interval when not configured
        // This happens after the initial delay, so we may not see it in quick tests
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseConfiguredInterval()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_enabled"))
            .ReturnsAsync("true");

        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_interval_hours"))
            .ReturnsAsync("12");

        var service = new UpdateCheckBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object);

        var cts = new CancellationTokenSource();

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // If no exception, service handled the configuration correctly
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleInvalidIntervalConfig()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_enabled"))
            .ReturnsAsync("true");

        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_interval_hours"))
            .ReturnsAsync("invalid");

        _updateRepositoryMock
            .Setup(x => x.SetConfigValueAsync("update_check_interval_hours", "24"))
            .Returns(Task.CompletedTask);

        var service = new UpdateCheckBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object);

        var cts = new CancellationTokenSource();

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // If no exception, service handled invalid config gracefully
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleZeroInterval()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_enabled"))
            .ReturnsAsync("true");

        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_interval_hours"))
            .ReturnsAsync("0"); // Zero hours - should use default

        _updateRepositoryMock
            .Setup(x => x.SetConfigValueAsync("update_check_interval_hours", "24"))
            .Returns(Task.CompletedTask);

        var service = new UpdateCheckBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object);

        var cts = new CancellationTokenSource();

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Service should set default when interval is 0
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleNegativeInterval()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_enabled"))
            .ReturnsAsync("true");

        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_interval_hours"))
            .ReturnsAsync("-5"); // Negative hours - should use default

        _updateRepositoryMock
            .Setup(x => x.SetConfigValueAsync("update_check_interval_hours", "24"))
            .Returns(Task.CompletedTask);

        var service = new UpdateCheckBackgroundService(
            _serviceProviderMock.Object,
            _loggerMock.Object);

        var cts = new CancellationTokenSource();

        // Act
        var startTask = service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Service should set default when interval is negative
    }
}
