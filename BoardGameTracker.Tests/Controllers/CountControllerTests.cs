using System;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class CountControllerTests
    {
        private readonly Mock<IGameService> _gameServiceMock;
        private readonly Mock<IPlayerService> _playerServiceMock;
        private readonly Mock<ILocationService> _locationServiceMock;
        private readonly CountController _controller;

        public CountControllerTests()
        {
            _gameServiceMock = new Mock<IGameService>();
            _playerServiceMock = new Mock<IPlayerService>();
            _locationServiceMock = new Mock<ILocationService>();
            
            _controller = new CountController(
                _locationServiceMock.Object,
                _playerServiceMock.Object,
                _gameServiceMock.Object);
        }

        [Fact]
        public async Task GetMenuCounts_ShouldReturnOkResultWithCorrectCounts_WhenAllServicesReturnValidCounts()
        {
            const int expectedGameCount = 25;
            const int expectedPlayerCount = 150;
            const int expectedLocationCount = 8;

            _gameServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(expectedGameCount);

            _playerServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(expectedPlayerCount);

            _locationServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(expectedLocationCount);

            var result = await _controller.GetMenuCounts();

            result.Should().BeOfType<OkObjectResult>();
            
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<KeyValuePairViewModel<string, int>[]>();
            
            var counts = okResult.Value as KeyValuePairViewModel<string, int>[];
            counts.Should().HaveCount(3);
            
            counts![0].Key.Should().Be("games");
            counts[0].Value.Should().Be(expectedGameCount);
            
            counts[1].Key.Should().Be("players");
            counts[1].Value.Should().Be(expectedPlayerCount);
            
            counts[2].Key.Should().Be("locations");
            counts[2].Value.Should().Be(expectedLocationCount);

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _playerServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _locationServiceMock.Verify(x => x.CountAsync(), Times.Once);
            
            _gameServiceMock.VerifyNoOtherCalls();
            _playerServiceMock.VerifyNoOtherCalls();
            _locationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMenuCounts_ShouldReturnOkResultWithZeroCounts_WhenAllServicesReturnZero()
        {
            _gameServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(0);

            _playerServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(0);

            _locationServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(0);

            var result = await _controller.GetMenuCounts();

            result.Should().BeOfType<OkObjectResult>();
            
            var okResult = result as OkObjectResult;
            var counts = okResult!.Value as KeyValuePairViewModel<string, int>[];
            
            counts.Should().HaveCount(3);
            counts!.Should().AllSatisfy(c => c.Value.Should().Be(0));

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _playerServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _locationServiceMock.Verify(x => x.CountAsync(), Times.Once);
            
            _gameServiceMock.VerifyNoOtherCalls();
            _playerServiceMock.VerifyNoOtherCalls();
            _locationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMenuCounts_ShouldThrowException_WhenGameServiceThrows()
        {
            var expectedException = new InvalidOperationException("Game service error");
            
            _gameServiceMock
                .Setup(x => x.CountAsync())
                .ThrowsAsync(expectedException);

            _playerServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(100);

            _locationServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(5);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.GetMenuCounts());
            
            exception.Should().Be(expectedException);

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _gameServiceMock.VerifyNoOtherCalls();
            
            _playerServiceMock.VerifyNoOtherCalls();
            _locationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMenuCounts_ShouldThrowException_WhenPlayerServiceThrows()
        {
            var expectedException = new ArgumentException("Player service error");
            
            _gameServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(50);

            _playerServiceMock
                .Setup(x => x.CountAsync())
                .ThrowsAsync(expectedException);

            _locationServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(3);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _controller.GetMenuCounts());
            
            exception.Should().Be(expectedException);

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _playerServiceMock.Verify(x => x.CountAsync(), Times.Once);
            
            _gameServiceMock.VerifyNoOtherCalls();
            _playerServiceMock.VerifyNoOtherCalls();
            _locationServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMenuCounts_ShouldThrowException_WhenLocationServiceThrows()
        {
            var expectedException = new TimeoutException("Location service timeout");
            
            _gameServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(75);

            _playerServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(200);

            _locationServiceMock
                .Setup(x => x.CountAsync())
                .ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => _controller.GetMenuCounts());
            
            exception.Should().Be(expectedException);

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _playerServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _locationServiceMock.Verify(x => x.CountAsync(), Times.Once);
            
            _gameServiceMock.VerifyNoOtherCalls();
            _playerServiceMock.VerifyNoOtherCalls();
            _locationServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(100, 500, 25)]
        [InlineData(0, 1000, 0)]
        [InlineData(999, 0, 50)]
        public async Task GetMenuCounts_ShouldReturnCorrectOrder_WithVariousCounts(
            int gameCount, int playerCount, int locationCount)
        {
            _gameServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(gameCount);

            _playerServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(playerCount);

            _locationServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(locationCount);

            var result = await _controller.GetMenuCounts();

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var counts = okResult.Value.Should().BeOfType<KeyValuePairViewModel<string, int>[]>().Subject;
            
            counts.Should().HaveCount(3);
            
            counts[0].Key.Should().Be("games");
            counts[0].Value.Should().Be(gameCount);
            
            counts[1].Key.Should().Be("players");
            counts[1].Value.Should().Be(playerCount);
            
            counts[2].Key.Should().Be("locations");
            counts[2].Value.Should().Be(locationCount);

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _playerServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _locationServiceMock.Verify(x => x.CountAsync(), Times.Once);
            
            _gameServiceMock.VerifyNoOtherCalls();
            _playerServiceMock.VerifyNoOtherCalls();
            _locationServiceMock.VerifyNoOtherCalls();
        }
    }