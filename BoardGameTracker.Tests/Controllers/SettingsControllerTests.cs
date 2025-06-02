using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Language;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Languages.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;
public class SettingsControllerTests
    {
        private readonly Mock<IConfigFileProvider> _configFileProviderMock;
        private readonly Mock<IEnvironmentProvider> _environmentProviderMock;
        private readonly Mock<ILanguageService> _languageServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly SettingsController _controller;

        public SettingsControllerTests()
        {
            _configFileProviderMock = new Mock<IConfigFileProvider>();
            _environmentProviderMock = new Mock<IEnvironmentProvider>();
            _languageServiceMock = new Mock<ILanguageService>();
            _mapperMock = new Mock<IMapper>();
            
            _controller = new SettingsController(
                _configFileProviderMock.Object,
                _environmentProviderMock.Object,
                _languageServiceMock.Object,
                _mapperMock.Object);
        }

        [Fact]
        public void Get_ShouldReturnOkResultWithUIResourceViewModel_WhenCalled()
        {
            const string expectedTimeFormat = "HH:mm";
            const string expectedDateFormat = "yyyy-MM-dd";
            const string expectedUILanguage = "en-US";
            const string expectedCurrency = "USD";
            const string expectedDecimalSeparator = ".";
            const bool expectedStatistics = true;

            _configFileProviderMock.Setup(x => x.TimeFormat).Returns(expectedTimeFormat);
            _configFileProviderMock.Setup(x => x.DateFormat).Returns(expectedDateFormat);
            _configFileProviderMock.Setup(x => x.UILanguage).Returns(expectedUILanguage);
            _configFileProviderMock.Setup(x => x.Currency).Returns(expectedCurrency);
            _configFileProviderMock.Setup(x => x.DecimalSeparator).Returns(expectedDecimalSeparator);
            _environmentProviderMock.Setup(x => x.EnableStatistics).Returns(expectedStatistics);

            var result = _controller.Get();

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<UIResourceViewModel>();
            
            var uiResources = okResult.Value as UIResourceViewModel;
            uiResources!.TimeFormat.Should().Be(expectedTimeFormat);
            uiResources.DateFormat.Should().Be(expectedDateFormat);
            uiResources.UILanguage.Should().Be(expectedUILanguage);
            uiResources.Currency.Should().Be(expectedCurrency);
            uiResources.DecimalSeparator.Should().Be(expectedDecimalSeparator);
            uiResources.Statistics.Should().Be(expectedStatistics);

            _configFileProviderMock.Verify(x => x.TimeFormat, Times.Once);
            _configFileProviderMock.Verify(x => x.DateFormat, Times.Once);
            _configFileProviderMock.Verify(x => x.UILanguage, Times.Once);
            _configFileProviderMock.Verify(x => x.Currency, Times.Once);
            _configFileProviderMock.Verify(x => x.DecimalSeparator, Times.Once);
            _environmentProviderMock.Verify(x => x.EnableStatistics, Times.Once);
            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyNoOtherCalls();
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("HH:mm:ss", "dd/MM/yyyy", "fr-FR", "EUR", ",", false)]
        [InlineData("h:mm tt", "MM/dd/yyyy", "en-US", "USD", ".", true)]
        [InlineData("HH:mm", "yyyy-MM-dd", "de-DE", "GBP", ".", false)]
        public void Get_ShouldReturnCorrectValues_WithDifferentConfigurations(
            string timeFormat, string dateFormat, string uiLanguage, 
            string currency, string decimalSeparator, bool enableStatistics)
        {
            _configFileProviderMock.Setup(x => x.TimeFormat).Returns(timeFormat);
            _configFileProviderMock.Setup(x => x.DateFormat).Returns(dateFormat);
            _configFileProviderMock.Setup(x => x.UILanguage).Returns(uiLanguage);
            _configFileProviderMock.Setup(x => x.Currency).Returns(currency);
            _configFileProviderMock.Setup(x => x.DecimalSeparator).Returns(decimalSeparator);
            _environmentProviderMock.Setup(x => x.EnableStatistics).Returns(enableStatistics);

            var result = _controller.Get();

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var uiResources = okResult!.Value as UIResourceViewModel;
            
            uiResources!.TimeFormat.Should().Be(timeFormat);
            uiResources.DateFormat.Should().Be(dateFormat);
            uiResources.UILanguage.Should().Be(uiLanguage);
            uiResources.Currency.Should().Be(currency);
            uiResources.DecimalSeparator.Should().Be(decimalSeparator);
            uiResources.Statistics.Should().Be(enableStatistics);

            _configFileProviderMock.VerifyGet(x => x.Currency, Times.Once);
            _configFileProviderMock.VerifyGet(x => x.DecimalSeparator, Times.Once);
            _configFileProviderMock.VerifyGet(x => x.TimeFormat, Times.Once);
            _configFileProviderMock.VerifyGet(x => x.DateFormat, Times.Once);
            _configFileProviderMock.VerifyGet(x => x.UILanguage, Times.Once);
            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyGet(x => x.EnableStatistics, Times.Once);
            _environmentProviderMock.VerifyNoOtherCalls();
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void Update_ShouldReturnOkResultWithModel_WhenValidModelProvided()
        {
            var model = new UIResourceViewModel
            {
                TimeFormat = "HH:mm:ss",
                DateFormat = "dd/MM/yyyy",
                UILanguage = "fr-FR",
                Currency = "EUR",
                DecimalSeparator = ",",
                Statistics = true
            };

            _configFileProviderMock.SetupAllProperties();

            var result = _controller.Update(model);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(model);

            _configFileProviderMock.VerifySet(x => x.Currency = model.Currency, Times.Once);
            _configFileProviderMock.VerifySet(x => x.DecimalSeparator = model.DecimalSeparator, Times.Once);
            _configFileProviderMock.VerifySet(x => x.TimeFormat = model.TimeFormat, Times.Once);
            _configFileProviderMock.VerifySet(x => x.DateFormat = model.DateFormat, Times.Once);
            _configFileProviderMock.VerifySet(x => x.UILanguage = model.UILanguage, Times.Once);
            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyNoOtherCalls();
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("12:00", "yyyy/MM/dd", "ja-JP", "JPY", ".")]
        [InlineData("24:00", "dd.MM.yyyy", "ru-RU", "RUB", ",")]
        [InlineData("h:mm a", "M/d/yyyy", "es-ES", "USD", ".")]
        public void Update_ShouldSetAllProperties_WithDifferentValues(
            string timeFormat, string dateFormat, string uiLanguage, string currency, string decimalSeparator)
        {
            var model = new UIResourceViewModel
            {
                TimeFormat = timeFormat,
                DateFormat = dateFormat,
                UILanguage = uiLanguage,
                Currency = currency,
                DecimalSeparator = decimalSeparator,
                Statistics = false
            };

            _configFileProviderMock.SetupAllProperties();

            var result = _controller.Update(model);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(model);

            _configFileProviderMock.VerifySet(x => x.Currency = currency, Times.Once);
            _configFileProviderMock.VerifySet(x => x.DecimalSeparator = decimalSeparator, Times.Once);
            _configFileProviderMock.VerifySet(x => x.TimeFormat = timeFormat, Times.Once);
            _configFileProviderMock.VerifySet(x => x.DateFormat = dateFormat, Times.Once);
            _configFileProviderMock.VerifySet(x => x.UILanguage = uiLanguage, Times.Once);
            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyNoOtherCalls();
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void GetEnvironment_ShouldReturnOkResultWithUIEnvironmentViewModel_WhenCalled()
        {
            const bool expectedEnableStatistics = true;
            const LogLevel expectedLogLevel = LogLevel.Information;
            const string expectedEnvironmentName = "Production";
            const int expectedPort = 8080;

            _environmentProviderMock.Setup(x => x.EnableStatistics).Returns(expectedEnableStatistics);
            _environmentProviderMock.Setup(x => x.LogLevel).Returns(expectedLogLevel);
            _environmentProviderMock.Setup(x => x.EnvironmentName).Returns(expectedEnvironmentName);
            _environmentProviderMock.Setup(x => x.Port).Returns(expectedPort);

            var result = _controller.GetEnvironment();

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<UIEnvironmentViewModel>();
            
            var environmentResources = okResult.Value as UIEnvironmentViewModel;
            environmentResources!.EnableStatistics.Should().Be(expectedEnableStatistics);
            environmentResources.LogLevel.Should().Be(expectedLogLevel);
            environmentResources.EnvironmentName.Should().Be(expectedEnvironmentName);
            environmentResources.Port.Should().Be(expectedPort);

            _environmentProviderMock.Verify(x => x.EnableStatistics, Times.Once);
            _environmentProviderMock.Verify(x => x.LogLevel, Times.Once);
            _environmentProviderMock.Verify(x => x.EnvironmentName, Times.Once);
            _environmentProviderMock.Verify(x => x.Port, Times.Once);
            _environmentProviderMock.VerifyNoOtherCalls();
            _configFileProviderMock.VerifyNoOtherCalls();
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(LogLevel.Debug, "Development", 3000, false)]
        [InlineData(LogLevel.Information, "Staging", 5000, true)]
        [InlineData(LogLevel.Warning, "Production", 8080, true)]
        [InlineData(LogLevel.Error, "Testing", 9000, false)]
        public void GetEnvironment_ShouldReturnCorrectValues_WithDifferentConfigurations(
            LogLevel logLevel, string environmentName, int port, bool enableStatistics)
        {
            _environmentProviderMock.Setup(x => x.EnableStatistics).Returns(enableStatistics);
            _environmentProviderMock.Setup(x => x.LogLevel).Returns(logLevel);
            _environmentProviderMock.Setup(x => x.EnvironmentName).Returns(environmentName);
            _environmentProviderMock.Setup(x => x.Port).Returns(port);

            var result = _controller.GetEnvironment();

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var environmentResources = okResult!.Value as UIEnvironmentViewModel;
            
            environmentResources!.EnableStatistics.Should().Be(enableStatistics);
            environmentResources.LogLevel.Should().Be(logLevel);
            environmentResources.EnvironmentName.Should().Be(environmentName);
            environmentResources.Port.Should().Be(port);

            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyGet(x => x.EnableStatistics, Times.Once);
            _environmentProviderMock.VerifyGet(x => x.LogLevel, Times.Once);
            _environmentProviderMock.VerifyGet(x => x.EnvironmentName, Times.Once);
            _environmentProviderMock.VerifyGet(x => x.Port, Times.Once);
            _environmentProviderMock.VerifyNoOtherCalls();
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetLanguages_ShouldReturnOkResultWithMappedLanguages_WhenLanguagesExist()
        {
            var languages = new List<Language>
            {
                new() { Id = 1, Key = "en", TranslationKey = "English" },
                new() { Id = 2, Key = "fr", TranslationKey = "French" },
                new() { Id = 3, Key = "de", TranslationKey = "German" }
            };

            var mappedLanguages = new List<LanguageViewModel>
            {
                new() { Id = 1, Key = "en", TranslationKey = "English" },
                new() { Id = 2, Key = "fr", TranslationKey = "French" },
                new() { Id = 3, Key = "de", TranslationKey = "German" }
            };

            _languageServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(languages);
            _mapperMock.Setup(x => x.Map<IList<LanguageViewModel>>(languages)).Returns(mappedLanguages);

            var result = await _controller.GetLanguages();

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(mappedLanguages);

            _languageServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
            _mapperMock.Verify(x => x.Map<IList<LanguageViewModel>>(languages), Times.Once);
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetLanguages_ShouldReturnOkResultWithEmptyList_WhenNoLanguagesExist()
        {
            var languages = new List<Language>();
            var mappedLanguages = new List<LanguageViewModel>();

            _languageServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(languages);
            _mapperMock.Setup(x => x.Map<IList<LanguageViewModel>>(languages)).Returns(mappedLanguages);

            var result = await _controller.GetLanguages();

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(mappedLanguages);

            _languageServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
            _mapperMock.Verify(x => x.Map<IList<LanguageViewModel>>(languages), Times.Once);
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetLanguages_ShouldThrowException_WhenLanguageServiceThrows()
        {
            var expectedException = new InvalidOperationException("Language service error");

            _languageServiceMock.Setup(x => x.GetAllAsync()).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.GetLanguages());
            
            exception.Should().Be(expectedException);

            _languageServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void Update_ShouldNotUpdateStatisticsProperty_WhenModelContainsStatistics()
        {
            var model = new UIResourceViewModel
            {
                TimeFormat = "HH:mm",
                DateFormat = "yyyy-MM-dd",
                UILanguage = "en-US",
                Currency = "USD",
                DecimalSeparator = ".",
                Statistics = true
            };

            _configFileProviderMock.SetupAllProperties();

            var result = _controller.Update(model);

            result.Should().BeOfType<OkObjectResult>();

            _configFileProviderMock.VerifySet(x => x.Currency = model.Currency, Times.Once);
            _configFileProviderMock.VerifySet(x => x.DecimalSeparator = model.DecimalSeparator, Times.Once);
            _configFileProviderMock.VerifySet(x => x.TimeFormat = model.TimeFormat, Times.Once);
            _configFileProviderMock.VerifySet(x => x.DateFormat = model.DateFormat, Times.Once);
            _configFileProviderMock.VerifySet(x => x.UILanguage = model.UILanguage, Times.Once);
            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyNoOtherCalls();
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void Get_ShouldReadPropertiesInCorrectOrder_WhenCalled()
        {
            var sequence = new MockSequence();
            _configFileProviderMock.InSequence(sequence).Setup(x => x.TimeFormat).Returns("HH:mm");
            _configFileProviderMock.InSequence(sequence).Setup(x => x.DateFormat).Returns("yyyy-MM-dd");
            _configFileProviderMock.InSequence(sequence).Setup(x => x.UILanguage).Returns("en-US");
            _configFileProviderMock.InSequence(sequence).Setup(x => x.Currency).Returns("USD");
            _configFileProviderMock.InSequence(sequence).Setup(x => x.DecimalSeparator).Returns(".");
            _environmentProviderMock.InSequence(sequence).Setup(x => x.EnableStatistics).Returns(true);

            var result = _controller.Get();

            result.Should().BeOfType<OkObjectResult>();
            
            _configFileProviderMock.VerifyGet(x => x.Currency, Times.Once);
            _configFileProviderMock.VerifyGet(x => x.DecimalSeparator, Times.Once);
            _configFileProviderMock.VerifyGet(x => x.TimeFormat, Times.Once);
            _configFileProviderMock.VerifyGet(x => x.DateFormat, Times.Once);
            _configFileProviderMock.VerifyGet(x => x.UILanguage, Times.Once);
            _configFileProviderMock.VerifyNoOtherCalls();
            _environmentProviderMock.VerifyGet(x => x.EnableStatistics);
            _environmentProviderMock.VerifyNoOtherCalls();
            _languageServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
        }
    }