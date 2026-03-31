using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights;
using BoardGameTracker.Core.GameNights.Interfaces;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class GameNightServiceTests
{
    private readonly Mock<IGameNightRepository> _gameNightRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<ILogger<GameNightService>> _loggerMock;
    private readonly GameNightService _gameNightService;

    public GameNightServiceTests()
    {
        _gameNightRepositoryMock = new Mock<IGameNightRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _loggerMock = new Mock<ILogger<GameNightService>>();

        _gameNightService = new GameNightService(
            _gameNightRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _gameRepositoryMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
    }

    #region GetGameNights Tests

    [Fact]
    public async Task GetGameNights_ShouldReturnAllGameNights_WhenGameNightsExist()
    {
        var gameNights = new List<GameNight>
        {
            GameNight.Create("Night 1", "Notes 1", DateTime.UtcNow.AddDays(1), 1, 1),
            GameNight.Create("Night 2", "Notes 2", DateTime.UtcNow.AddDays(2), 2, 1),
        };

        _gameNightRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(gameNights);

        var result = await _gameNightService.GetGameNights();

        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Title == "Night 1");
        result.Should().Contain(g => g.Title == "Night 2");

        _gameNightRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameNights_ShouldReturnEmptyList_WhenNoGameNightsExist()
    {
        _gameNightRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([]);

        var result = await _gameNightService.GetGameNights();

        result.Should().BeEmpty();

        _gameNightRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ShouldReturnGameNight_WhenGameNightExists()
    {
        var gameNightId = 1;
        var gameNight = GameNight.Create("Night 1", "Notes", DateTime.UtcNow.AddDays(1), 1, 1);

        _gameNightRepositoryMock
            .Setup(x => x.GetByIdAsync(gameNightId))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetById(gameNightId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Night 1");

        _gameNightRepositoryMock.Verify(x => x.GetByIdAsync(gameNightId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenGameNightDoesNotExist()
    {
        var gameNightId = 999;

        _gameNightRepositoryMock
            .Setup(x => x.GetByIdAsync(gameNightId))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.GetById(gameNightId);

        result.Should().BeNull();

        _gameNightRepositoryMock.Verify(x => x.GetByIdAsync(gameNightId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldAddHostAsAccepted_WhenHostIsNotInInvitedList()
    {
        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = "Some notes",
            StartDate = DateTime.UtcNow.AddDays(7),
            HostId = 10,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [1, 2, 3]
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([]);

        _gameNightRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<GameNight>()))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Create(command);

        result.Should().NotBeNull();
        result.InvitedPlayers.Should().HaveCount(4);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 10 && p.State == GameNightRsvpState.Accepted);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 1 && p.State == GameNightRsvpState.Pending);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 2 && p.State == GameNightRsvpState.Pending);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 3 && p.State == GameNightRsvpState.Pending);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<GameNight>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldUpdateHostStateToAccepted_WhenHostIsInInvitedList()
    {
        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = "Some notes",
            StartDate = DateTime.UtcNow.AddDays(7),
            HostId = 2,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [1, 2, 3]
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([]);

        _gameNightRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<GameNight>()))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Create(command);

        result.Should().NotBeNull();
        result.InvitedPlayers.Should().HaveCount(3);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 2 && p.State == GameNightRsvpState.Accepted);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 1 && p.State == GameNightRsvpState.Pending);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 3 && p.State == GameNightRsvpState.Pending);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<GameNight>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldCallRepositoryCreateAsyncAndSaveChanges()
    {
        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow.AddDays(1),
            HostId = 1,
            LocationId = 2,
            SuggestedGameIds = [],
            InvitedPlayerIds = []
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([]);

        _gameNightRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<GameNight>()))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _gameNightService.Create(command);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<GameNight>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldFetchSuggestedGames_ByProvidedIds()
    {
        var suggestedGameIds = new List<int> { 5, 10, 15 };
        var games = new List<Game>
        {
            new Game("Chess") { Id = 5 },
            new Game("Catan") { Id = 10 },
            new Game("Risk") { Id = 15 },
        };

        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow.AddDays(1),
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = suggestedGameIds,
            InvitedPlayerIds = []
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(suggestedGameIds))
            .ReturnsAsync(games);

        _gameNightRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<GameNight>()))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Create(command);

        result.SuggestedGames.Should().HaveCount(3);
        result.SuggestedGames.Should().Contain(g => g.Id == 5);
        result.SuggestedGames.Should().Contain(g => g.Id == 10);
        result.SuggestedGames.Should().Contain(g => g.Id == 15);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(suggestedGameIds), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<GameNight>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateGameNightProperties_WhenGameNightExists()
    {
        var existingGameNight = GameNight.Create("Old Title", "Old Notes", DateTime.UtcNow, 1, 1);
        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "New Title",
            Notes = "New Notes",
            StartDate = DateTime.UtcNow.AddDays(5),
            HostId = 2,
            LocationId = 3,
            SuggestedGameIds = [7],
            InvitedPlayerIds = []
        };

        var games = new List<Game> { new Game("Catan") { Id = 7 } };

        _gameNightRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync(existingGameNight);

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync(games);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Title");
        result.Notes.Should().Be("New Notes");
        result.HostId.Should().Be(2);
        result.LocationId.Should().Be(3);
        result.SuggestedGames.Should().HaveCount(1);
        result.SuggestedGames.Should().Contain(g => g.Id == 7);

        _gameNightRepositoryMock.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldThrowEntityNotFoundException_WhenGameNightNotFound()
    {
        var command = new UpdateGameNightCommand
        {
            Id = 999,
            Title = "Title",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow,
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = []
        };

        _gameNightRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync((GameNight?)null);

        var action = async () => await _gameNightService.Update(command);

        await action.Should().ThrowAsync<EntityNotFoundException>();

        _gameNightRepositoryMock.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldAddNewPlayersAndRemoveOldPlayers_WhenInvitedPlayerIdsChange()
    {
        var existingRsvp1 = GameNightRsvp.Create(1, GameNightRsvpState.Accepted);
        var existingRsvp2 = GameNightRsvp.Create(2, GameNightRsvpState.Pending);
        var existingGameNight = GameNight.Create("Title", "Notes", DateTime.UtcNow, 1, 1);
        existingGameNight.SetInvitedPlayers([existingRsvp1, existingRsvp2]);

        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Title",
            Notes = "Notes",
            StartDate = DateTime.UtcNow,
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [1, 3]
        };

        _gameNightRepositoryMock
            .Setup(x => x.GetByIdAsync(command.Id))
            .ReturnsAsync(existingGameNight);

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([]);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.InvitedPlayers.Should().HaveCount(2);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 1);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 3);
        result.InvitedPlayers.Should().NotContain(p => p.PlayerId == 2);

        _gameNightRepositoryMock.Verify(x => x.GetByIdAsync(command.Id), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldCallDeleteAsyncAndSaveChanges()
    {
        var gameNightId = 1;

        _gameNightRepositoryMock
            .Setup(x => x.DeleteAsync(gameNightId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        await _gameNightService.Delete(gameNightId);

        _gameNightRepositoryMock.Verify(x => x.DeleteAsync(gameNightId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateRsvp Tests

    [Fact]
    public async Task UpdateRsvp_ShouldFetchRsvpById_WhenIdIsProvided()
    {
        var rsvp = GameNightRsvp.Create(1, GameNightRsvpState.Pending);
        var command = new UpdateRsvpCommand
        {
            Id = 5,
            State = GameNightRsvpState.Accepted
        };

        _gameNightRepositoryMock
            .Setup(x => x.GetRsvpByIdAsync(command.Id.Value))
            .ReturnsAsync(rsvp);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.Should().NotBeNull();
        result.State.Should().Be(GameNightRsvpState.Accepted);

        _gameNightRepositoryMock.Verify(x => x.GetRsvpByIdAsync(command.Id.Value), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldFetchRsvpByPlayerAndGame_WhenIdIsNull()
    {
        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Pending);
        var command = new UpdateRsvpCommand
        {
            Id = null,
            GameNightId = 10,
            PlayerId = 3,
            State = GameNightRsvpState.Declined
        };

        _gameNightRepositoryMock
            .Setup(x => x.GetRsvpByPlayerAndGameAsync(command.PlayerId.Value, command.GameNightId.Value))
            .ReturnsAsync(rsvp);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.Should().NotBeNull();
        result.State.Should().Be(GameNightRsvpState.Declined);

        _gameNightRepositoryMock.Verify(x => x.GetRsvpByPlayerAndGameAsync(command.PlayerId.Value, command.GameNightId.Value), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldThrow_WhenRsvpNotFound()
    {
        var command = new UpdateRsvpCommand
        {
            Id = 999,
            State = GameNightRsvpState.Accepted
        };

        _gameNightRepositoryMock
            .Setup(x => x.GetRsvpByIdAsync(command.Id.Value))
            .ReturnsAsync((GameNightRsvp?)null);

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<Exception>();

        _gameNightRepositoryMock.Verify(x => x.GetRsvpByIdAsync(command.Id.Value), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldUpdateState_WhenRsvpIsFound()
    {
        var rsvp = GameNightRsvp.Create(1, GameNightRsvpState.Pending);
        var command = new UpdateRsvpCommand
        {
            Id = 7,
            State = GameNightRsvpState.Declined
        };

        _gameNightRepositoryMock
            .Setup(x => x.GetRsvpByIdAsync(command.Id.Value))
            .ReturnsAsync(rsvp);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.State.Should().Be(GameNightRsvpState.Declined);

        _gameNightRepositoryMock.Verify(x => x.GetRsvpByIdAsync(command.Id.Value), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CountFutureGameNights Tests

    [Fact]
    public async Task CountFutureGameNights_ShouldReturnCountFromRepository()
    {
        _gameNightRepositoryMock
            .Setup(x => x.GetFutureGameNightsCountAsync())
            .ReturnsAsync(7);

        var result = await _gameNightService.CountFutureGameNights();

        result.Should().Be(7);

        _gameNightRepositoryMock.Verify(x => x.GetFutureGameNightsCountAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetByLinkId Tests

    [Fact]
    public async Task GetByLinkId_ShouldReturnGameNight_WhenFound()
    {
        var linkId = Guid.NewGuid();
        var gameNight = GameNight.Create("Night", "Notes", DateTime.UtcNow.AddDays(1), 1, 1);

        _gameNightRepositoryMock
            .Setup(x => x.GetGameNightByLinkId(linkId))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetByLinkId(linkId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Night");

        _gameNightRepositoryMock.Verify(x => x.GetGameNightByLinkId(linkId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByLinkId_ShouldReturnNull_WhenNotFound()
    {
        var linkId = Guid.NewGuid();

        _gameNightRepositoryMock
            .Setup(x => x.GetGameNightByLinkId(linkId))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.GetByLinkId(linkId);

        result.Should().BeNull();

        _gameNightRepositoryMock.Verify(x => x.GetGameNightByLinkId(linkId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
