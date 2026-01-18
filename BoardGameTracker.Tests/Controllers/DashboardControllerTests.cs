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

    #region GetDashboardStatistics Tests

    [Fact]
    public async Task GetDashboardStatistics_ShouldReturnOkWithStatistics_WhenDataExists()
    {
        // Arrange
        var statistics = new DashboardStatisticsDto
        {
            GameCount = 25,
            PlayerCount = 10,
            SessionCount = 100,
            TotalPlayTime = 5000
        };

        _dashboardServiceMock
            .Setup(x => x.GetStatistics())
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeAssignableTo<DashboardStatisticsDto>().Subject;

        returnedStats.GameCount.Should().Be(25);
        returnedStats.PlayerCount.Should().Be(10);
        returnedStats.SessionCount.Should().Be(100);
        returnedStats.TotalPlayTime.Should().Be(5000);

        _dashboardServiceMock.Verify(x => x.GetStatistics(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboardStatistics_ShouldReturnOkWithZeroValues_WhenNoDataExists()
    {
        // Arrange
        var statistics = new DashboardStatisticsDto
        {
            GameCount = 0,
            PlayerCount = 0,
            SessionCount = 0,
            TotalPlayTime = 0
        };

        _dashboardServiceMock
            .Setup(x => x.GetStatistics())
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetDashboardStatistics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeAssignableTo<DashboardStatisticsDto>().Subject;

        returnedStats.GameCount.Should().Be(0);
        returnedStats.PlayerCount.Should().Be(0);
        returnedStats.SessionCount.Should().Be(0);
        returnedStats.TotalPlayTime.Should().Be(0);

        _dashboardServiceMock.Verify(x => x.GetStatistics(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetDashboardCharts Tests

    [Fact]
    public async Task GetDashboardCharts_ShouldReturnOkWithCharts_WhenDataExists()
    {
        // Arrange
        var charts = new DashboardChartsDto();

        _dashboardServiceMock
            .Setup(x => x.GetCharts())
            .ReturnsAsync(charts);

        // Act
        var result = await _controller.GetDashboardCharts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<DashboardChartsDto>();

        _dashboardServiceMock.Verify(x => x.GetCharts(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboardCharts_ShouldReturnOkWithEmptyCharts_WhenNoDataExists()
    {
        // Arrange
        var charts = new DashboardChartsDto();

        _dashboardServiceMock
            .Setup(x => x.GetCharts())
            .ReturnsAsync(charts);

        // Act
        var result = await _controller.GetDashboardCharts();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();

        _dashboardServiceMock.Verify(x => x.GetCharts(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
