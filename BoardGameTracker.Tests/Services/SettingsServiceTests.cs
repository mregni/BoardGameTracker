using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Updates;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Settings;
using BoardGameTracker.Core.Updates.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class SettingsServiceTests
{
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        _configRepositoryMock = new Mock<IConfigRepository>();
        _updateServiceMock = new Mock<IUpdateService>();
        _environmentProviderMock = new Mock<IEnvironmentProvider>();

        _settingsService = new SettingsService(
            _configRepositoryMock.Object,
            _updateServiceMock.Object,
            _environmentProviderMock.Object);
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
        var updateSettings = new UpdateSettings
        {
            Enabled = true,
            VersionTrack = VersionTrack.Stable
        };

        _updateServiceMock
            .Setup(x => x.GetUpdateSettingsAsync())
            .ReturnsAsync(updateSettings);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.TimeFormat))
            .ReturnsAsync("HH:mm");

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.DateFormat))
            .ReturnsAsync("yyyy-MM-dd");

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.UiLanguage))
            .ReturnsAsync("en-US");

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.Currency))
            .ReturnsAsync("USD");

        _environmentProviderMock
            .Setup(x => x.EnableStatistics)
            .Returns(true);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled))
            .ReturnsAsync(true);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(6);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.GameNightsEnabled))
            .ReturnsAsync(true);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.PublicUrl))
            .ReturnsAsync("https://example.com");

        var result = await _settingsService.GetSettingsAsync();

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

        _updateServiceMock.Verify(x => x.GetUpdateSettingsAsync(), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.TimeFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.DateFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.UiLanguage), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.Currency), Times.Once);
        _environmentProviderMock.Verify(x => x.EnableStatistics, Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.GameNightsEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.PublicUrl), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnSettings_WhenUpdateCheckIsDisabled()
    {
        var updateSettings = new UpdateSettings
        {
            Enabled = false,
            VersionTrack = VersionTrack.Beta
        };

        _updateServiceMock
            .Setup(x => x.GetUpdateSettingsAsync())
            .ReturnsAsync(updateSettings);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.TimeFormat))
            .ReturnsAsync("hh:mm a");

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.DateFormat))
            .ReturnsAsync("MM/dd/yyyy");

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.UiLanguage))
            .ReturnsAsync("de-DE");

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.Currency))
            .ReturnsAsync("EUR");

        _environmentProviderMock
            .Setup(x => x.EnableStatistics)
            .Returns(false);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled))
            .ReturnsAsync(false);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths))
            .ReturnsAsync(12);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.GameNightsEnabled))
            .ReturnsAsync(false);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueAsync<string>(Constants.AppConfig.PublicUrl))
            .ReturnsAsync("https://test.com");

        var result = await _settingsService.GetSettingsAsync();

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

        _updateServiceMock.Verify(x => x.GetUpdateSettingsAsync(), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.TimeFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.DateFormat), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.UiLanguage), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.Currency), Times.Once);
        _environmentProviderMock.Verify(x => x.EnableStatistics, Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.ShelfOfShameEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<int>(Constants.AppConfig.ShelfOfShameMonths), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<bool>(Constants.AppConfig.GameNightsEnabled), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueAsync<string>(Constants.AppConfig.PublicUrl), Times.Once);
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
