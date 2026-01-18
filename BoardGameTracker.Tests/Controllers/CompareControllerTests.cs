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
        // Arrange
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
            .Setup(x => x.GetPlayerComparisation(playerOne, playerTwo))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _controller.GetPlayerComparison(playerOne, playerTwo);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeAssignableTo<CompareResultDto>().Subject;

        returnedResult.WinCount.PlayerOne.Should().Be(10);
        returnedResult.WinCount.PlayerTwo.Should().Be(5);
        returnedResult.WinPercentage.PlayerOne.Should().Be(66.7);
        returnedResult.WinPercentage.PlayerTwo.Should().Be(33.3);
        returnedResult.DirectWins.PlayerOne.Should().Be(7);
        returnedResult.DirectWins.PlayerTwo.Should().Be(3);
        returnedResult.TotalSessionsTogether.Should().Be(10);
        returnedResult.MinutesPlayed.Should().Be(500);

        _compareServiceMock.Verify(x => x.GetPlayerComparisation(playerOne, playerTwo), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnOkWithEmptyResult_WhenNoSessionsTogether()
    {
        // Arrange
        var playerOne = 1;
        var playerTwo = 2;
        var compareResult = new CompareResultDto
        {
            TotalSessionsTogether = 0,
            MinutesPlayed = 0
        };

        _compareServiceMock
            .Setup(x => x.GetPlayerComparisation(playerOne, playerTwo))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _controller.GetPlayerComparison(playerOne, playerTwo);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeAssignableTo<CompareResultDto>().Subject;

        returnedResult.TotalSessionsTogether.Should().Be(0);
        returnedResult.MinutesPlayed.Should().Be(0);

        _compareServiceMock.Verify(x => x.GetPlayerComparisation(playerOne, playerTwo), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldCallServiceWithCorrectParameters()
    {
        // Arrange
        var playerOne = 5;
        var playerTwo = 10;
        var compareResult = new CompareResultDto();

        _compareServiceMock
            .Setup(x => x.GetPlayerComparisation(playerOne, playerTwo))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _controller.GetPlayerComparison(playerOne, playerTwo);

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        _compareServiceMock.Verify(x => x.GetPlayerComparisation(5, 10), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerComparison_ShouldReturnOkWithResult_WhenComparingSamePlayer()
    {
        // Arrange
        var playerId = 1;
        var compareResult = new CompareResultDto
        {
            WinCount = new CompareRow<int>(10, 10),
            DirectWins = new CompareRow<int>(0, 0),
            TotalSessionsTogether = 0
        };

        _compareServiceMock
            .Setup(x => x.GetPlayerComparisation(playerId, playerId))
            .ReturnsAsync(compareResult);

        // Act
        var result = await _controller.GetPlayerComparison(playerId, playerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeAssignableTo<CompareResultDto>().Subject;

        returnedResult.WinCount.PlayerOne.Should().Be(10);
        returnedResult.WinCount.PlayerTwo.Should().Be(10);

        _compareServiceMock.Verify(x => x.GetPlayerComparisation(playerId, playerId), Times.Once);
        VerifyNoOtherCalls();
    }
}
