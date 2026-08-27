using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Compares.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

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
        var playerOne = 1;
        var playerTwo = 2;
        var compareResult = new CompareResultDto
        {
            WinCount = new CompareRow<int>(10, 5),
            WinPercentage = new CompareRow<double>(66.7, 33.3),
            SessionCounts = new CompareRow<int>(15, 15),
            TotalDuration = new CompareRow<double>(300.5, 280.0),
            DirectWins = new CompareRow<int>(7, 3),
            TotalSessionsTogether = 10,
            MinutesPlayed = 500
        };

        _compareServiceMock
            .Setup(x => x.GetPlayerComparison(playerOne, playerTwo))
            .ReturnsAsync(compareResult);

        var result = await _controller.GetPlayerComparison(playerOne, playerTwo);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(compareResult);

        _compareServiceMock.Verify(x => x.GetPlayerComparison(playerOne, playerTwo), Times.Once);
        VerifyNoOtherCalls();
    }
}
