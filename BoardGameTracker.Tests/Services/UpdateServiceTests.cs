using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Updates;
using BoardGameTracker.Core.Updates.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class UpdateServiceTests
{
    private readonly Mock<IUpdateRepository> _updateRepositoryMock;
    private readonly Mock<IDockerHubApi> _dockerHubApiMock;
    private readonly Mock<ILogger<UpdateService>> _loggerMock;
    private readonly UpdateService _updateService;

    public UpdateServiceTests()
    {
        _updateRepositoryMock = new Mock<IUpdateRepository>();
        _dockerHubApiMock = new Mock<IDockerHubApi>();
        _loggerMock = new Mock<ILogger<UpdateService>>();

        _updateService = new UpdateService(
            _updateRepositoryMock.Object,
            _dockerHubApiMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _updateRepositoryMock.VerifyNoOtherCalls();
        _dockerHubApiMock.VerifyNoOtherCalls();
    }

    #region GetUpdateStatusAsync Tests

    [Fact]
    public async Task GetUpdateStatusAsync_ShouldReturnUpdateStatus_WhenConfigExists()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            { "update_available_version", "1.2.0" },
            { "update_available", "true" },
            { "update_check_last_run", DateTime.UtcNow.ToString("O") }
        };

        _updateRepositoryMock
            .Setup(x => x.GetUpdateConfigAsync())
            .ReturnsAsync(config);

        // Act
        var result = await _updateService.GetVersionInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.LatestVersion.Should().Be("1.2.0");
        result.UpdateAvailable.Should().BeTrue();
        result.LastChecked.Should().NotBeNull();

        _updateRepositoryMock.Verify(x => x.GetUpdateConfigAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUpdateStatusAsync_ShouldReturnNoUpdateAvailable_WhenConfigIsFalse()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            { "update_available", "false" }
        };

        _updateRepositoryMock
            .Setup(x => x.GetUpdateConfigAsync())
            .ReturnsAsync(config);

        // Act
        var result = await _updateService.GetVersionInfoAsync();

        // Assert
        result.UpdateAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GetUpdateStatusAsync_ShouldIncludeErrorMessage_WhenPresent()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            { "update_check_error", "Network error occurred" }
        };

        _updateRepositoryMock
            .Setup(x => x.GetUpdateConfigAsync())
            .ReturnsAsync(config);

        // Act
        var result = await _updateService.GetVersionInfoAsync();

        // Assert
        result.ErrorMessage.Should().Be("Network error occurred");
    }

    [Fact]
    public async Task GetUpdateStatusAsync_ShouldReturnEmptyConfig_WhenNoConfigExists()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetUpdateConfigAsync())
            .ReturnsAsync(new Dictionary<string, string>());

        // Act
        var result = await _updateService.GetVersionInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.LatestVersion.Should().BeNull();
        result.UpdateAvailable.Should().BeFalse();
        result.LastChecked.Should().BeNull();
    }

    #endregion

    #region GetUpdateSettingsAsync Tests

    [Fact]
    public async Task GetUpdateSettingsAsync_ShouldReturnSettings_WhenConfigExists()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_enabled"))
            .ReturnsAsync("true");

        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_interval_hours"))
            .ReturnsAsync("12");

        // Act
        var result = await _updateService.GetUpdateSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Enabled.Should().BeTrue();
        result.IntervalHours.Should().Be(12);
    }

    [Fact]
    public async Task GetUpdateSettingsAsync_ShouldReturnDefaultValues_WhenNoConfigExists()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_enabled"))
            .ReturnsAsync((string?)null);

        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_interval_hours"))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _updateService.GetUpdateSettingsAsync();

        // Assert
        result.Enabled.Should().BeTrue(); // Default is enabled when not "false"
        result.IntervalHours.Should().Be(24); // Default is 24 hours
    }

    [Fact]
    public async Task GetUpdateSettingsAsync_ShouldReturnDisabled_WhenExplicitlyFalse()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_enabled"))
            .ReturnsAsync("false");

        _updateRepositoryMock
            .Setup(x => x.GetConfigValueAsync("update_check_interval_hours"))
            .ReturnsAsync("48");

        // Act
        var result = await _updateService.GetUpdateSettingsAsync();

        // Assert
        result.Enabled.Should().BeFalse();
        result.IntervalHours.Should().Be(48);
    }

    #endregion

    #region UpdateSettingsAsync Tests

    [Fact]
    public async Task UpdateSettingsAsync_ShouldUpdateSettings()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.SetConfigValueAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _updateService.UpdateSettingsAsync(true, 12);

        // Assert
        _updateRepositoryMock.Verify(x => x.SetConfigValueAsync("update_check_enabled", "true"), Times.Once);
        _updateRepositoryMock.Verify(x => x.SetConfigValueAsync("update_check_interval_hours", "12"), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ShouldDisableUpdates()
    {
        // Arrange
        _updateRepositoryMock
            .Setup(x => x.SetConfigValueAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _updateService.UpdateSettingsAsync(false, 24);

        // Assert
        _updateRepositoryMock.Verify(x => x.SetConfigValueAsync("update_check_enabled", "false"), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ShouldThrowException_WhenIntervalLessThanOne()
    {
        // Act
        var action = async () => await _updateService.UpdateSettingsAsync(true, 0);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Interval must be at least 1 hour*");
    }

    [Fact]
    public async Task UpdateSettingsAsync_ShouldThrowException_WhenIntervalNegative()
    {
        // Act
        var action = async () => await _updateService.UpdateSettingsAsync(true, -5);

        // Assert
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Interval must be at least 1 hour*");
    }

    #endregion

    #region GetCurrentVersion Tests

    [Fact]
    public void GetCurrentVersion_ShouldReturnVersion()
    {
        // Act
        var result = _updateService.GetCurrentVersion();

        // Assert
        result.Should().NotBeNullOrEmpty();
        // Version format should be x.x.x
        result.Should().MatchRegex(@"^\d+\.\d+\.\d+");
    }

    #endregion
}
