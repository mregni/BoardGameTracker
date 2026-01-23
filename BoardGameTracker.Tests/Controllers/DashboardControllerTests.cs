using System.Collections.Generic;
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
        // Arrange
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
            RecentActivities = new List<RecentActivityDto>(),
            Collection = new List<Common.Models.Charts.GameStateChart>(),
            MostPlayedGames = new List<MostPlayedGameDto>(),
            TopPlayers = new List<DashboardTopPlayerDto>(),
            RecentAddedGames = new List<RecentGameDto>(),
            SessionsByDayOfWeek = new List<Common.Models.Charts.PlayByDay>()
        };

        _dashboardServiceMock
            .Setup(x => x.GetStatistics())
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeAssignableTo<DashboardStatisticsDto>().Subject;

        returnedStats.TotalGames.Should().Be(25);
        returnedStats.ActivePlayers.Should().Be(10);
        returnedStats.SessionsPlayed.Should().Be(100);
        returnedStats.TotalPlayedTime.Should().Be(5000);
        returnedStats.TotalCollectionValue.Should().Be(887.5);
        returnedStats.AvgGamePrice.Should().Be(35.5);
        returnedStats.ExpansionsOwned.Should().Be(15);
        returnedStats.AvgSessionTime.Should().Be(50);

        _dashboardServiceMock.Verify(x => x.GetStatistics(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboardStatistics_ShouldReturnOkWithZeroValues_WhenNoDataExists()
    {
        // Arrange
        var statistics = new DashboardStatisticsDto
        {
            TotalGames = 0,
            ActivePlayers = 0,
            SessionsPlayed = 0,
            TotalPlayedTime = 0,
            TotalCollectionValue = null,
            AvgGamePrice = null,
            ExpansionsOwned = 0,
            AvgSessionTime = 0
        };

        _dashboardServiceMock
            .Setup(x => x.GetStatistics())
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeAssignableTo<DashboardStatisticsDto>().Subject;

        returnedStats.TotalGames.Should().Be(0);
        returnedStats.ActivePlayers.Should().Be(0);
        returnedStats.SessionsPlayed.Should().Be(0);
        returnedStats.TotalPlayedTime.Should().Be(0);
        returnedStats.TotalCollectionValue.Should().BeNull();
        returnedStats.AvgGamePrice.Should().BeNull();

        _dashboardServiceMock.Verify(x => x.GetStatistics(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboardStatistics_ShouldReturnOkWithLists_WhenListDataExists()
    {
        // Arrange
        var statistics = new DashboardStatisticsDto
        {
            TotalGames = 5,
            ActivePlayers = 3,
            SessionsPlayed = 10,
            RecentActivities = new List<RecentActivityDto>
            {
                new() { Id = 1, GameId = 1, GameTitle = "Catan", PlayerCount = 4 }
            },
            MostPlayedGames = new List<MostPlayedGameDto>
            {
                new() { Id = 1, Title = "Catan", TotalSessions = 10 }
            },
            TopPlayers = new List<DashboardTopPlayerDto>
            {
                new() { Id = 1, Name = "John", PlayCount = 15, WinCount = 8 }
            },
            RecentAddedGames = new List<RecentGameDto>
            {
                new() { Id = 2, Title = "Ticket to Ride" }
            }
        };

        _dashboardServiceMock
            .Setup(x => x.GetStatistics())
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeAssignableTo<DashboardStatisticsDto>().Subject;

        returnedStats.RecentActivities.Should().HaveCount(1);
        returnedStats.MostPlayedGames.Should().HaveCount(1);
        returnedStats.TopPlayers.Should().HaveCount(1);
        returnedStats.RecentAddedGames.Should().HaveCount(1);

        _dashboardServiceMock.Verify(x => x.GetStatistics(), Times.Once);
        VerifyNoOtherCalls();
    }
}
