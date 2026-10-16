using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Compare;
using BoardGameTracker.Core.Compares.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Compares;

public class CompareControllerTests
{
    private readonly Mock<ICompareService> _compareServiceMock;
    private readonly CompareController _controller;

    public CompareControllerTests()
    {
        _compareServiceMock = new Mock<ICompareService>();
        _controller = new CompareController(_compareServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _compareServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnOkWithCompareResult_WhenPlayersExist()
    {
        var compareResult = new CompareResultDto
        {
            DirectWins = new CompareRow<int>(7, 3),
            WonByOthers = 2,
            TotalSessionsTogether = 12,
            MinutesPlayed = 500
        };

        _compareServiceMock
            .Setup(x => x.GetPlayerComparison(1, 2))
            .ReturnsAsync(compareResult);

        var result = await _controller.GetPlayerComparison(1, 2);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(compareResult);

        _compareServiceMock.Verify(x => x.GetPlayerComparison(1, 2), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnBadRequest_WhenBothSidesAreTheSamePlayer()
    {
        var result = await _controller.GetPlayerComparison(4, 4);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be(Constants.Errors.CompareSamePlayer);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDefaultPair_ShouldReturnOkWithThePair()
    {
        var pair = new ComparePairDto { PlayerOneId = 2, PlayerTwoId = 5, SessionCount = 9 };
        _compareServiceMock.Setup(x => x.GetDefaultPair()).ReturnsAsync(pair);

        var result = await _controller.GetDefaultPair();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(pair);
        _compareServiceMock.Verify(x => x.GetDefaultPair(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDefaultPair_ShouldReturnNoContent_WhenNobodyPlayedTogether()
    {
        _compareServiceMock.Setup(x => x.GetDefaultPair()).ReturnsAsync((ComparePairDto?)null);

        var result = await _controller.GetDefaultPair();

        result.Should().BeOfType<NoContentResult>();
        _compareServiceMock.Verify(x => x.GetDefaultPair(), Times.Once);
        VerifyNoOtherCalls();
    }
}
