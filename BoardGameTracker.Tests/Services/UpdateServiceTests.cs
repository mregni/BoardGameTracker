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
