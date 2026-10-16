using System.Security.Claims;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Dashboard.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Dashboard;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _controller = new DashboardController(_dashboardServiceMock.Object);
        SignIn("user-1");
    }

    private void SignIn(string? userId)
    {
        var claims = userId == null ? [] : new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    private void VerifyNoOtherCalls()
    {
        _dashboardServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboardStatistics_ShouldReturnOkWithStatistics_WhenDataExists()
    {
        var statistics = new DashboardStatisticsDto
        {
            TotalGames = 25,
            ActivePlayers = 10,
            SessionsPlayed = 100,
            TotalPlayedTime = 5000,
            TotalCollectionValue = 887.5m,
            ExpansionsOwned = 15,
            AvgSessionTime = 50,
            RecentActivities = [],
            Collection = [],
            MostPlayedGames = [],
            TopPlayers = [],
            RecentAddedGames = []
        };

        _dashboardServiceMock
            .Setup(x => x.GetStatistics("user-1", TestContext.Current.CancellationToken))
            .ReturnsAsync(statistics);

        var result = await _controller.GetDashboardStatistics(TestContext.Current.CancellationToken);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(statistics);

        _dashboardServiceMock.Verify(x => x.GetStatistics("user-1", TestContext.Current.CancellationToken), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboardStatistics_ShouldPassNoUserId_WhenTheUserHasNoIdentifier()
    {
        SignIn(null);
        var statistics = new DashboardStatisticsDto();
        _dashboardServiceMock
            .Setup(x => x.GetStatistics(null, TestContext.Current.CancellationToken))
            .ReturnsAsync(statistics);

        var result = await _controller.GetDashboardStatistics(TestContext.Current.CancellationToken);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(statistics);

        _dashboardServiceMock.Verify(x => x.GetStatistics(null, TestContext.Current.CancellationToken), Times.Once);
        VerifyNoOtherCalls();
    }
}
