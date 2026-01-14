using System;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
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

        private void VerifyNoOtherCalls()
        {
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
    }