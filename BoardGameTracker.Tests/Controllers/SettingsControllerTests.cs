using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models.Updates;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Languages.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Updates.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog.Events;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class SettingsControllerTests
{
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<ILanguageService> _languageServiceMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _settingsServiceMock = new Mock<ISettingsService>();
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _languageServiceMock = new Mock<ILanguageService>();
        _updateServiceMock = new Mock<IUpdateService>();

        _controller = new SettingsController(
            _settingsServiceMock.Object,
            _environmentProviderMock.Object,
            _languageServiceMock.Object,
            _updateServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _settingsServiceMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
        _languageServiceMock.VerifyNoOtherCalls();
        _updateServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ShouldReturnSettings_WhenCalled()
    {
        // Arrange
        var expectedDto = new UIResourceDto
        {
            TimeFormat = "HH:mm",
            DateFormat = "yyyy-MM-dd",
            UiLanguage = "en-US",
            Currency = "USD",
            Statistics = true,
            UpdateCheckEnabled = true,
            ShelfOfShameEnabled = false,
            ShelfOfShameMonthsLimit = 6,
            GameNightsEnabled = true,
            PublicUrl = "https://example.com"
        };

        _settingsServiceMock
            .Setup(x => x.GetSettingsAsync())
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var settings = okResult.Value.Should().BeAssignableTo<UIResourceDto>().Subject;

        settings.Should().BeSameAs(expectedDto);

        _settingsServiceMock.Verify(x => x.GetSettingsAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldUpdateSettings_WhenCalled()
    {
        // Arrange
        var model = new UIResourceDto
        {
            TimeFormat = "HH:mm:ss",
            DateFormat = "dd-MM-yyyy",
            UiLanguage = "de-DE",
            Currency = "GBP",
            UpdateCheckEnabled = true,
            GameNightsEnabled = true,
            PublicUrl = "https://example.com"
        };

        _settingsServiceMock
            .Setup(x => x.UpdateSettingsAsync(model))
            .ReturnsAsync(model);

        // Act
        var result = await _controller.Update(model);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedModel = okResult.Value.Should().BeAssignableTo<UIResourceDto>().Subject;

        returnedModel.Should().BeSameAs(model);

        _settingsServiceMock.Verify(x => x.UpdateSettingsAsync(model), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldUpdateSettings_WhenDisablingUpdateCheck()
    {
        // Arrange
        var model = new UIResourceDto
        {
            TimeFormat = "HH:mm",
            DateFormat = "yyyy-MM-dd",
            UiLanguage = "en-US",
            Currency = "USD",
            UpdateCheckEnabled = false
        };

        _settingsServiceMock
            .Setup(x => x.UpdateSettingsAsync(model))
            .ReturnsAsync(model);

        // Act
        var result = await _controller.Update(model);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedModel = okResult.Value.Should().BeAssignableTo<UIResourceDto>().Subject;

        returnedModel.Should().BeSameAs(model);

        _settingsServiceMock.Verify(x => x.UpdateSettingsAsync(model), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void GetEnvironment_ShouldReturnEnvironmentInfo_WhenCalled()
    {
        // Arrange
        _environmentProviderMock.SetupGet(x => x.StatisticsEnabled).Returns(true);
        _environmentProviderMock.SetupGet(x => x.LogLevel).Returns(LogEventLevel.Information);
        _environmentProviderMock.SetupGet(x => x.EnvironmentName).Returns("Production");
        _environmentProviderMock.SetupGet(x => x.Port).Returns(5000);

        _updateServiceMock
            .Setup(x => x.GetCurrentVersion())
            .Returns("1.2.3");

        // Act
        var result = _controller.GetEnvironment();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var environment = okResult.Value.Should().BeAssignableTo<UIEnvironmentDto>().Subject;

        environment.EnableStatistics.Should().BeTrue();
        environment.LogLevel.Should().Be(LogEventLevel.Information);
        environment.EnvironmentName.Should().Be("Production");
        environment.Port.Should().Be(5000);
        environment.Version.Should().Be("1.2.3");

        _environmentProviderMock.VerifyGet(x => x.StatisticsEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.LogLevel, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.EnvironmentName, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.Port, Times.Once);
        _updateServiceMock.Verify(x => x.GetCurrentVersion(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void GetEnvironment_ShouldReturnEnvironmentInfo_WithDifferentLogLevel()
    {
        // Arrange
        _environmentProviderMock.SetupGet(x => x.StatisticsEnabled).Returns(false);
        _environmentProviderMock.SetupGet(x => x.LogLevel).Returns(LogEventLevel.Debug);
        _environmentProviderMock.SetupGet(x => x.EnvironmentName).Returns("Development");
        _environmentProviderMock.SetupGet(x => x.Port).Returns(3000);

        _updateServiceMock
            .Setup(x => x.GetCurrentVersion())
            .Returns("2.0.0-beta");

        // Act
        var result = _controller.GetEnvironment();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var environment = okResult.Value.Should().BeAssignableTo<UIEnvironmentDto>().Subject;

        environment.EnableStatistics.Should().BeFalse();
        environment.LogLevel.Should().Be(LogEventLevel.Debug);
        environment.EnvironmentName.Should().Be("Development");
        environment.Port.Should().Be(3000);
        environment.Version.Should().Be("2.0.0-beta");

        _environmentProviderMock.VerifyGet(x => x.StatisticsEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.LogLevel, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.EnvironmentName, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.Port, Times.Once);
        _updateServiceMock.Verify(x => x.GetCurrentVersion(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLanguages_ShouldReturnLanguages_WhenLanguagesExist()
    {
        // Arrange
        var languages = new List<Language>
        {
            new() { Id = 1, Key = "en", TranslationKey = "English" },
            new() { Id = 2, Key = "fr", TranslationKey = "French" },
            new() { Id = 3, Key = "de", TranslationKey = "German" }
        };

        _languageServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(languages);

        // Act
        var result = await _controller.GetLanguages();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLanguages = okResult.Value.Should().BeAssignableTo<List<Language>>().Subject;

        returnedLanguages.Should().HaveCount(3);
        returnedLanguages[0].Key.Should().Be("en");
        returnedLanguages[1].Key.Should().Be("fr");
        returnedLanguages[2].Key.Should().Be("de");

        _languageServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLanguages_ShouldReturnEmptyList_WhenNoLanguagesExist()
    {
        // Arrange
        _languageServiceMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([]);

        // Act
        var result = await _controller.GetLanguages();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLanguages = okResult.Value.Should().BeAssignableTo<List<Language>>().Subject;

        returnedLanguages.Should().BeEmpty();

        _languageServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUpdateStatus_ShouldReturnStatus_WhenUpdateIsAvailable()
    {
        // Arrange
        var updateStatus = new UpdateStatus
        {
            CurrentVersion = "1.0.0",
            LatestVersion = "1.1.0",
            UpdateAvailable = true,
            LastChecked = DateTime.UtcNow.AddHours(-1),
            ErrorMessage = null
        };

        _updateServiceMock
            .Setup(x => x.GetVersionInfoAsync())
            .ReturnsAsync(updateStatus);

        // Act
        var result = await _controller.GetVersionInfo();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("1.0.0");
        statusDto.LatestVersion.Should().Be("1.1.0");
        statusDto.UpdateAvailable.Should().BeTrue();
        statusDto.LastChecked.Should().Be(updateStatus.LastChecked);
        statusDto.ErrorMessage.Should().BeNull();

        _updateServiceMock.Verify(x => x.GetVersionInfoAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUpdateStatus_ShouldReturnStatus_WhenNoUpdateIsAvailable()
    {
        // Arrange
        var updateStatus = new UpdateStatus
        {
            CurrentVersion = "2.5.0",
            LatestVersion = "2.5.0",
            UpdateAvailable = false,
            LastChecked = DateTime.UtcNow.AddMinutes(-30),
            ErrorMessage = null
        };

        _updateServiceMock
            .Setup(x => x.GetVersionInfoAsync())
            .ReturnsAsync(updateStatus);

        // Act
        var result = await _controller.GetVersionInfo();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("2.5.0");
        statusDto.LatestVersion.Should().Be("2.5.0");
        statusDto.UpdateAvailable.Should().BeFalse();
        statusDto.LastChecked.Should().Be(updateStatus.LastChecked);
        statusDto.ErrorMessage.Should().BeNull();

        _updateServiceMock.Verify(x => x.GetVersionInfoAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUpdateStatus_ShouldReturnStatus_WhenNeverChecked()
    {
        // Arrange
        var updateStatus = new UpdateStatus
        {
            CurrentVersion = "1.0.0",
            LatestVersion = null,
            UpdateAvailable = false,
            LastChecked = null,
            ErrorMessage = null
        };

        _updateServiceMock
            .Setup(x => x.GetVersionInfoAsync())
            .ReturnsAsync(updateStatus);

        // Act
        var result = await _controller.GetVersionInfo();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("1.0.0");
        statusDto.LatestVersion.Should().BeNull();
        statusDto.UpdateAvailable.Should().BeFalse();
        statusDto.LastChecked.Should().BeNull();
        statusDto.ErrorMessage.Should().BeNull();

        _updateServiceMock.Verify(x => x.GetVersionInfoAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUpdateStatus_ShouldReturnStatus_WhenErrorOccurred()
    {
        // Arrange
        var updateStatus = new UpdateStatus
        {
            CurrentVersion = "1.5.0",
            LatestVersion = null,
            UpdateAvailable = false,
            LastChecked = DateTime.UtcNow.AddDays(-1),
            ErrorMessage = "Failed to connect to update server"
        };

        _updateServiceMock
            .Setup(x => x.GetVersionInfoAsync())
            .ReturnsAsync(updateStatus);

        // Act
        var result = await _controller.GetVersionInfo();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("1.5.0");
        statusDto.LatestVersion.Should().BeNull();
        statusDto.UpdateAvailable.Should().BeFalse();
        statusDto.LastChecked.Should().Be(updateStatus.LastChecked);
        statusDto.ErrorMessage.Should().Be("Failed to connect to update server");

        _updateServiceMock.Verify(x => x.GetVersionInfoAsync(), Times.Once);
        VerifyNoOtherCalls();
    }
}
