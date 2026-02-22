using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Settings;
using BoardGameTracker.Core.Updates.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class SettingsServiceTests
{
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<ILogger<SettingsService>> _loggerMock;
    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        _configRepositoryMock = new Mock<IConfigRepository>();
        _updateServiceMock = new Mock<IUpdateService>();
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _loggerMock = new Mock<ILogger<SettingsService>>();

        _settingsService = new SettingsService(
            _configRepositoryMock.Object,
            _updateServiceMock.Object,
            _environmentProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _configRepositoryMock.VerifyNoOtherCalls();
        _updateServiceMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    #region GetSettingsAsync Tests

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnSettings_WhenCalled()
    {
        // Arrange
        var configs = new Dictionary<string, string>
        {
            { "time_format", "HH:mm" },
            { "date_format", "yyyy-MM-dd" },
            { "ui_language", "en-US" },
            { "currency", "USD" },
            { "shelf_of_shame_enabled", "true" },
            { "shelf_of_shame_months", "6" },
            { "game_nights_enabled", "true" },
            { "public_url", "https://example.com" },
            { "rsvp_authentication_enabled", "true" },
            { "update_check_enabled", "true" },
            { "update_track", "stable" }
        };

        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(configs);

        _environmentProviderMock
            .Setup(x => x.EnableStatistics)
            .Returns(true);

        // Act
        var result = await _settingsService.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TimeFormat.Should().Be("HH:mm");
        result.DateFormat.Should().Be("yyyy-MM-dd");
        result.UiLanguage.Should().Be("en-US");
        result.Currency.Should().Be("USD");
        result.Statistics.Should().BeTrue();
        result.UpdateCheckEnabled.Should().BeTrue();
        result.VersionTrack.Should().Be(VersionTrack.Stable);
        result.ShelfOfShameEnabled.Should().BeTrue();
        result.ShelfOfShameMonthsLimit.Should().Be(6);
        result.GameNightsEnabled.Should().BeTrue();
        result.PublicUrl.Should().Be("https://example.com");

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _environmentProviderMock.Verify(x => x.EnableStatistics, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnSettings_WhenUpdateCheckIsDisabled()
    {
        // Arrange
        var configs = new Dictionary<string, string>
        {
            { "time_format", "hh:mm a" },
            { "date_format", "MM/dd/yyyy" },
            { "ui_language", "de-DE" },
            { "currency", "EUR" },
            { "shelf_of_shame_enabled", "false" },
            { "shelf_of_shame_months", "12" },
            { "game_nights_enabled", "false" },
            { "public_url", "https://test.com" },
            { "update_check_enabled", "false" },
            { "update_track", "beta" }
        };

        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(configs);

        _environmentProviderMock
            .Setup(x => x.EnableStatistics)
            .Returns(false);

        // Act
        var result = await _settingsService.GetSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TimeFormat.Should().Be("hh:mm a");
        result.DateFormat.Should().Be("MM/dd/yyyy");
        result.UiLanguage.Should().Be("de-DE");
        result.Currency.Should().Be("EUR");
        result.Statistics.Should().BeFalse();
        result.UpdateCheckEnabled.Should().BeFalse();
        result.VersionTrack.Should().Be(VersionTrack.Beta);
        result.ShelfOfShameEnabled.Should().BeFalse();
        result.ShelfOfShameMonthsLimit.Should().Be(12);
        result.GameNightsEnabled.Should().BeFalse();
        result.PublicUrl.Should().Be("https://test.com");

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _environmentProviderMock.Verify(x => x.EnableStatistics, Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateSettingsAsync Tests

    [Fact]
    public async Task UpdateSettingsAsync_ShouldUpdateAllSettings_WhenCalled()
    {
        var model = new UIResourceDto
        {
            TimeFormat = "HH:mm:ss",
            DateFormat = "dd-MM-yyyy",
            UiLanguage = "fr-FR",
            Currency = "GBP",
            UpdateCheckEnabled = true,
            VersionTrack = VersionTrack.Stable,
            ShelfOfShameEnabled = true,
            ShelfOfShameMonthsLimit = 8,
            GameNightsEnabled = true,
            PublicUrl = "https://myapp.com"
        };

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.Currency, model.Currency))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.TimeFormat, model.TimeFormat))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.DateFormat, model.DateFormat))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.UiLanguage, model.UiLanguage))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameEnabled, model.ShelfOfShameEnabled))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths, model.ShelfOfShameMonthsLimit))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.PublicUrl, model.PublicUrl))
            .Returns(Task.CompletedTask);

        _updateServiceMock
            .Setup(x => x.UpdateSettingsAsync(model.UpdateCheckEnabled, model.VersionTrack))
            .Returns(Task.CompletedTask);

        await _settingsService.UpdateSettingsAsync(model);

        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.Currency, model.Currency), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.TimeFormat, model.TimeFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.DateFormat, model.DateFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.UiLanguage, model.UiLanguage), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameEnabled, model.ShelfOfShameEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths, model.ShelfOfShameMonthsLimit), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.PublicUrl, model.PublicUrl), Times.Once);
        _updateServiceMock.Verify(x => x.UpdateSettingsAsync(model.UpdateCheckEnabled, model.VersionTrack), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateSettingsAsync_ShouldUpdateSettings_WhenDisablingUpdateCheck()
    {
        var model = new UIResourceDto
        {
            TimeFormat = "HH:mm",
            DateFormat = "yyyy/MM/dd",
            UiLanguage = "ja-JP",
            Currency = "JPY",
            UpdateCheckEnabled = false,
            VersionTrack = VersionTrack.Beta,
            ShelfOfShameEnabled = false,
            ShelfOfShameMonthsLimit = 3,
            GameNightsEnabled = false,
            PublicUrl = "https://localhost"
        };

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.Currency, model.Currency))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.TimeFormat, model.TimeFormat))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.DateFormat, model.DateFormat))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.UiLanguage, model.UiLanguage))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameEnabled, model.ShelfOfShameEnabled))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths, model.ShelfOfShameMonthsLimit))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.PublicUrl, model.PublicUrl))
            .Returns(Task.CompletedTask);

        _updateServiceMock
            .Setup(x => x.UpdateSettingsAsync(model.UpdateCheckEnabled, model.VersionTrack))
            .Returns(Task.CompletedTask);

        await _settingsService.UpdateSettingsAsync(model);

        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.Currency, model.Currency), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.TimeFormat, model.TimeFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.DateFormat, model.DateFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.UiLanguage, model.UiLanguage), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameEnabled, model.ShelfOfShameEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths, model.ShelfOfShameMonthsLimit), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.PublicUrl, model.PublicUrl), Times.Once);
        _updateServiceMock.Verify(x => x.UpdateSettingsAsync(model.UpdateCheckEnabled, model.VersionTrack), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
