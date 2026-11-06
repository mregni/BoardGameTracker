using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Leaderboard.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Leaderboard;

public class LeaderboardControllerTests
{
    private readonly Mock<ILeaderboardService> _leaderboardServiceMock = new();
    private readonly LeaderboardController _controller;

    public LeaderboardControllerTests()
    {
        _controller = new LeaderboardController(_leaderboardServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _leaderboardServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboard_ShouldPassTheFiltersToTheService_AndReturnOk()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 12, 31);
        var expectedFilter = new LeaderboardFilter(from, to, 4, 3, LeaderboardRankBy.WinRate);
        var leaderboard = new LeaderboardDto { MinimumSessions = 3, RankBy = LeaderboardRankBy.WinRate };
        _leaderboardServiceMock
            .Setup(x => x.GetLeaderboardAsync(expectedFilter, TestContext.Current.CancellationToken))
            .ReturnsAsync(leaderboard);

        var result = await _controller.GetLeaderboard(from, to, 4, 3, LeaderboardRankBy.WinRate, TestContext.Current.CancellationToken);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(leaderboard);
        _leaderboardServiceMock.Verify(x => x.GetLeaderboardAsync(expectedFilter, TestContext.Current.CancellationToken), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboard_ShouldUseTheDefaults_WhenNoFilterIsGiven()
    {
        var leaderboard = new LeaderboardDto();
        _leaderboardServiceMock
            .Setup(x => x.GetLeaderboardAsync(new LeaderboardFilter(), TestContext.Current.CancellationToken))
            .ReturnsAsync(leaderboard);

        var result = await _controller.GetLeaderboard(null, null, null, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(leaderboard);
        _leaderboardServiceMock.Verify(x => x.GetLeaderboardAsync(new LeaderboardFilter(), TestContext.Current.CancellationToken), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnBadRequest_WhenTheStartIsAfterTheEnd()
    {
        var result = await _controller.GetLeaderboard(new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1), null, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>()
            .Which.Should().Match<ProblemDetails>(x => x.Status == 400 && x.Title == "The start date must be on or before the end date.");
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetLeaderboard_ShouldAcceptTheRange_WhenTheDatesAreEqualOrOneIsOpen(bool withFrom, bool withTo)
    {
        DateOnly? from = withFrom ? new DateOnly(2026, 5, 1) : null;
        DateOnly? to = withTo ? new DateOnly(2026, 5, 1) : null;
        var leaderboard = new LeaderboardDto();
        var expectedFilter = new LeaderboardFilter(from, to);
        _leaderboardServiceMock
            .Setup(x => x.GetLeaderboardAsync(expectedFilter, TestContext.Current.CancellationToken))
            .ReturnsAsync(leaderboard);

        var result = await _controller.GetLeaderboard(from, to, null, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(leaderboard);
        _leaderboardServiceMock.Verify(x => x.GetLeaderboardAsync(expectedFilter, TestContext.Current.CancellationToken), Times.Once);
        VerifyNoOtherCalls();
    }
}
