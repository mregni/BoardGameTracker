using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
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
    private readonly Mock<ILogger<PlayerController>> _loggerMock;
    private readonly PlayerController _controller;

    public PlayerControllerTests()
    {
        _playerServiceMock = new Mock<IPlayerService>();
        _loggerMock = new Mock<ILogger<PlayerController>>();
        _controller = new PlayerController(_playerServiceMock.Object, _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _playerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayers_ShouldReturnOkWithPlayers_WhenPlayersExist()
    {
        // Arrange
        var players = new List<Player>
        {
            new Player("Alice", "https://example.com/alice.jpg") { Id = 1 },
            new Player("Bob", "https://example.com/bob.jpg") { Id = 2 }
        };

        _playerServiceMock
            .Setup(x => x.GetList())
            .ReturnsAsync(players);

        // Act
        var result = await _controller.GetPlayers();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPlayers = okResult.Value.Should().BeAssignableTo<List<PlayerDto>>().Subject;

        returnedPlayers.Should().HaveCount(2);
        returnedPlayers[0].Name.Should().Be("Alice");
        returnedPlayers[1].Name.Should().Be("Bob");

        _playerServiceMock.Verify(x => x.GetList(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayers_ShouldReturnOkWithEmptyList_WhenNoPlayersExist()
    {
        // Arrange
        _playerServiceMock
            .Setup(x => x.GetList())
            .ReturnsAsync(new List<Player>());

        // Act
        var result = await _controller.GetPlayers();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPlayers = okResult.Value.Should().BeAssignableTo<List<PlayerDto>>().Subject;

        returnedPlayers.Should().BeEmpty();

        _playerServiceMock.Verify(x => x.GetList(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePlayer_ShouldReturnOkWithCreatedPlayer_WhenPlayerIsCreated()
    {
        // Arrange
        var command = new CreatePlayerCommand
        {
            Name = "Charlie",
            Image = "https://example.com/charlie.jpg"
        };

        var createdPlayer = new Player(command.Name, command.Image) { Id = 1 };

        _playerServiceMock
            .Setup(x => x.Create(It.IsAny<Player>()))
            .ReturnsAsync(createdPlayer);

        // Act
        var result = await _controller.CreatePlayer(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var playerDto = okResult.Value.Should().BeAssignableTo<PlayerDto>().Subject;

        playerDto.Id.Should().Be(1);
        playerDto.Name.Should().Be("Charlie");
        playerDto.Image.Should().Be("https://example.com/charlie.jpg");

        _playerServiceMock.Verify(x => x.Create(It.Is<Player>(p => p.Name == command.Name && p.Image == command.Image)), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePlayer_ShouldReturnBadRequest_WhenCommandIsNull()
    {
        // Act
        var result = await _controller.CreatePlayer(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePlayer_ShouldReturnInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var command = new CreatePlayerCommand
        {
            Name = "Dave",
            Image = null
        };

        var expectedException = new InvalidOperationException("Database error");

        _playerServiceMock
            .Setup(x => x.Create(It.IsAny<Player>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.CreatePlayer(command);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        _playerServiceMock.Verify(x => x.Create(It.IsAny<Player>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatePlayer_ShouldReturnOkWithUpdatedPlayer_WhenPlayerIsUpdated()
    {
        // Arrange
        var command = new UpdatePlayerCommand
        {
            Id = 1,
            Name = "Alice Updated",
            Image = "https://example.com/alice_new.jpg"
        };

        var updatedPlayer = new Player(command.Name, command.Image) { Id = command.Id };

        _playerServiceMock
            .Setup(x => x.Update(command))
            .ReturnsAsync(updatedPlayer);

        // Act
        var result = await _controller.UpdatePlayer(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var playerDto = okResult.Value.Should().BeAssignableTo<PlayerDto>().Subject;

        playerDto.Id.Should().Be(1);
        playerDto.Name.Should().Be("Alice Updated");
        playerDto.Image.Should().Be("https://example.com/alice_new.jpg");

        _playerServiceMock.Verify(x => x.Update(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatePlayer_ShouldReturnBadRequest_WhenCommandIsNull()
    {
        // Act
        var result = await _controller.UpdatePlayer(null);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatePlayer_ShouldReturnBadRequest_WhenServiceReturnsNull()
    {
        // Arrange
        var command = new UpdatePlayerCommand
        {
            Id = 999,
            Name = "NonExistent",
            Image = null
        };

        _playerServiceMock
            .Setup(x => x.Update(command))
            .ReturnsAsync((Player?)null);

        // Act
        var result = await _controller.UpdatePlayer(command);

        // Assert
        result.Should().BeOfType<BadRequestResult>();

        _playerServiceMock.Verify(x => x.Update(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatePlayer_ShouldReturnInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var command = new UpdatePlayerCommand
        {
            Id = 1,
            Name = "Eve",
            Image = null
        };

        var expectedException = new TimeoutException("Update timeout");

        _playerServiceMock
            .Setup(x => x.Update(command))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.UpdatePlayer(command);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);

        _playerServiceMock.Verify(x => x.Update(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerById_ShouldReturnOkWithPlayer_WhenPlayerExists()
    {
        // Arrange
        var playerId = 1;
        var player = new Player("Frank", "https://example.com/frank.jpg") { Id = playerId };

        _playerServiceMock
            .Setup(x => x.Get(playerId))
            .ReturnsAsync(player);

        // Act
        var result = await _controller.GetPlayerById(playerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var playerDto = okResult.Value.Should().BeAssignableTo<PlayerDto>().Subject;

        playerDto.Id.Should().Be(playerId);
        playerDto.Name.Should().Be("Frank");

        _playerServiceMock.Verify(x => x.Get(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerById_ShouldReturnNotFound_WhenPlayerDoesNotExist()
    {
        // Arrange
        var playerId = 999;

        _playerServiceMock
            .Setup(x => x.Get(playerId))
            .ReturnsAsync((Player?)null);

        // Act
        var result = await _controller.GetPlayerById(playerId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _playerServiceMock.Verify(x => x.Get(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteGameById_ShouldReturnOkWithSuccess_WhenPlayerIsDeleted()
    {
        // Arrange
        var playerId = 1;

        _playerServiceMock
            .Setup(x => x.Delete(playerId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteGameById(playerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value;

        response.Should().NotBeNull();

        _playerServiceMock.Verify(x => x.Delete(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameStats_ShouldReturnOkWithStats_WhenStatsExist()
    {
        // Arrange
        var playerId = 1;
        var stats = new PlayerStatistics
        {
            PlayCount = 10,
            WinCount = 5,
            TotalPlayedTime = 300.5,
            DistinctGameCount = 3,
            MostPlayedGames = new List<MostPlayedGame>()
        };

        _playerServiceMock
            .Setup(x => x.GetStats(playerId))
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetGameStats(playerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeAssignableTo<PlayerStatistics>().Subject;

        returnedStats.PlayCount.Should().Be(10);
        returnedStats.WinCount.Should().Be(5);
        returnedStats.TotalPlayedTime.Should().Be(300.5);
        returnedStats.DistinctGameCount.Should().Be(3);

        _playerServiceMock.Verify(x => x.GetStats(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameSessionsById_ShouldReturnOkWithSessions_WhenSessionsExist()
    {
        // Arrange
        var playerId = 1;
        var count = 10;
        var sessions = new List<Session>
        {
            new Session(1, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2).AddHours(2), "Fun game") { Id = 1 },
            new Session(2, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddHours(1), "Quick match") { Id = 2 }
        };

        _playerServiceMock
            .Setup(x => x.GetSessions(playerId, count))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetGameSessionsById(playerId, count);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;

        returnedSessions.Should().HaveCount(2);
        returnedSessions[0].Id.Should().Be(1);
        returnedSessions[1].Id.Should().Be(2);

        _playerServiceMock.Verify(x => x.GetSessions(playerId, count), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameSessionsById_ShouldReturnOkWithEmptyList_WhenNoSessionsExist()
    {
        // Arrange
        var playerId = 1;
        int? count = null;

        _playerServiceMock
            .Setup(x => x.GetSessions(playerId, count))
            .ReturnsAsync(new List<Session>());

        // Act
        var result = await _controller.GetGameSessionsById(playerId, count);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;

        returnedSessions.Should().BeEmpty();

        _playerServiceMock.Verify(x => x.GetSessions(playerId, count), Times.Once);
        VerifyNoOtherCalls();
    }
}
