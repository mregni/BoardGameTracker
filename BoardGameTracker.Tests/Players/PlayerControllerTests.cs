using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Players;

public class PlayerControllerTests
{
    private readonly Mock<IPlayerService> _playerServiceMock;
    private readonly PlayerController _controller;

    public PlayerControllerTests()
    {
        _playerServiceMock = new Mock<IPlayerService>();
        _controller = new PlayerController(_playerServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _playerServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayers_ShouldReturnOkWithPlayers_WhenPlayersExist()
    {
        var players = new List<Player>
        {
            new Player("Alice", "https://example.com/alice.jpg") { Id = 1 },
            new Player("Bob", "https://example.com/bob.jpg") { Id = 2 }
        };

        _playerServiceMock
            .Setup(x => x.GetList())
            .ReturnsAsync(players);

        var result = await _controller.GetPlayers();

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
        _playerServiceMock
            .Setup(x => x.GetList())
            .ReturnsAsync([]);

        var result = await _controller.GetPlayers();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedPlayers = okResult.Value.Should().BeAssignableTo<List<PlayerDto>>().Subject;

        returnedPlayers.Should().BeEmpty();

        _playerServiceMock.Verify(x => x.GetList(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePlayer_ShouldReturnCreatedWithPlayer_WhenPlayerIsCreated()
    {
        var command = new CreatePlayerCommand
        {
            Name = "Charlie",
            Image = "https://example.com/charlie.jpg"
        };

        var createdPlayer = new Player(command.Name, command.Image) { Id = 1 };

        _playerServiceMock
            .Setup(x => x.Create(command))
            .ReturnsAsync(createdPlayer);

        var result = await _controller.CreatePlayer(command);

        var okResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        okResult.ActionName.Should().Be(nameof(PlayerController.GetPlayerById));
        okResult.RouteValues!["id"].Should().Be(1);
        var playerDto = okResult.Value.Should().BeAssignableTo<PlayerDto>().Subject;

        playerDto.Id.Should().Be(1);
        playerDto.Name.Should().Be("Charlie");
        playerDto.Image.Should().Be("https://example.com/charlie.jpg");

        _playerServiceMock.Verify(x => x.Create(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdatePlayer_ShouldReturnOkWithUpdatedPlayer_WhenPlayerIsUpdated()
    {
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

        var result = await _controller.UpdatePlayer(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var playerDto = okResult.Value.Should().BeAssignableTo<PlayerDto>().Subject;

        playerDto.Id.Should().Be(1);
        playerDto.Name.Should().Be("Alice Updated");
        playerDto.Image.Should().Be("https://example.com/alice_new.jpg");
        playerDto.ImageSource.Should().Be(PlayerImageSource.Upload);

        _playerServiceMock.Verify(x => x.Update(command), Times.Once);
        _playerServiceMock.Verify(x => x.GetLinkedAccountAsync(1), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerById_ShouldReturnOkWithPlayer_WhenPlayerExists()
    {
        var playerId = 1;
        var player = new Player("Frank", "https://example.com/frank.jpg") { Id = playerId };

        _playerServiceMock
            .Setup(x => x.Get(playerId))
            .ReturnsAsync(player);
        _playerServiceMock
            .Setup(x => x.GetLinkedAccountAsync(playerId))
            .ReturnsAsync(new PlayerLinkedAccountDto { Id = "u1", Username = "frank", DisplayName = "Frank F" });

        var result = await _controller.GetPlayerById(playerId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var playerDto = okResult.Value.Should().BeAssignableTo<PlayerDto>().Subject;

        playerDto.Id.Should().Be(playerId);
        playerDto.Name.Should().Be("Frank");
        playerDto.LinkedAccount!.Username.Should().Be("frank");

        _playerServiceMock.Verify(x => x.Get(playerId), Times.Once);
        _playerServiceMock.Verify(x => x.GetLinkedAccountAsync(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerById_ShouldReturnNotFound_WhenPlayerDoesNotExist()
    {
        var playerId = 999;

        _playerServiceMock
            .Setup(x => x.Get(playerId))
            .ReturnsAsync((Player?)null);

        var result = await _controller.GetPlayerById(playerId);

        result.Should().BeOfType<NotFoundResult>();

        _playerServiceMock.Verify(x => x.Get(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeletePlayerById_ShouldReturnNoContent_WhenPlayerIsDeleted()
    {
        var playerId = 1;

        _playerServiceMock
            .Setup(x => x.Delete(playerId))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeletePlayerById(playerId);

        result.Should().BeOfType<NoContentResult>();

        _playerServiceMock.Verify(x => x.Delete(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerStats_ShouldReturnOkWithStats_WhenStatsExist()
    {
        var playerId = 1;
        var stats = new PlayerStatistics
        {
            PlayCount = 10,
            WinCount = 5,
            TotalPlayedTime = 300.5,
            DistinctGameCount = 3,
            MostPlayedGames = []
        };

        _playerServiceMock
            .Setup(x => x.ExistsAsync(playerId))
            .ReturnsAsync(true);
        _playerServiceMock
            .Setup(x => x.GetStats(playerId))
            .ReturnsAsync(stats);

        var result = await _controller.GetPlayerStats(playerId);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(stats);

        _playerServiceMock.Verify(x => x.ExistsAsync(playerId), Times.Once);
        _playerServiceMock.Verify(x => x.GetStats(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerStats_ShouldReturnNotFound_WhenPlayerDoesNotExist()
    {
        _playerServiceMock
            .Setup(x => x.ExistsAsync(5))
            .ReturnsAsync(false);

        var result = await _controller.GetPlayerStats(5);

        result.Should().BeOfType<NotFoundResult>();

        _playerServiceMock.Verify(x => x.ExistsAsync(5), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerSessionsById_ShouldReturnOkWithSessions_WhenSessionsExist()
    {
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

        var result = await _controller.GetPlayerSessionsById(playerId, count);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;

        returnedSessions.Should().HaveCount(2);
        returnedSessions[0].Id.Should().Be(1);
        returnedSessions[1].Id.Should().Be(2);

        _playerServiceMock.Verify(x => x.GetSessions(playerId, count), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayerSessionsById_ShouldReturnOkWithEmptyList_WhenNoSessionsExist()
    {
        var playerId = 1;
        int? count = null;

        _playerServiceMock
            .Setup(x => x.GetSessions(playerId, count))
            .ReturnsAsync([]);

        var result = await _controller.GetPlayerSessionsById(playerId, count);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSessions = okResult.Value.Should().BeAssignableTo<List<SessionDto>>().Subject;

        returnedSessions.Should().BeEmpty();

        _playerServiceMock.Verify(x => x.GetSessions(playerId, count), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPlayersOverview_ShouldReturnOkWithOverview()
    {
        var overview = new List<PlayerOverviewDto>
        {
            new() { Id = 1, Name = "Alice", PlayCount = 4, WinCount = 2 },
            new() { Id = 2, Name = "Bob" }
        };
        _playerServiceMock.Setup(x => x.GetOverview()).ReturnsAsync(overview);

        var result = await _controller.GetPlayersOverview();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(overview);
        _playerServiceMock.Verify(x => x.GetOverview(), Times.Once);
        VerifyNoOtherCalls();
    }
}
