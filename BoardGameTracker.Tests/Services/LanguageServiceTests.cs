using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Languages;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class LanguageServiceTests
{
    private readonly Mock<IRepository<Language>> _languageRepositoryMock;
    private readonly Mock<ILogger<LanguageService>> _loggerMock;
    private readonly LanguageService _languageService;

    public LanguageServiceTests()
    {
        _languageRepositoryMock = new Mock<IRepository<Language>>();
        _loggerMock = new Mock<ILogger<LanguageService>>();
        _languageService = new LanguageService(_languageRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnLanguageList_WhenRepositoryReturnsData()
    {
        var expectedLanguages = new List<Language>
        {
            new() {Id = 1, Key = "en", TranslationKey = "English"},
            new() {Id = 2, Key = "fr", TranslationKey = "French"},
            new() {Id = 3, Key = "de", TranslationKey = "German"},
            new() {Id = 4, Key = "es", TranslationKey = "Spanish"}
        };

        _languageRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedLanguages);

        var result = await _languageService.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Should().BeEquivalentTo(expectedLanguages);
        _languageRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _languageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllAsync_ShouldThrowException_WhenRepositoryThrows()
    {
        var expectedException = new InvalidOperationException("Database connection failed");

        _languageRepositoryMock.Setup(x => x.GetAllAsync()).ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _languageService.GetAllAsync());

        exception.Should().Be(expectedException);
        _languageRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _languageRepositoryMock.VerifyNoOtherCalls();
    }

}
