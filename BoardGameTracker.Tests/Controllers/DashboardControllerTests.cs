using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Core.Dashboard.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _controller = new DashboardController(_dashboardServiceMock.Object);
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
            TotalCollectionValue = 887.5,
            AvgGamePrice = 35.5,
            ExpansionsOwned = 15,
            AvgSessionTime = 50,
            RecentActivities = [],
            Collection = [],
            MostPlayedGames = [],
            TopPlayers = [],
            RecentAddedGames = [],
            SessionsByDayOfWeek = []
        };

        _dashboardServiceMock
            .Setup(x => x.GetStatistics())
            .ReturnsAsync(statistics);

        var result = await _controller.GetDashboardStatistics();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(statistics);

        _dashboardServiceMock.Verify(x => x.GetStatistics(), Times.Once);
        VerifyNoOtherCalls();
    }
}
