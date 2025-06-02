using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

    public class PlayerControllerTests
    {
        private readonly Mock<IPlayerService> _playerServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<PlayerController>> _loggerMock;
        private readonly PlayerController _controller;

        public PlayerControllerTests()
        {
            _playerServiceMock = new Mock<IPlayerService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<PlayerController>>();
            
            _controller = new PlayerController(
                _playerServiceMock.Object, 
                _mapperMock.Object, 
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetPlayers_ShouldReturnOkResultWithMappedPlayers_WhenServiceReturnsPlayers()
        {
            var players = new List<Player>
            {
                new() { Id = 1, Name = "John", Image = "john.jpg" },
                new() { Id = 2, Name = "Jane", Image = "jane.jpg" }
            };

            var mappedPlayers = new List<PlayerViewModel>
            {
                new() { Id = "1", Name = "John", Image = "john.jpg" },
                new() { Id = "2", Name = "Jane", Image = "jane.jpg" }
            };

            _playerServiceMock.Setup(x => x.GetList()).ReturnsAsync(players);
            _mapperMock.Setup(x => x.Map<IList<PlayerViewModel>>(players)).Returns(mappedPlayers);

            var result = await _controller.GetPlayers();

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(mappedPlayers);

            _playerServiceMock.Verify(x => x.GetList(), Times.Once);
            _mapperMock.Verify(x => x.Map<IList<PlayerViewModel>>(players), Times.Once);
            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetPlayers_ShouldReturnOkResultWithEmptyList_WhenServiceReturnsEmptyList()
        {
            var players = new List<Player>();
            var mappedPlayers = new List<PlayerViewModel>();

            _playerServiceMock.Setup(x => x.GetList()).ReturnsAsync(players);
            _mapperMock.Setup(x => x.Map<IList<PlayerViewModel>>(players)).Returns(mappedPlayers);

            var result = await _controller.GetPlayers();

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(mappedPlayers);

            _playerServiceMock.Verify(x => x.GetList(), Times.Once);
            _mapperMock.Verify(x => x.Map<IList<PlayerViewModel>>(players), Times.Once);
            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }


        [Theory]
        [InlineData("")]
        [InlineData("John Doe")]
        [InlineData("Player With Very Long Name And Special Characters!@#")]
        public async Task CreatePlayer_ShouldHandleDifferentNames_WhenValidNamesProvided(string playerName)
        {
            var creationViewModel = new PlayerCreationViewModel { Name = playerName, Image = "test.jpg" };
            var player = new Player { Name = playerName, Image = "test.jpg" };
            var createdPlayer = new Player { Id = 1, Name = playerName, Image = "test.jpg" };
            var playerViewModel = new PlayerViewModel { Id = "1", Name = playerName, Image = "test.jpg" };

            _mapperMock.Setup(x => x.Map<Player>(creationViewModel)).Returns(player);
            _playerServiceMock.Setup(x => x.Create(player)).ReturnsAsync(createdPlayer);
            _mapperMock.Setup(x => x.Map<PlayerViewModel>(createdPlayer)).Returns(playerViewModel);

            var result = await _controller.CreatePlayer(creationViewModel);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(playerViewModel);

            _mapperMock.Verify(x => x.Map<Player>(creationViewModel), Times.Once);
            _playerServiceMock.Verify(x => x.Create(player), Times.Once);
            _mapperMock.Verify(x => x.Map<PlayerViewModel>(createdPlayer), Times.Once);
            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }


        [Fact]
        public async Task CreatePlayer_ShouldReturnBadRequest_WhenViewModelIsNull()
        {
            var result = await _controller.CreatePlayer(null);

            result.Should().BeOfType<BadRequestResult>();

            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreatePlayer_ShouldReturnInternalServerError_WhenServiceThrowsException()
        {
            var creationViewModel = new PlayerCreationViewModel { Name = "Test Player" };
            var player = new Player { Name = "Test Player" };
            var expectedException = new InvalidOperationException("Service error");

            _mapperMock.Setup(x => x.Map<Player>(creationViewModel)).Returns(player);
            _playerServiceMock.Setup(x => x.Create(player)).ThrowsAsync(expectedException);

            var result = await _controller.CreatePlayer(creationViewModel);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult!.StatusCode.Should().Be(500);

            _mapperMock.Verify(x => x.Map<Player>(creationViewModel), Times.Once);
            _playerServiceMock.Verify(x => x.Create(player), Times.Once);
            _mapperMock.VerifyNoOtherCalls();
            _playerServiceMock.VerifyNoOtherCalls();
            VerifyLoggerErrorCalled(expectedException.Message);
        }

        [Fact]
        public async Task UpdatePlayer_ShouldReturnOkResultWithMappedPlayer_WhenValidViewModelProvided()
        {
            var viewModel = new PlayerViewModel { Id = "1", Name = "Updated Player", Image = "updated.jpg" };
            var player = new Player { Id = 1, Name = "Updated Player", Image = "updated.jpg" };
            var updatedPlayer = new Player { Id = 1, Name = "Updated Player", Image = "updated.jpg" };
            var resultViewModel = new PlayerViewModel { Id = "1", Name = "Updated Player", Image = "updated.jpg" };

            _mapperMock.Setup(x => x.Map<Player>(viewModel)).Returns(player);
            _playerServiceMock.Setup(x => x.Update(player)).ReturnsAsync(updatedPlayer);
            _mapperMock.Setup(x => x.Map<PlayerViewModel>(updatedPlayer)).Returns(resultViewModel);

            var result = await _controller.UpdatePlayer(viewModel);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(resultViewModel);

            _mapperMock.Verify(x => x.Map<Player>(viewModel), Times.Once);
            _playerServiceMock.Verify(x => x.Update(player), Times.Once);
            _mapperMock.Verify(x => x.Map<PlayerViewModel>(updatedPlayer), Times.Once);
            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdatePlayer_ShouldReturnBadRequest_WhenViewModelIdIsNull()
        {
            PlayerViewModel? viewModel = null;

            var result = await _controller.UpdatePlayer(viewModel);

            result.Should().BeOfType<BadRequestResult>();

            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdatePlayer_ShouldReturnInternalServerError_WhenServiceThrowsException()
        {
            var viewModel = new PlayerViewModel { Id = "1", Name = "Test Player" };
            var player = new Player { Id = 1, Name = "Test Player" };
            var expectedException = new InvalidOperationException("Update failed");

            _mapperMock.Setup(x => x.Map<Player>(viewModel)).Returns(player);
            _playerServiceMock.Setup(x => x.Update(player)).ThrowsAsync(expectedException);

            var result = await _controller.UpdatePlayer(viewModel);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult!.StatusCode.Should().Be(500);

            _mapperMock.Verify(x => x.Map<Player>(viewModel), Times.Once);
            _playerServiceMock.Verify(x => x.Update(player), Times.Once);
            _mapperMock.VerifyNoOtherCalls();
            _playerServiceMock.VerifyNoOtherCalls();
            VerifyLoggerErrorCalled(expectedException.Message);
        }

        [Fact]
        public async Task GetPlayerById_ShouldReturnNotFound_WhenPlayerDoesNotExist()
        {
            const int playerId = 1;

            _playerServiceMock.Setup(x => x.Get(playerId)).ReturnsAsync((Player?)null);

            var result = await _controller.GetPlayerById(playerId);

            result.Should().BeOfType<NotFoundResult>();

            _playerServiceMock.Verify(x => x.Get(playerId), Times.Once);
            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public async Task GetPlayerById_ShouldHandleDifferentIds_WhenValidIdsProvided(int playerId)
        {
            var player = new Player { Id = playerId, Name = "Test Player" };
            var playerViewModel = new PlayerViewModel { Id = playerId.ToString(), Name = "Test Player" };

            _playerServiceMock.Setup(x => x.Get(playerId)).ReturnsAsync(player);
            _mapperMock.Setup(x => x.Map<PlayerViewModel>(player)).Returns(playerViewModel);

            var result = await _controller.GetPlayerById(playerId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(playerViewModel);

            _playerServiceMock.Verify(x => x.Get(playerId), Times.Once);
            _mapperMock.Verify(x => x.Map<PlayerViewModel>(player), Times.Once);
            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public async Task DeleteGameById_ShouldHandleDifferentIds_WhenValidIdsProvided(int playerId)
        {
            _playerServiceMock.Setup(x => x.Delete(playerId)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteGameById(playerId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var deletionResult = okResult!.Value as DeletionResultViewModel;
            deletionResult!.Type.Should().Be((int)ResultState.Success);

            _playerServiceMock.Verify(x => x.Delete(playerId), Times.Once);
            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetGameStats_ShouldReturnOkResultWithMappedStats_WhenStatsExist()
        {
            const int playerId = 1;
            var stats = new PlayerStatistics 
            { 
                PlayCount = 10, 
                WinCount = 5, 
                TotalPlayedTime = 500.5,
                DistinctGameCount = 3,
            };
            var statsViewModel = new PlayerStatisticsViewModel 
            { 
                PlayCount = 10, 
                WinCount = 5, 
                TotalPlayedTime = 500.5,
                DistinctGameCount = 3,
                BestGameId = 1,
                MostWinsGame = new BestWinningGameViewModel { TotalWins = 5 }
            };

            _playerServiceMock.Setup(x => x.GetStats(playerId)).ReturnsAsync(stats);
            _mapperMock.Setup(x => x.Map<PlayerStatisticsViewModel>(stats)).Returns(statsViewModel);

            var result = await _controller.GetGameStats(playerId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(statsViewModel);

            _playerServiceMock.Verify(x => x.GetStats(playerId), Times.Once);
            _mapperMock.Verify(x => x.Map<PlayerStatisticsViewModel>(stats), Times.Once);
            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public async Task GetGameStats_ShouldHandleDifferentIds_WhenValidIdsProvided(int playerId)
        {
            var stats = new PlayerStatistics { PlayCount = 5, WinCount = 2 };
            var statsViewModel = new PlayerStatisticsViewModel 
            { 
                PlayCount = 5, 
                WinCount = 2,
                MostWinsGame = new BestWinningGameViewModel { TotalWins = 2 }
            };

            _playerServiceMock.Setup(x => x.GetStats(playerId)).ReturnsAsync(stats);
            _mapperMock.Setup(x => x.Map<PlayerStatisticsViewModel>(stats)).Returns(statsViewModel);

            var result = await _controller.GetGameStats(playerId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(statsViewModel);

            _playerServiceMock.Verify(x => x.GetStats(playerId), Times.Once);
            _mapperMock.Verify(x => x.Map<PlayerStatisticsViewModel>(stats), Times.Once);
            _playerServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
        }

        private void VerifyLoggerErrorCalled(string expectedMessage)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _loggerMock.VerifyNoOtherCalls();
        }
    }