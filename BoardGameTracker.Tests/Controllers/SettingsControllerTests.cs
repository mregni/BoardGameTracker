using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models.Updates;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Languages.Interfaces;
using BoardGameTracker.Core.Updates.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Serilog.Events;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class SettingsControllerTests
{
    private readonly Mock<IConfigFileProvider> _configFileProviderMock;
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
    private readonly Mock<ILanguageService> _languageServiceMock;
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly SettingsController _controller;

    public SettingsControllerTests()
    {
        _configFileProviderMock = new Mock<IConfigFileProvider>();
        _environmentProviderMock = new Mock<IEnvironmentProvider>();
        _languageServiceMock = new Mock<ILanguageService>();
        _updateServiceMock = new Mock<IUpdateService>();

        _controller = new SettingsController(
            _configFileProviderMock.Object,
            _environmentProviderMock.Object,
            _languageServiceMock.Object,
            _updateServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _configFileProviderMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
        _languageServiceMock.VerifyNoOtherCalls();
        _updateServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ShouldReturnSettings_WhenCalled()
    {
        // Arrange
        var updateSettings = new UpdateSettings
        {
            Enabled = true,
            IntervalHours = 24
        };

        _updateServiceMock
            .Setup(x => x.GetUpdateSettingsAsync())
            .ReturnsAsync(updateSettings);

        _configFileProviderMock.SetupGet(x => x.TimeFormat).Returns("HH:mm");
        _configFileProviderMock.SetupGet(x => x.DateFormat).Returns("yyyy-MM-dd");
        _configFileProviderMock.SetupGet(x => x.UILanguage).Returns("en-US");
        _configFileProviderMock.SetupGet(x => x.Currency).Returns("USD");
        _environmentProviderMock.SetupGet(x => x.EnableStatistics).Returns(true);

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var settings = okResult.Value.Should().BeAssignableTo<UIResourceDto>().Subject;

        settings.TimeFormat.Should().Be("HH:mm");
        settings.DateFormat.Should().Be("yyyy-MM-dd");
        settings.UILanguage.Should().Be("en-US");
        settings.Currency.Should().Be("USD");
        settings.Statistics.Should().BeTrue();
        settings.UpdateCheckEnabled.Should().BeTrue();
        settings.UpdateCheckIntervalHours.Should().Be(24);

        _updateServiceMock.Verify(x => x.GetUpdateSettingsAsync(), Times.Once);
        _configFileProviderMock.VerifyGet(x => x.TimeFormat, Times.Once);
        _configFileProviderMock.VerifyGet(x => x.DateFormat, Times.Once);
        _configFileProviderMock.VerifyGet(x => x.UILanguage, Times.Once);
        _configFileProviderMock.VerifyGet(x => x.Currency, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.EnableStatistics, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ShouldReturnSettings_WhenUpdateCheckIsDisabled()
    {
        // Arrange
        var updateSettings = new UpdateSettings
        {
            Enabled = false,
            IntervalHours = 0
        };

        _updateServiceMock
            .Setup(x => x.GetUpdateSettingsAsync())
            .ReturnsAsync(updateSettings);

        _configFileProviderMock.SetupGet(x => x.TimeFormat).Returns("hh:mm tt");
        _configFileProviderMock.SetupGet(x => x.DateFormat).Returns("MM/dd/yyyy");
        _configFileProviderMock.SetupGet(x => x.UILanguage).Returns("fr-FR");
        _configFileProviderMock.SetupGet(x => x.Currency).Returns("EUR");
        _environmentProviderMock.SetupGet(x => x.EnableStatistics).Returns(false);

        // Act
        var result = await _controller.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var settings = okResult.Value.Should().BeAssignableTo<UIResourceDto>().Subject;

        settings.TimeFormat.Should().Be("hh:mm tt");
        settings.DateFormat.Should().Be("MM/dd/yyyy");
        settings.UILanguage.Should().Be("fr-FR");
        settings.Currency.Should().Be("EUR");
        settings.Statistics.Should().BeFalse();
        settings.UpdateCheckEnabled.Should().BeFalse();
        settings.UpdateCheckIntervalHours.Should().Be(0);

        _updateServiceMock.Verify(x => x.GetUpdateSettingsAsync(), Times.Once);
        _configFileProviderMock.VerifyGet(x => x.TimeFormat, Times.Once);
        _configFileProviderMock.VerifyGet(x => x.DateFormat, Times.Once);
        _configFileProviderMock.VerifyGet(x => x.UILanguage, Times.Once);
        _configFileProviderMock.VerifyGet(x => x.Currency, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.EnableStatistics, Times.Once);
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
            UILanguage = "de-DE",
            Currency = "GBP",
            UpdateCheckEnabled = true,
            UpdateCheckIntervalHours = 48
        };

        _configFileProviderMock.SetupSet(x => x.Currency = model.Currency).Verifiable();
        _configFileProviderMock.SetupSet(x => x.TimeFormat = model.TimeFormat).Verifiable();
        _configFileProviderMock.SetupSet(x => x.DateFormat = model.DateFormat).Verifiable();
        _configFileProviderMock.SetupSet(x => x.UILanguage = model.UILanguage).Verifiable();

        _updateServiceMock
            .Setup(x => x.UpdateSettingsAsync(model.UpdateCheckEnabled, model.UpdateCheckIntervalHours))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(model);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedModel = okResult.Value.Should().BeAssignableTo<UIResourceDto>().Subject;

        returnedModel.Should().BeSameAs(model);

        _configFileProviderMock.VerifySet(x => x.Currency = model.Currency, Times.Once);
        _configFileProviderMock.VerifySet(x => x.TimeFormat = model.TimeFormat, Times.Once);
        _configFileProviderMock.VerifySet(x => x.DateFormat = model.DateFormat, Times.Once);
        _configFileProviderMock.VerifySet(x => x.UILanguage = model.UILanguage, Times.Once);
        _updateServiceMock.Verify(x => x.UpdateSettingsAsync(model.UpdateCheckEnabled, model.UpdateCheckIntervalHours), Times.Once);
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
            UILanguage = "en-US",
            Currency = "USD",
            UpdateCheckEnabled = false,
            UpdateCheckIntervalHours = 0
        };

        _configFileProviderMock.SetupSet(x => x.Currency = model.Currency).Verifiable();
        _configFileProviderMock.SetupSet(x => x.TimeFormat = model.TimeFormat).Verifiable();
        _configFileProviderMock.SetupSet(x => x.DateFormat = model.DateFormat).Verifiable();
        _configFileProviderMock.SetupSet(x => x.UILanguage = model.UILanguage).Verifiable();

        _updateServiceMock
            .Setup(x => x.UpdateSettingsAsync(model.UpdateCheckEnabled, model.UpdateCheckIntervalHours))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Update(model);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedModel = okResult.Value.Should().BeAssignableTo<UIResourceDto>().Subject;

        returnedModel.Should().BeSameAs(model);

        _configFileProviderMock.VerifySet(x => x.Currency = model.Currency, Times.Once);
        _configFileProviderMock.VerifySet(x => x.TimeFormat = model.TimeFormat, Times.Once);
        _configFileProviderMock.VerifySet(x => x.DateFormat = model.DateFormat, Times.Once);
        _configFileProviderMock.VerifySet(x => x.UILanguage = model.UILanguage, Times.Once);
        _updateServiceMock.Verify(x => x.UpdateSettingsAsync(model.UpdateCheckEnabled, model.UpdateCheckIntervalHours), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void GetEnvironment_ShouldReturnEnvironmentInfo_WhenCalled()
    {
        // Arrange
        _environmentProviderMock.SetupGet(x => x.EnableStatistics).Returns(true);
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

        _environmentProviderMock.VerifyGet(x => x.EnableStatistics, Times.Once);
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
        _environmentProviderMock.SetupGet(x => x.EnableStatistics).Returns(false);
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

        _environmentProviderMock.VerifyGet(x => x.EnableStatistics, Times.Once);
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
            .ReturnsAsync(new List<Language>());

        // Act
        var result = await _controller.GetLanguages();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLanguages = okResult.Value.Should().BeAssignableTo<List<Language>>().Subject;

        returnedLanguages.Should().BeEmpty();

        _languageServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }
}
