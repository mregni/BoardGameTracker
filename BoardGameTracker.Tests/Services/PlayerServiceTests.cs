using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class PlayerServiceTests
{
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<IPlayerStatisticsService> _playerStatisticsDomainServiceMock;
    private readonly Mock<IGameSessionRepository> _gameSessionRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly PlayerService _playerService;

    public PlayerServiceTests()
    {
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _imageServiceMock = new Mock<IImageService>();
        _playerStatisticsDomainServiceMock = new Mock<IPlayerStatisticsService>();
        _gameSessionRepositoryMock = new Mock<IGameSessionRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _playerService = new PlayerService(
            _playerRepositoryMock.Object,
            _imageServiceMock.Object,
            _playerStatisticsDomainServiceMock.Object,
            _gameSessionRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _playerRepositoryMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _playerStatisticsDomainServiceMock.VerifyNoOtherCalls();
        _gameSessionRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    #region GetList Tests

    [Fact]
    public async Task GetList_ShouldReturnAllPlayers_WhenPlayersExist()
    {
        // Arrange
        var players = new List<Player>
        {
            new Player("John") { Id = 1 },
            new Player("Jane") { Id = 2 },
            new Player("Bob") { Id = 3 }
        };

        _playerRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(players);

        // Act
        var result = await _playerService.GetList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.Name == "John");
        result.Should().Contain(p => p.Name == "Jane");
        result.Should().Contain(p => p.Name == "Bob");

        _playerRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetList_ShouldReturnEmptyList_WhenNoPlayersExist()
    {
        // Arrange
        _playerRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Player>());

        // Act
        var result = await _playerService.GetList();

        // Assert
        result.Should().BeEmpty();

        _playerRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldCreatePlayer_AndSaveChanges()
    {
        // Arrange
        var player = new Player("New Player", "player.png");

        _playerRepositoryMock
            .Setup(x => x.CreateAsync(player))
            .ReturnsAsync(player);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _playerService.Create(player);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Player");
        result.Image.Should().Be("player.png");

        _playerRepositoryMock.Verify(x => x.CreateAsync(player), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ShouldReturnPlayer_WhenPlayerExists()
    {
        // Arrange
        var playerId = 1;
        var player = new Player("John") { Id = playerId };

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync(player);

        // Act
        var result = await _playerService.Get(playerId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(playerId);
        result.Name.Should().Be("John");

        _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenPlayerDoesNotExist()
    {
        // Arrange
        var playerId = 999;

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync((Player?)null);

        // Act
        var result = await _playerService.Get(playerId);

        // Assert
        result.Should().BeNull();

        _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdatePlayer_WhenPlayerExists()
    {
        // Arrange
        var playerId = 1;
        var existingPlayer = new Player("Old Name", "old.png") { Id = playerId };
        var command = new UpdatePlayerCommand
        {
            Id = playerId,
            Name = "New Name",
            Image = "new.png"
        };

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync(existingPlayer);

        _playerRepositoryMock
            .Setup(x => x.UpdateAsync(existingPlayer))
            .ReturnsAsync(existingPlayer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _playerService.Update(command);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.Image.Should().Be("new.png");

        _imageServiceMock.Verify(x => x.DeleteImage("old.png"), Times.Once);
        _playerRepositoryMock.Verify(x => x.UpdateAsync(existingPlayer), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Update_ShouldReturnNull_WhenPlayerDoesNotExist()
    {
        // Arrange
        var command = new UpdatePlayerCommand
        {
            Id = 999,
            Name = "New Name"
        };

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync((Player?)null);

        // Act
        var result = await _playerService.Update(command);

        // Assert
        result.Should().BeNull();

        _playerRepositoryMock.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldNotDeleteImage_WhenImageUnchanged()
    {
        // Arrange
        var playerId = 1;
        var existingImage = "same.png";
        var existingPlayer = new Player("Old Name", existingImage) { Id = playerId };
        var command = new UpdatePlayerCommand
        {
            Id = playerId,
            Name = "New Name",
            Image = existingImage // Same image
        };

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync(existingPlayer);

        _playerRepositoryMock
            .Setup(x => x.UpdateAsync(existingPlayer))
            .ReturnsAsync(existingPlayer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _playerService.Update(command);

        // Assert
        result.Should().NotBeNull();
        _imageServiceMock.Verify(x => x.DeleteImage(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Update_ShouldOnlyUpdateName_WhenNameChangedAndImageSame()
    {
        // Arrange
        var playerId = 1;
        var existingPlayer = new Player("Old Name", "same.png") { Id = playerId };
        var command = new UpdatePlayerCommand
        {
            Id = playerId,
            Name = "Updated Name",
            Image = "same.png"
        };

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync(existingPlayer);

        _playerRepositoryMock
            .Setup(x => x.UpdateAsync(existingPlayer))
            .ReturnsAsync(existingPlayer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _playerService.Update(command);

        // Assert
        result!.Name.Should().Be("Updated Name");
        result.Image.Should().Be("same.png");
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ShouldReturnPlayerCount()
    {
        // Arrange
        _playerRepositoryMock
            .Setup(x => x.CountAsync())
            .ReturnsAsync(15);

        // Act
        var result = await _playerService.CountAsync();

        // Assert
        result.Should().Be(15);

        _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetSessions Tests

    [Fact]
    public async Task GetSessions_ShouldReturnPlayerSessions()
    {
        // Arrange
        var playerId = 1;
        var count = 10;
        var sessions = new List<Session>
        {
            new Session(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddHours(2), "Session 1"),
            new Session(2, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2).AddHours(3), "Session 2")
        };

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(playerId, count))
            .ReturnsAsync(sessions);

        // Act
        var result = await _playerService.GetSessions(playerId, count);

        // Assert
        result.Should().HaveCount(2);

        _gameSessionRepositoryMock.Verify(x => x.GetSessionsByPlayerId(playerId, count), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSessions_ShouldPassNullCount_WhenCountNotSpecified()
    {
        // Arrange
        var playerId = 1;

        _gameSessionRepositoryMock
            .Setup(x => x.GetSessionsByPlayerId(playerId, null))
            .ReturnsAsync(new List<Session>());

        // Act
        var result = await _playerService.GetSessions(playerId, null);

        // Assert
        _gameSessionRepositoryMock.Verify(x => x.GetSessionsByPlayerId(playerId, null), Times.Once);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeletePlayerAndSessions_WhenPlayerExists()
    {
        // Arrange
        var playerId = 1;
        var player = new Player("John", "john.png") { Id = playerId };
        var sessions = new List<Session>
        {
            new Session(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, "Session 1") { Id = 1 },
            new Session(2, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), "Session 2") { Id = 2 }
        };

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync(player);

        _sessionRepositoryMock
            .Setup(x => x.GetByPlayer(playerId, null))
            .ReturnsAsync(sessions);

        _sessionRepositoryMock
            .Setup(x => x.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        _playerRepositoryMock
            .Setup(x => x.DeleteAsync(playerId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _playerService.Delete(playerId);

        // Assert
        _sessionRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Once);
        _sessionRepositoryMock.Verify(x => x.DeleteAsync(2), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("john.png"), Times.Once);
        _playerRepositoryMock.Verify(x => x.DeleteAsync(playerId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldDoNothing_WhenPlayerDoesNotExist()
    {
        // Arrange
        var playerId = 999;

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync((Player?)null);

        // Act
        await _playerService.Delete(playerId);

        // Assert
        _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldDeletePlayer_WhenNoSessions()
    {
        // Arrange
        var playerId = 1;
        var player = new Player("John") { Id = playerId };

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync(player);

        _sessionRepositoryMock
            .Setup(x => x.GetByPlayer(playerId, null))
            .ReturnsAsync(new List<Session>());

        _playerRepositoryMock
            .Setup(x => x.DeleteAsync(playerId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _playerService.Delete(playerId);

        // Assert
        _sessionRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
        _playerRepositoryMock.Verify(x => x.DeleteAsync(playerId), Times.Once);
    }

    #endregion

    #region GetStats Tests

    [Fact]
    public async Task GetStats_ShouldReturnPlayerStatistics()
    {
        // Arrange
        var playerId = 1;
        var stats = new PlayerStatistics
        {
            PlayCount = 50,
            WinCount = 25,
            TotalPlayedTime = 3000.0,
            DistinctGameCount = 10,
            MostPlayedGames = new List<MostPlayedGame>()
        };

        _playerStatisticsDomainServiceMock
            .Setup(x => x.CalculateStatisticsAsync(playerId))
            .ReturnsAsync(stats);

        // Act
        var result = await _playerService.GetStats(playerId);

        // Assert
        result.PlayCount.Should().Be(50);
        result.WinCount.Should().Be(25);
        result.TotalPlayedTime.Should().Be(3000.0);
        result.DistinctGameCount.Should().Be(10);

        _playerStatisticsDomainServiceMock.Verify(x => x.CalculateStatisticsAsync(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetTotalPlayCount Tests

    [Fact]
    public async Task GetTotalPlayCount_ShouldReturnPlayCount()
    {
        // Arrange
        var playerId = 1;
        var expectedCount = 42;

        _playerRepositoryMock
            .Setup(x => x.GetTotalPlayCount(playerId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _playerService.GetTotalPlayCount(playerId);

        // Assert
        result.Should().Be(42);

        _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
