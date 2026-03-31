using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Updates;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class UpdateServiceTests
{
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<IDockerHubApi> _dockerHubApiMock;
    private readonly Mock<ILogger<UpdateService>> _loggerMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly UpdateService _updateService;

    public UpdateServiceTests()
    {
        _configRepositoryMock = new Mock<IConfigRepository>();
        _dockerHubApiMock = new Mock<IDockerHubApi>();
        _loggerMock = new Mock<ILogger<UpdateService>>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);

        _updateService = new UpdateService(
            _configRepositoryMock.Object,
            _dockerHubApiMock.Object,
            _loggerMock.Object,
            _dateTimeProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _configRepositoryMock.VerifyNoOtherCalls();
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

        _configRepositoryMock
            .Setup(x => x.GetConfigsByPrefixAsync("update_"))
            .ReturnsAsync(config);

        // Act
        var result = await _updateService.GetVersionInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.LatestVersion.Should().Be("1.2.0");
        result.UpdateAvailable.Should().BeTrue();
        result.LastChecked.Should().NotBeNull();

        _configRepositoryMock.Verify(x => x.GetConfigsByPrefixAsync("update_"), Times.Once);
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

        _configRepositoryMock
            .Setup(x => x.GetConfigsByPrefixAsync("update_"))
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

        _configRepositoryMock
            .Setup(x => x.GetConfigsByPrefixAsync("update_"))
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
        _configRepositoryMock
            .Setup(x => x.GetConfigsByPrefixAsync("update_"))
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
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>("update_check_enabled"))
            .ReturnsAsync(true);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<VersionTrack>("update_track"))
            .ReturnsAsync(VersionTrack.Stable);

        // Act
        var result = await _updateService.GetUpdateSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Enabled.Should().BeTrue();
        result.VersionTrack.Should().Be(VersionTrack.Stable);
    }

    [Fact]
    public async Task GetUpdateSettingsAsync_ShouldReturnDefaultValues_WhenNoConfigExists()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>("update_check_enabled"))
            .ReturnsAsync(true);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<VersionTrack>("update_track"))
            .ReturnsAsync(VersionTrack.Stable);

        // Act
        var result = await _updateService.GetUpdateSettingsAsync();

        // Assert
        result.Enabled.Should().BeTrue(); // Default is enabled
    }

    [Fact]
    public async Task GetUpdateSettingsAsync_ShouldReturnDisabled_WhenExplicitlyFalse()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>("update_check_enabled"))
            .ReturnsAsync(false);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<VersionTrack>("update_track"))
            .ReturnsAsync(VersionTrack.Stable);

        // Act
        var result = await _updateService.GetUpdateSettingsAsync();

        // Assert
        result.Enabled.Should().BeFalse();
    }

    #endregion

    #region UpdateSettingsAsync Tests

    [Fact]
    public async Task UpdateSettingsAsync_ShouldUpdateSettings()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(It.IsAny<string>(), It.IsAny<VersionTrack>()))
            .Returns(Task.CompletedTask);

        // Act
        await _updateService.UpdateSettingsAsync(true, VersionTrack.Stable);

        // Assert
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync("update_check_enabled", true), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync("update_track", VersionTrack.Stable), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ShouldDisableUpdates()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(It.IsAny<string>(), It.IsAny<VersionTrack>()))
            .Returns(Task.CompletedTask);

        // Act
        await _updateService.UpdateSettingsAsync(false, VersionTrack.Beta);

        // Assert
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync("update_check_enabled", false), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync("update_track", VersionTrack.Beta), Times.Once);
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
