using System;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Loans.Interfaces;
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
        private readonly Mock<ILoanService> _loanServiceMock;
        private readonly CountController _controller;

        public CountControllerTests()
        {
            _gameServiceMock = new Mock<IGameService>();
            _playerServiceMock = new Mock<IPlayerService>();
            _locationServiceMock = new Mock<ILocationService>();
            _loanServiceMock = new Mock<ILoanService>();

            _controller = new CountController(
                _locationServiceMock.Object,
                _playerServiceMock.Object,
                _gameServiceMock.Object,
                _loanServiceMock.Object);
        }

        private void VerifyNoOtherCalls()
        {
            _gameServiceMock.VerifyNoOtherCalls();
            _playerServiceMock.VerifyNoOtherCalls();
            _locationServiceMock.VerifyNoOtherCalls();
            _loanServiceMock.VerifyNoOtherCalls();
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

            _gameServiceMock
                .Setup(x => x.CountShelfOfShameGames())
                .ReturnsAsync(2);

            _loanServiceMock
                .Setup(x => x.CountActiveLoans())
                .ReturnsAsync(3);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _controller.GetMenuCounts());

            exception.Should().Be(expectedException);

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            VerifyNoOtherCalls();
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

            _gameServiceMock
                .Setup(x => x.CountShelfOfShameGames())
                .ReturnsAsync(2);

            _loanServiceMock
                .Setup(x => x.CountActiveLoans())
                .ReturnsAsync(3);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _controller.GetMenuCounts());

            exception.Should().Be(expectedException);

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _playerServiceMock.Verify(x => x.CountAsync(), Times.Once);
            VerifyNoOtherCalls();
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

            _gameServiceMock
                .Setup(x => x.CountShelfOfShameGames())
                .ReturnsAsync(2);

            _loanServiceMock
                .Setup(x => x.CountActiveLoans())
                .ReturnsAsync(3);

            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => _controller.GetMenuCounts());

            exception.Should().Be(expectedException);

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _playerServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _locationServiceMock.Verify(x => x.CountAsync(), Times.Once);
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMenuCounts_ShouldReturnCounts_WhenSuccessful()
        {
            // Arrange
            _gameServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(42);

            _playerServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(15);

            _locationServiceMock
                .Setup(x => x.CountAsync())
                .ReturnsAsync(7);

            _gameServiceMock
                .Setup(x => x.CountShelfOfShameGames())
                .ReturnsAsync(3);

            _loanServiceMock
                .Setup(x => x.CountActiveLoans())
                .ReturnsAsync(2);

            // Act
            var result = await _controller.GetMenuCounts();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var counts = okResult.Value.Should().BeAssignableTo<BoardGameTracker.Common.DTOs.KeyValuePairDto<int>[]>().Subject;

            counts.Should().HaveCount(5);
            counts[0].Key.Should().Be("games");
            counts[0].Value.Should().Be(42);
            counts[1].Key.Should().Be("players");
            counts[1].Value.Should().Be(15);
            counts[2].Key.Should().Be("locations");
            counts[2].Value.Should().Be(7);
            counts[3].Key.Should().Be("shames");
            counts[3].Value.Should().Be(3);
            counts[4].Key.Should().Be("loans");
            counts[4].Value.Should().Be(2);

            _gameServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _playerServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _locationServiceMock.Verify(x => x.CountAsync(), Times.Once);
            _gameServiceMock.Verify(x => x.CountShelfOfShameGames(), Times.Once);
            _loanServiceMock.Verify(x => x.CountActiveLoans(), Times.Once);
            VerifyNoOtherCalls();
        }
    }