using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class SettingsServiceTests
{
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<ILogger<SettingsService>> _loggerMock;
    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        _configRepositoryMock = new Mock<IConfigRepository>();
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey))
            .ReturnsAsync(string.Empty);
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _loggerMock = new Mock<ILogger<SettingsService>>();

        _settingsService = new SettingsService(
            _configRepositoryMock.Object,
            _environmentProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.AtMostOnce());
        _configRepositoryMock.VerifyNoOtherCalls();
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
            .Setup(x => x.StatisticsEnabled)
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
        result.RsvpAuthenticationEnabled.Should().BeTrue();

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _environmentProviderMock.Verify(x => x.StatisticsEnabled, Times.Once);
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
            { "rsvp_authentication_enabled", "false" },
            { "update_check_enabled", "false" },
            { "update_track", "beta" }
        };

        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(configs);

        _environmentProviderMock
            .Setup(x => x.StatisticsEnabled)
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
        result.RsvpAuthenticationEnabled.Should().BeFalse();

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _environmentProviderMock.Verify(x => x.StatisticsEnabled, Times.Once);
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
            PublicUrl = "https://myapp.com",
            RsvpAuthenticationEnabled = true
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

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.RsvpAuthenticationEnabled, model.RsvpAuthenticationEnabled))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.UpdateConfig.CheckEnabled, model.UpdateCheckEnabled))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.UpdateConfig.Track, model.VersionTrack))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.BggConfig.ApiKey, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await _settingsService.UpdateSettingsAsync(model);

        result.Should().NotBeNull();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.Currency, model.Currency), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.TimeFormat, model.TimeFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.DateFormat, model.DateFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.UiLanguage, model.UiLanguage), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameEnabled, model.ShelfOfShameEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths, model.ShelfOfShameMonthsLimit), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.PublicUrl, model.PublicUrl), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.RsvpAuthenticationEnabled, model.RsvpAuthenticationEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.UpdateConfig.CheckEnabled, model.UpdateCheckEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.UpdateConfig.Track, model.VersionTrack), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.BggConfig.ApiKey, It.IsAny<string>()), Times.Once);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _environmentProviderMock.VerifyGet(x => x.StatisticsEnabled, Times.Once);
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
            PublicUrl = "https://localhost",
            RsvpAuthenticationEnabled = false
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

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.AppConfig.RsvpAuthenticationEnabled, model.RsvpAuthenticationEnabled))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.UpdateConfig.CheckEnabled, model.UpdateCheckEnabled))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.UpdateConfig.Track, model.VersionTrack))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.SetConfigValueAsync(Constants.BggConfig.ApiKey, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await _settingsService.UpdateSettingsAsync(model);

        result.Should().NotBeNull();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.Currency, model.Currency), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.TimeFormat, model.TimeFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.DateFormat, model.DateFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.UiLanguage, model.UiLanguage), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameEnabled, model.ShelfOfShameEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths, model.ShelfOfShameMonthsLimit), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, model.GameNightsEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.PublicUrl, model.PublicUrl), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.RsvpAuthenticationEnabled, model.RsvpAuthenticationEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.UpdateConfig.CheckEnabled, model.UpdateCheckEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.UpdateConfig.Track, model.VersionTrack), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.BggConfig.ApiKey, It.IsAny<string>()), Times.Once);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _environmentProviderMock.VerifyGet(x => x.StatisticsEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetBggApiKeyAsync Tests

    private static async Task<T> WithBggEnvApiKey<T>(string? value, Func<Task<T>> action)
    {
        var original = Environment.GetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName);
        Environment.SetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName, value);
        try
        {
            return await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(Constants.BggConfig.EnvApiKeyName, original);
        }
    }

    [Fact]
    public async Task GetBggApiKeyAsync_ShouldReturnDbValue_WhenEnvVariableNotSet()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey))
            .ReturnsAsync("db-api-key");

        // Act
        var result = await WithBggEnvApiKey(null, () => _settingsService.GetBggApiKeyAsync());

        // Assert
        result.Should().Be("db-api-key");
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBggApiKeyAsync_ShouldReturnTrimmedEnvValue_WhenEnvVariableSet()
    {
        // Act
        var result = await WithBggEnvApiKey("  env-api-key  ", () => _settingsService.GetBggApiKeyAsync());

        // Assert
        result.Should().Be("env-api-key");
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBggApiKeyAsync_ShouldReturnNull_WhenNeitherEnvNorDbValueSet()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey))
            .ReturnsAsync((string)null!);

        // Act
        var result = await WithBggEnvApiKey(null, () => _settingsService.GetBggApiKeyAsync());

        // Assert
        result.Should().BeNull();
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region IsBggEnabled Tests

    [Fact]
    public async Task IsBggEnabled_ShouldReturnTrue_WhenApiKeyIsConfigured()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey))
            .ReturnsAsync("some-api-key");

        // Act
        var result = await WithBggEnvApiKey(null, () => _settingsService.IsBggEnabled());

        // Assert
        result.Should().BeTrue();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsBggEnabled_ShouldReturnFalse_WhenApiKeyIsEmpty()
    {
        // Act (constructor default returns string.Empty for the API key)
        var result = await WithBggEnvApiKey(null, () => _settingsService.IsBggEnabled());

        // Assert
        result.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetBggConfigStatus Tests

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnEnvBggStatus_WhenApiKeyEnvVariableIsSet()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(new Dictionary<string, string>());
        _environmentProviderMock
            .Setup(x => x.StatisticsEnabled)
            .Returns(true);

        // Act
        var result = await WithBggEnvApiKey("env-api-key", () => _settingsService.GetSettingsAsync());

        // Assert
        result.BggStatus.Should().NotBeNull();
        result.BggStatus.IsConfigured.Should().BeTrue();
        result.BggStatus.Source.Should().Be("env");
        result.BggStatus.IsReadOnly.Should().BeTrue();

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _environmentProviderMock.Verify(x => x.StatisticsEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnDbBggStatus_WhenApiKeyIsStoredInDatabase()
    {
        // Arrange
        var configs = new Dictionary<string, string>
        {
            { Constants.BggConfig.ApiKey, "db-api-key" }
        };
        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(configs);
        _environmentProviderMock
            .Setup(x => x.StatisticsEnabled)
            .Returns(true);

        // Act
        var result = await WithBggEnvApiKey(null, () => _settingsService.GetSettingsAsync());

        // Assert
        result.BggStatus.Should().NotBeNull();
        result.BggStatus.IsConfigured.Should().BeTrue();
        result.BggStatus.Source.Should().Be("db");
        result.BggStatus.IsReadOnly.Should().BeFalse();

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _environmentProviderMock.Verify(x => x.StatisticsEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnNoneBggStatus_WhenApiKeyIsNotConfigured()
    {
        // Arrange
        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(new Dictionary<string, string>());
        _environmentProviderMock
            .Setup(x => x.StatisticsEnabled)
            .Returns(true);

        // Act
        var result = await WithBggEnvApiKey(null, () => _settingsService.GetSettingsAsync());

        // Assert
        result.BggStatus.Should().NotBeNull();
        result.BggStatus.IsConfigured.Should().BeFalse();
        result.BggStatus.Source.Should().Be("none");
        result.BggStatus.IsReadOnly.Should().BeFalse();

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        _environmentProviderMock.Verify(x => x.StatisticsEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
