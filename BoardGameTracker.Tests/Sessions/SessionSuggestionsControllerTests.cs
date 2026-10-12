using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Sessions;

public class SessionSuggestionsControllerTests
{
    private readonly Mock<ISessionSuggestionService> _sessionSuggestionServiceMock;
    private readonly SessionSuggestionsController _controller;

    public SessionSuggestionsControllerTests()
    {
        _sessionSuggestionServiceMock = new Mock<ISessionSuggestionService>();
        _controller = new SessionSuggestionsController(_sessionSuggestionServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _sessionSuggestionServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSuggestions_ShouldReturnOkWithSuggestions()
    {
        var suggestions = new SessionSuggestionsDto
        {
            RecentGameIds = [3, 1],
            FrequentPlayerIds = [8, 2, 5],
            LastLocationId = 4
        };

        _sessionSuggestionServiceMock
            .Setup(x => x.GetSuggestions(It.IsAny<CancellationToken>()))
            .ReturnsAsync(suggestions);

        var result = await _controller.GetSuggestions(TestContext.Current.CancellationToken);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeAssignableTo<SessionSuggestionsDto>().Subject;
        dto.RecentGameIds.Should().Equal(3, 1);
        dto.FrequentPlayerIds.Should().Equal(8, 2, 5);
        dto.LastLocationId.Should().Be(4);

        _sessionSuggestionServiceMock.Verify(x => x.GetSuggestions(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }
}
