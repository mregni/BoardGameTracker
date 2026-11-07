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

        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _sessionSuggestionServiceMock
            .Setup(x => x.GetSuggestions(token))
            .ReturnsAsync(suggestions);

        var result = await _controller.GetSuggestions(token);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeAssignableTo<SessionSuggestionsDto>().Subject;
        dto.RecentGameIds.Should().Equal(3, 1);
        dto.FrequentPlayerIds.Should().Equal(8, 2, 5);
        dto.LastLocationId.Should().Be(4);

        _sessionSuggestionServiceMock.Verify(x => x.GetSuggestions(token), Times.Once);
        VerifyNoOtherCalls();
    }
}
