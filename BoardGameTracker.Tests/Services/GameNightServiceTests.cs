using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using BoardGameTracker.Core.GameNights;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class GameNightServiceTests
{
    private readonly Mock<IRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IReadRepository<GameNightRsvp>> _rsvpRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IPublicUrlBuilder> _publicUrlBuilderMock;
    private readonly Mock<ILogger<GameNightService>> _loggerMock;
    private readonly GameNightService _gameNightService;

    public GameNightServiceTests()
    {
        _gameNightRepositoryMock = new Mock<IRepository<GameNight>>();
        _rsvpRepositoryMock = new Mock<IReadRepository<GameNightRsvp>>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _publicUrlBuilderMock = new Mock<IPublicUrlBuilder>();
        _loggerMock = new Mock<ILogger<GameNightService>>();

        _gameNightService = new GameNightService(
            _gameNightRepositoryMock.Object,
            _rsvpRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _gameRepositoryMock.Object,
            _emailServiceMock.Object,
            _publicUrlBuilderMock.Object,
            _dateTimeProviderMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _rsvpRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
        _emailServiceMock.VerifyNoOtherCalls();
        _publicUrlBuilderMock.VerifyNoOtherCalls();
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
            .Setup(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNights);

        var result = await _gameNightService.GetGameNights();

        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Title == "Night 1");
        result.Should().Contain(g => g.Title == "Night 2");

        _gameNightRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameNights_ShouldReturnEmptyList_WhenNoGameNightsExist()
    {
        _gameNightRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _gameNightService.GetGameNights();

        result.Should().BeEmpty();

        _gameNightRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<GameNight>>(s => s is GameNightsOverviewSpec), It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetById(gameNightId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Night 1");

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenGameNightDoesNotExist()
    {
        var gameNightId = 999;

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.GetById(gameNightId);

        result.Should().BeNull();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task Create_ShouldTreatNullCollections_AsEmpty()
    {
        var command = new CreateGameNightCommand
        {
            Title = "Game Night",
            Notes = string.Empty,
            StartDate = DateTime.UtcNow.AddDays(1),
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = null!,
            InvitedPlayerIds = null!
        };

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(It.Is<List<int>>(ids => ids.Count == 0)))
            .ReturnsAsync([]);

        _gameNightRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<GameNight>()))
            .ReturnsAsync((GameNight g) => g);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Create(command);

        result.SuggestedGames.Should().BeEmpty();
        result.InvitedPlayers.Should().ContainSingle(p => p.PlayerId == 1 && p.State == GameNightRsvpState.Accepted);

        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(It.Is<List<int>>(ids => ids.Count == 0)), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
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

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNight?)null);

        var action = async () => await _gameNightService.Update(command);

        await action.Should().ThrowAsync<EntityNotFoundException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
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

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetByIdsAsync(command.SuggestedGameIds), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldKeepHostAsAccepted_WhenHostNotInInvitedPlayerIds()
    {
        var hostRsvp = GameNightRsvp.Create(1, GameNightRsvpState.Accepted);
        var guestRsvp = GameNightRsvp.Create(2, GameNightRsvpState.Pending);
        var existingGameNight = GameNight.Create("Title", "Notes", DateTime.UtcNow, 1, 1);
        existingGameNight.SetInvitedPlayers([hostRsvp, guestRsvp]);

        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Title",
            Notes = "Notes",
            StartDate = DateTime.UtcNow,
            HostId = 1,
            LocationId = 1,
            SuggestedGameIds = [],
            InvitedPlayerIds = [2, 3]
        };

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGameNight);

        _gameRepositoryMock
            .Setup(x => x.GetByIdsAsync(command.SuggestedGameIds))
            .ReturnsAsync([]);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.Update(command);

        result.InvitedPlayers.Should().HaveCount(3);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 1 && p.State == GameNightRsvpState.Accepted);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 2);
        result.InvitedPlayers.Should().Contain(p => p.PlayerId == 3);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
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

    #region SendInvites Tests

    private static GameNightRsvp RsvpWithPlayer(int playerId, GameNightRsvpState state, Player player)
    {
        var rsvp = GameNightRsvp.Create(playerId, state);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.Player))!.SetValue(rsvp, player);
        return rsvp;
    }

    private static GameNightRsvp RsvpWithGameNight(
        int playerId,
        GameNightRsvpState state,
        Player player,
        int hostId,
        Player host)
    {
        var rsvp = RsvpWithPlayer(playerId, state, player);
        var gameNight = GameNight.Create("Games night", "", new DateTime(2026, 8, 13, 11, 47, 0, DateTimeKind.Utc), hostId, 1);
        typeof(GameNight).GetProperty(nameof(GameNight.Host))!.SetValue(gameNight, host);
        typeof(GameNightRsvp).GetProperty(nameof(GameNightRsvp.GameNight))!.SetValue(rsvp, gameNight);
        return rsvp;
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldThrow_WhenGameNightNotFound()
    {
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync((GameNight?)null);

        var act = () => _gameNightService.SendInvitesAsync(99);

        await act.Should().ThrowAsync<EntityNotFoundException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldThrow_WhenEmailNotConfigured()
    {
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(false);

        var act = () => _gameNightService.SendInvitesAsync(1);

        await act.Should().ThrowAsync<DomainException>();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldSendToPlayersWithEmailAndSkipOthers()
    {
        var withEmail = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice", null, "alice@test.com"));
        var noEmail = RsvpWithPlayer(2, GameNightRsvpState.Pending, new Player("Bob"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers([withEmail, noEmail]);

        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync("http://x/rsvp");
        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(1);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        _emailServiceMock.Verify(x => x.SendAsync("alice@test.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldOnlyEmailPendingPlayers()
    {
        var pending = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice", null, "alice@test.com"));
        var accepted = RsvpWithPlayer(2, GameNightRsvpState.Accepted, new Player("Host", null, "host@test.com"));
        var declined = RsvpWithPlayer(3, GameNightRsvpState.Declined, new Player("Bob", null, "bob@test.com"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 2, 1);
        gameNight.SetInvitedPlayers([pending, accepted, declined]);

        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync("http://x/rsvp");
        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(1);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        _emailServiceMock.Verify(x => x.SendAsync("alice@test.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendAsync("host@test.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(x => x.SendAsync("bob@test.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendInvitesAsync_ShouldNotCountAsSent_WhenSendThrows()
    {
        var withEmail = RsvpWithPlayer(1, GameNightRsvpState.Pending, new Player("Alice", null, "alice@test.com"));
        var gameNight = GameNight.Create("Night", "", DateTime.UtcNow, 1, 1);
        gameNight.SetInvitedPlayers([withEmail]);

        _gameNightRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>())).ReturnsAsync(gameNight);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _publicUrlBuilderMock.Setup(x => x.BuildRsvpUrlAsync(gameNight.LinkId)).ReturnsAsync("http://x/rsvp");
        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("smtp down"));

        var result = await _gameNightService.SendInvitesAsync(1);

        result.Sent.Should().Be(0);

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByIdWithDetailsSpec), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _publicUrlBuilderMock.Verify(x => x.BuildRsvpUrlAsync(gameNight.LinkId), Times.Once);
        _emailServiceMock.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateRsvp Tests

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

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByPlayerAndGameNightSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.Should().NotBeNull();
        result.State.Should().Be(GameNightRsvpState.Declined);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByPlayerAndGameNightSpec), It.IsAny<CancellationToken>()), Times.Once);
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

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNightRsvp?)null);

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<ArgumentNullException>();

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldThrowArgumentNullException_WhenIdAndGameNightIdAreNull()
    {
        var command = new UpdateRsvpCommand
        {
            Id = null,
            GameNightId = null,
            PlayerId = 3,
            State = GameNightRsvpState.Accepted
        };

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<ArgumentNullException>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldThrowArgumentNullException_WhenIdAndPlayerIdAreNull()
    {
        var command = new UpdateRsvpCommand
        {
            Id = null,
            GameNightId = 10,
            PlayerId = null,
            State = GameNightRsvpState.Accepted
        };

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<ArgumentNullException>();

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldThrow_WhenRsvpNotFoundByPlayerAndGameNight()
    {
        var command = new UpdateRsvpCommand
        {
            Id = null,
            GameNightId = 10,
            PlayerId = 3,
            State = GameNightRsvpState.Accepted
        };

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByPlayerAndGameNightSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNightRsvp?)null);

        var action = async () => await _gameNightService.UpdateRsvp(command);

        await action.Should().ThrowAsync<ArgumentNullException>();

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByPlayerAndGameNightSpec), It.IsAny<CancellationToken>()), Times.Once);
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

        _rsvpRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rsvp);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var result = await _gameNightService.UpdateRsvp(command);

        result.State.Should().Be(GameNightRsvpState.Declined);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GameNightRsvpState.Accepted, "will come to")]
    [InlineData(GameNightRsvpState.Declined, "will not come to")]
    [InlineData(GameNightRsvpState.Pending, "is not sure about")]
    public async Task UpdateRsvp_ShouldWordEmailByState(GameNightRsvpState state, string expected)
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = state };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _gameNightService.UpdateRsvp(command);

        _emailServiceMock.Verify(
            x => x.SendAsync(
                "host@test.com",
                It.Is<string>(s => s.Contains("Games night")),
                It.Is<string>(b => b.Contains("Kathleen") && b.Contains(expected)),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotEmail_WhenHostRespondsToOwnGameNight()
    {
        var rsvp = RsvpWithGameNight(1, GameNightRsvpState.Pending, new Player("Mikhael", null, "host@test.com"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _gameNightService.UpdateRsvp(command);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotEmail_WhenHostHasNoEmail()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);

        await _gameNightService.UpdateRsvp(command);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldNotEmail_WhenEmailIsNotConfigured()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(false);

        await _gameNightService.UpdateRsvp(command);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldStillSucceed_WhenHostEmailThrows()
    {
        var rsvp = RsvpWithGameNight(2, GameNightRsvpState.Pending, new Player("Kathleen"),
            hostId: 1, host: new Player("Mikhael", null, "host@test.com"));
        var command = new UpdateRsvpCommand { Id = 7, State = GameNightRsvpState.Accepted };

        _rsvpRepositoryMock.Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>())).ReturnsAsync(rsvp);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.SetupGet(x => x.IsConfigured).Returns(true);
        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("smtp down"));

        var result = await _gameNightService.UpdateRsvp(command);

        result.State.Should().Be(GameNightRsvpState.Accepted);

        _rsvpRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNightRsvp>>(s => s is RsvpByIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.VerifyGet(x => x.IsConfigured, Times.Once);
        _emailServiceMock.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CountFutureGameNights Tests

    [Fact]
    public async Task CountFutureGameNights_ShouldReturnCountFromRepository()
    {
        _gameNightRepositoryMock
            .Setup(x => x.CountAsync(It.Is<ISpecification<GameNight>>(s => s is FutureGameNightsSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await _gameNightService.CountFutureGameNights();

        result.Should().Be(7);

        _gameNightRepositoryMock.Verify(x => x.CountAsync(It.Is<ISpecification<GameNight>>(s => s is FutureGameNightsSpec), It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameNight);

        var result = await _gameNightService.GetByLinkId(linkId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Night");

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByLinkId_ShouldReturnNull_WhenNotFound()
    {
        var linkId = Guid.NewGuid();

        _gameNightRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameNight?)null);

        var result = await _gameNightService.GetByLinkId(linkId);

        result.Should().BeNull();

        _gameNightRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<ISingleResultSpecification<GameNight>>(s => s is GameNightByLinkIdSpec), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
