using System;
using System.Threading.Tasks;
using AutoMapper;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Common.Models.Dashboard;
using BoardGameTracker.Common.ViewModels.Dashboard;
using BoardGameTracker.Core.Dashboard.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _mapperMock = new Mock<IMapper>();
        _dashboardServiceMock = new Mock<IDashboardService>();

        _controller = new DashboardController(_mapperMock.Object, _dashboardServiceMock.Object);
    }

    [Fact]
    public async Task GetDashboardStatistics_ShouldReturnOkResultWithStatistics_WhenServiceReturnsValidData()
    {
        var expectedStatistics = new DashboardStatistics
        {
            GameCount = 20,
            LocationCount = 10,
            PlayerCount = 5
        };

        _dashboardServiceMock
            .Setup(x => x.GetStatistics())
            .ReturnsAsync(expectedStatistics);

        var result = await _controller.GetDashboardStatistics();

        result.Should().BeOfType<OkObjectResult>();

        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(expectedStatistics);

        _dashboardServiceMock.Verify(x => x.GetStatistics(), Times.Once);
        _dashboardServiceMock.VerifyNoOtherCalls();

        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboardStatistics_ShouldThrowException_WhenServiceThrows()
    {
        var expectedException = new InvalidOperationException("Database connection failed");

        _dashboardServiceMock
            .Setup(x => x.GetStatistics())
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetDashboardStatistics());

        exception.Should().Be(expectedException);

        _dashboardServiceMock.Verify(x => x.GetStatistics(), Times.Once);
        _dashboardServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboardCharts_ShouldReturnOkResultWithMappedCharts_WhenServiceReturnsValidData()
    {
        // Arrange
        var serviceResult = new DashboardCharts
        {
            GameState =
            [
                new GameStateChart {Type = GameState.Owned, GameCount = 100},
                new GameStateChart {Type = GameState.Wanted, GameCount = 2}
            ],
        };

        var expectedMappedResult = new DashboardChartsViewModel
        {
            GameState =
            [
                new GameStateChartViewModel { Type = 1, GameCount = 100 },
                new GameStateChartViewModel { Type = 0, GameCount = 2 }
            ]
        };

        _dashboardServiceMock
            .Setup(x => x.GetCharts())
            .ReturnsAsync(serviceResult);

        _mapperMock
            .Setup(x => x.Map<DashboardChartsViewModel>(serviceResult))
            .Returns(expectedMappedResult);

        var result = await _controller.GetDashboardCharts();

        result.Should().BeOfType<OkObjectResult>();

        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(expectedMappedResult);

        _dashboardServiceMock.Verify(x => x.GetCharts(), Times.Once);
        _mapperMock.Verify(x => x.Map<DashboardChartsViewModel>(serviceResult), Times.Once);

        _dashboardServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDashboardCharts_ShouldThrowException_WhenServiceThrows()
    {
        var expectedException = new TimeoutException("Service timeout");

        _dashboardServiceMock
            .Setup(x => x.GetCharts())
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => _controller.GetDashboardCharts());

        exception.Should().Be(expectedException);

        _dashboardServiceMock.Verify(x => x.GetCharts(), Times.Once);
        _dashboardServiceMock.VerifyNoOtherCalls();
        _mapperMock.VerifyNoOtherCalls();
    }
}