using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Languages;
using BoardGameTracker.Core.Languages.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class LanguageServiceTests
{
    private readonly Mock<ILanguageRepository> _languageRepositoryMock;
    private readonly LanguageService _languageService;

    public LanguageServiceTests()
    {
        _languageRepositoryMock = new Mock<ILanguageRepository>();
        _languageService = new LanguageService(_languageRepositoryMock.Object);
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
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenRepositoryReturnsEmptyList()
    {
        var expectedLanguages = new List<Language>();

        _languageRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedLanguages);

        var result = await _languageService.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
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

    [Fact]
    public async Task GetAllAsync_ShouldBeConsistent_WhenCalledMultipleTimes()
    {
        var expectedLanguages = new List<Language>
        {
            new() {Id = 1, Key = "en", TranslationKey = "English"}
        };

        _languageRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedLanguages);

        var result1 = await _languageService.GetAllAsync();
        var result2 = await _languageService.GetAllAsync();
        var result3 = await _languageService.GetAllAsync();

        result1.Should().BeSameAs(expectedLanguages);
        result2.Should().BeSameAs(expectedLanguages);
        result3.Should().BeSameAs(expectedLanguages);
        _languageRepositoryMock.Verify(x => x.GetAllAsync(), Times.Exactly(3));
        _languageRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCorrectType_WhenCalled()
    {
        var expectedLanguages = new List<Language>
        {
            new() {Id = 1, Key = "en", TranslationKey = "English"}
        };

        _languageRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedLanguages);

        var result = await _languageService.GetAllAsync();

        result.Should().BeOfType<List<Language>>();
        result.Should().AllBeOfType<Language>();
        _languageRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _languageRepositoryMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(typeof(ArgumentException), "Invalid argument")]
    [InlineData(typeof(TimeoutException), "Request timeout")]
    [InlineData(typeof(UnauthorizedAccessException), "Access denied")]
    public async Task GetAllAsync_ShouldPropagateException_WhenRepositoryThrowsDifferentExceptions(Type exceptionType,
        string message)
    {
        var expectedException = (Exception) Activator.CreateInstance(exceptionType, message);

        _languageRepositoryMock.Setup(x => x.GetAllAsync()).ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync(exceptionType, () => _languageService.GetAllAsync());

        exception.Should().Be(expectedException);
        exception.Message.Should().Be(message);
        _languageRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        _languageRepositoryMock.VerifyNoOtherCalls();
    }
}
