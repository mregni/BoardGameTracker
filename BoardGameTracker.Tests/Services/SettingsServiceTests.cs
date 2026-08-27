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
        _configRepositoryMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    private void VerifyEnvironmentReads()
    {
        _environmentProviderMock.VerifyGet(x => x.StatisticsEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.EmailEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.RagEnabled, Times.Once);
    }

    private static async Task<T> WithEnvVar<T>(string name, string? value, Func<Task<T>> action)
    {
        var original = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        try
        {
            return await action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, original);
        }
    }

    #region GetSettingsAsync Tests

    [Theory]
    [InlineData("HH:mm", "yyyy-MM-dd", "en-US", "USD", true, true, 6, true, "https://example.com", true, true, "stable", VersionTrack.Stable)]
    [InlineData("hh:mm a", "MM/dd/yyyy", "de-DE", "EUR", false, false, 12, false, "https://test.com", false, false, "beta", VersionTrack.Beta)]
    public async Task GetSettingsAsync_ShouldMapAllConfigValues(
        string timeFormat,
        string dateFormat,
        string uiLanguage,
        string currency,
        bool statistics,
        bool shelfOfShameEnabled,
        int shelfOfShameMonths,
        bool gameNightsEnabled,
        string publicUrl,
        bool rsvpAuthenticationEnabled,
        bool updateCheckEnabled,
        string trackValue,
        VersionTrack expectedTrack)
    {
        var configs = new Dictionary<string, string>
        {
            { Constants.AppConfig.TimeFormat, timeFormat },
            { Constants.AppConfig.DateFormat, dateFormat },
            { Constants.AppConfig.UiLanguage, uiLanguage },
            { Constants.AppConfig.Currency, currency },
            { Constants.AppConfig.ShelfOfShameEnabled, shelfOfShameEnabled.ToString() },
            { Constants.AppConfig.ShelfOfShameMonths, shelfOfShameMonths.ToString() },
            { Constants.AppConfig.GameNightsEnabled, gameNightsEnabled.ToString() },
            { Constants.AppConfig.PublicUrl, publicUrl },
            { Constants.AppConfig.RsvpAuthenticationEnabled, rsvpAuthenticationEnabled.ToString() },
            { Constants.UpdateConfig.CheckEnabled, updateCheckEnabled.ToString() },
            { Constants.UpdateConfig.Track, trackValue }
        };

        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(configs);

        _environmentProviderMock.Setup(x => x.StatisticsEnabled).Returns(statistics);
        _environmentProviderMock.Setup(x => x.EmailEnabled).Returns(true);
        _environmentProviderMock.Setup(x => x.RagEnabled).Returns(false);

        var result = await _settingsService.GetSettingsAsync();

        result.Should().NotBeNull();
        result.TimeFormat.Should().Be(timeFormat);
        result.DateFormat.Should().Be(dateFormat);
        result.UiLanguage.Should().Be(uiLanguage);
        result.Currency.Should().Be(currency);
        result.Statistics.Should().Be(statistics);
        result.UpdateCheckEnabled.Should().Be(updateCheckEnabled);
        result.VersionTrack.Should().Be(expectedTrack);
        result.ShelfOfShameEnabled.Should().Be(shelfOfShameEnabled);
        result.ShelfOfShameMonthsLimit.Should().Be(shelfOfShameMonths);
        result.GameNightsEnabled.Should().Be(gameNightsEnabled);
        result.PublicUrl.Should().Be(publicUrl);
        result.RsvpAuthenticationEnabled.Should().Be(rsvpAuthenticationEnabled);
        result.EmailEnabled.Should().BeTrue();
        result.RagEnabled.Should().BeFalse();
        result.BggApiKey.Should().BeEmpty();

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        VerifyEnvironmentReads();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldPreferEnvironmentValue_WhenEnvVariableSetForConfigKey()
    {
        var configs = new Dictionary<string, string>
        {
            { Constants.AppConfig.TimeFormat, "db-time-format" }
        };

        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(configs);

        var result = await WithEnvVar("TIME_FORMAT", "env-time-format", () => _settingsService.GetSettingsAsync());

        result.TimeFormat.Should().Be("env-time-format");

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        VerifyEnvironmentReads();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldFallBackToDefaults_WhenConfigValuesAreUnparseable()
    {
        var configs = new Dictionary<string, string>
        {
            { Constants.AppConfig.ShelfOfShameMonths, "not-a-number" },
            { Constants.UpdateConfig.Track, "not-a-track" }
        };

        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(configs);

        var result = await _settingsService.GetSettingsAsync();

        result.ShelfOfShameMonthsLimit.Should().Be(0);
        result.VersionTrack.Should().Be(VersionTrack.Stable);

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        VerifyEnvironmentReads();
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateSettingsAsync Tests

    [Theory]
    [InlineData("HH:mm:ss", "dd-MM-yyyy", "fr-FR", "GBP", true, VersionTrack.Stable, true, 8, true, "https://myapp.com", true, null, "")]
    [InlineData("HH:mm", "yyyy/MM/dd", "ja-JP", "JPY", false, VersionTrack.Beta, false, 3, false, "https://localhost", false, "new-key", "new-key")]
    public async Task UpdateSettingsAsync_ShouldPersistAllSettings(
        string timeFormat,
        string dateFormat,
        string uiLanguage,
        string currency,
        bool updateCheckEnabled,
        VersionTrack track,
        bool shelfOfShameEnabled,
        int shelfOfShameMonths,
        bool gameNightsEnabled,
        string publicUrl,
        bool rsvpAuthenticationEnabled,
        string? bggApiKey,
        string expectedStoredApiKey)
    {
        var model = new UIResourceDto
        {
            TimeFormat = timeFormat,
            DateFormat = dateFormat,
            UiLanguage = uiLanguage,
            Currency = currency,
            UpdateCheckEnabled = updateCheckEnabled,
            VersionTrack = track,
            ShelfOfShameEnabled = shelfOfShameEnabled,
            ShelfOfShameMonthsLimit = shelfOfShameMonths,
            GameNightsEnabled = gameNightsEnabled,
            PublicUrl = publicUrl,
            RsvpAuthenticationEnabled = rsvpAuthenticationEnabled,
            BggApiKey = bggApiKey
        };

        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await _settingsService.UpdateSettingsAsync(model);

        result.Should().NotBeNull();
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.Currency, currency), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.TimeFormat, timeFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.DateFormat, dateFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.UiLanguage, uiLanguage), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameEnabled, shelfOfShameEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.ShelfOfShameMonths, shelfOfShameMonths), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.GameNightsEnabled, gameNightsEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.PublicUrl, publicUrl), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.AppConfig.RsvpAuthenticationEnabled, rsvpAuthenticationEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.UpdateConfig.CheckEnabled, updateCheckEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.UpdateConfig.Track, track), Times.Once);
        _configRepositoryMock.Verify(x => x.SetConfigValueAsync(Constants.BggConfig.ApiKey, expectedStoredApiKey), Times.Once);
        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        VerifyEnvironmentReads();
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetBggApiKeyAsync Tests

    [Fact]
    public async Task GetBggApiKeyAsync_ShouldReturnDbValue_WhenEnvVariableNotSet()
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey))
            .ReturnsAsync("db-api-key");

        var result = await WithEnvVar(Constants.BggConfig.EnvApiKeyName, null, () => _settingsService.GetBggApiKeyAsync());

        result.Should().Be("db-api-key");
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBggApiKeyAsync_ShouldReturnTrimmedEnvValue_WhenEnvVariableSet()
    {
        var result = await WithEnvVar(Constants.BggConfig.EnvApiKeyName, "  env-api-key  ", () => _settingsService.GetBggApiKeyAsync());

        result.Should().Be("env-api-key");
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBggApiKeyAsync_ShouldReturnNull_WhenNeitherEnvNorDbValueSet()
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey))
            .ReturnsAsync((string)null!);

        var result = await WithEnvVar(Constants.BggConfig.EnvApiKeyName, null, () => _settingsService.GetBggApiKeyAsync());

        result.Should().BeNull();
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region IsBggEnabled Tests

    [Fact]
    public async Task IsBggEnabled_ShouldReturnTrue_WhenApiKeyIsConfigured()
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey))
            .ReturnsAsync("some-api-key");

        var result = await WithEnvVar(Constants.BggConfig.EnvApiKeyName, null, () => _settingsService.IsBggEnabled());

        result.Should().BeTrue();
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsBggEnabled_ShouldReturnTrue_WhenApiKeyEnvVariableIsSet()
    {
        var result = await WithEnvVar(Constants.BggConfig.EnvApiKeyName, "env-api-key", () => _settingsService.IsBggEnabled());

        result.Should().BeTrue();
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsBggEnabled_ShouldReturnFalse_WhenApiKeyIsEmpty()
    {
        var result = await WithEnvVar(Constants.BggConfig.EnvApiKeyName, null, () => _settingsService.IsBggEnabled());

        result.Should().BeFalse();
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.BggConfig.ApiKey), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetBggConfigStatus Tests

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnEnvBggStatus_WhenApiKeyEnvVariableIsSet()
    {
        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await WithEnvVar(Constants.BggConfig.EnvApiKeyName, "env-api-key", () => _settingsService.GetSettingsAsync());

        result.BggStatus.Should().NotBeNull();
        result.BggStatus.IsConfigured.Should().BeTrue();
        result.BggStatus.Source.Should().Be("env");
        result.BggStatus.IsReadOnly.Should().BeTrue();

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        VerifyEnvironmentReads();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnDbBggStatus_WhenApiKeyIsStoredInDatabase()
    {
        var configs = new Dictionary<string, string>
        {
            { Constants.BggConfig.ApiKey, "db-api-key" }
        };
        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(configs);

        var result = await WithEnvVar(Constants.BggConfig.EnvApiKeyName, null, () => _settingsService.GetSettingsAsync());

        result.BggStatus.Should().NotBeNull();
        result.BggStatus.IsConfigured.Should().BeTrue();
        result.BggStatus.Source.Should().Be("db");
        result.BggStatus.IsReadOnly.Should().BeFalse();

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        VerifyEnvironmentReads();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnNoneBggStatus_WhenApiKeyIsNotConfigured()
    {
        _configRepositoryMock
            .Setup(x => x.GetAllConfigsAsync())
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await WithEnvVar(Constants.BggConfig.EnvApiKeyName, null, () => _settingsService.GetSettingsAsync());

        result.BggStatus.Should().NotBeNull();
        result.BggStatus.IsConfigured.Should().BeFalse();
        result.BggStatus.Source.Should().Be("none");
        result.BggStatus.IsReadOnly.Should().BeFalse();

        _configRepositoryMock.Verify(x => x.GetAllConfigsAsync(), Times.Once);
        VerifyEnvironmentReads();
        VerifyNoOtherCalls();
    }

    #endregion
}
