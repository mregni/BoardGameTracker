using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.GameNights.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class GameNightStatisticsControllerTests
{
    private readonly Mock<IGameNightStatisticsService> _statisticsServiceMock;
    private readonly GameNightStatisticsController _controller;

    public GameNightStatisticsControllerTests()
    {
        _statisticsServiceMock = new Mock<IGameNightStatisticsService>();
        _controller = new GameNightStatisticsController(_statisticsServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _statisticsServiceMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GameNightStatisticsPeriod.AllTime)]
    [InlineData(GameNightStatisticsPeriod.ThisYear)]
    [InlineData(GameNightStatisticsPeriod.Last12Months)]
    public async Task GetStatistics_ShouldReturnTheStatisticsOfThePeriod(GameNightStatisticsPeriod period)
    {
        var statistics = new GameNightStatisticsDto { Period = period, Nights = 3 };
        _statisticsServiceMock
            .Setup(x => x.GetStatistics(period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(statistics);

        var result = await _controller.GetStatistics(period);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(statistics);
        _statisticsServiceMock.Verify(x => x.GetStatistics(period, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public void GetStatistics_ShouldBeOpenToEverySignedInUser()
    {
        var authorize = typeof(GameNightStatisticsController).GetCustomAttributes<AuthorizeAttribute>().ToList();
        var method = typeof(GameNightStatisticsController).GetMethod(nameof(GameNightStatisticsController.GetStatistics))!;

        authorize.Should().ContainSingle().Which.Roles.Should().BeNull();
        method.GetCustomAttributes<AuthorizeAttribute>().Should().BeEmpty();
        method.GetCustomAttributes<HttpGetAttribute>().Should().ContainSingle();
        VerifyNoOtherCalls();
    }
}
