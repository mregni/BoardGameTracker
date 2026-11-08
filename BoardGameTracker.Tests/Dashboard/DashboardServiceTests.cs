using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Charts;
using BoardGameTracker.Core.Auth.Specifications;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Dashboard;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Dashboard;

public class DashboardServiceTests
{
    private const string UserId = "user-1";
    private static readonly DateTime LocalNow = new(2026, 10, 16, 20, 30, 0, DateTimeKind.Unspecified);
    private static readonly DateTime MonthStartUtc = new(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UtcNow = new(2026, 10, 16, 18, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime YearStartUtc = new(2025, 12, 31, 22, 0, 0, DateTimeKind.Utc);
    private static readonly CancellationToken Token = new CancellationTokenSource().Token;

    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameStatisticsRepository> _gameStatisticsRepositoryMock;
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IReadRepository<ApplicationUser>> _userRepositoryMock;
    private readonly Mock<IShameService> _shameServiceMock;
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameStatisticsRepositoryMock = new Mock<IGameStatisticsRepository>();
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _userRepositoryMock = new Mock<IReadRepository<ApplicationUser>>();
        _shameServiceMock = new Mock<IShameService>();
        _configRepositoryMock = new Mock<IConfigRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.Now).Returns(LocalNow);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(UtcNow);
        _dateTimeProviderMock.Setup(x => x.ConvertToUtc(new DateTime(2026, 10, 1))).Returns(MonthStartUtc);
        _dateTimeProviderMock.Setup(x => x.ConvertToUtc(new DateTime(2026, 1, 1))).Returns(YearStartUtc);

        _sut = new DashboardService(
            _gameRepositoryMock.Object,
            _gameStatisticsRepositoryMock.Object,
            _playerRepositoryMock.Object,
            _sessionRepositoryMock.Object,
            _gameNightRepositoryMock.Object,
            _userRepositoryMock.Object,
            _shameServiceMock.Object,
            _configRepositoryMock.Object,
            _dateTimeProviderMock.Object,
            NullLogger<DashboardService>.Instance);

        SetupDefaultMocks();
    }

    private void VerifyNoOtherCalls()
    {
        _gameRepositoryMock.VerifyNoOtherCalls();
        _gameStatisticsRepositoryMock.VerifyNoOtherCalls();
        _playerRepositoryMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _userRepositoryMock.VerifyNoOtherCalls();
        _shameServiceMock.VerifyNoOtherCalls();
        _configRepositoryMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
    }

    private static SessionsStartedSinceSpec StartedSince(DateTime since) =>
        It.Is<SessionsStartedSinceSpec>(s => s.IsSatisfiedBy(SessionStarting(since)) && !s.IsSatisfiedBy(SessionStarting(since.AddMinutes(-1))));

    private static NextGameNightSpec NextNight() => It.Is<NextGameNightSpec>(s => StartsAfterThePlayWindow(s));

    private static FutureGameNightsSpec FutureNights() => It.Is<FutureGameNightsSpec>(s => StartsAfterThePlayWindow(s));

    private static LinkedPlayerByUserIdSpec LinkedPlayer() =>
        It.Is<LinkedPlayerByUserIdSpec>(s => s.IsSatisfiedBy(LinkedUser(UserId)) && !s.IsSatisfiedBy(LinkedUser("user-2")));

    private static Session SessionStarting(DateTime start) => new SessionBuilder().Between(start, start.AddHours(1)).Build();

    private static bool StartsAfterThePlayWindow(ISpecification<GameNight> spec)
    {
        var playingSince = UtcNow - GameNight.PlayWindow;
        return spec.IsSatisfiedBy(GameNight.Create("Open", string.Empty, playingSince.AddMinutes(1), 1, 1))
            && !spec.IsSatisfiedBy(GameNight.Create("Over", string.Empty, playingSince, 1, 1));
    }

    private static ApplicationUser LinkedUser(string userId)
    {
        var user = new ApplicationUser("name", null) { Id = userId };
        typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.PlayerId))!.SetValue(user, 7);
        typeof(ApplicationUser).GetProperty(nameof(ApplicationUser.Player))!.SetValue(user, new Player("Linked") { Id = 7 });
        return user;
    }

    private void VerifyGetStatisticsCalls(
        bool userLookedUp = false,
        bool playerLinked = false,
        bool gameNightsEnabled = true,
        bool nightPlanned = false,
        bool shelfOfShameEnabled = false)
    {
        _dateTimeProviderMock.VerifyGet(x => x.Now, Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(new DateTime(2026, 10, 1)), Times.Once);
        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(new DateTime(2026, 1, 1)), Times.Once);
        _gameRepositoryMock.Verify(x => x.CountAsync(Token), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetTotalPlayTime(Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetMeanPlayTime(Token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetTotalPayedAsync(Token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.CountPricedOwnedGamesAsync(Token), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalExpansionCount(Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(StartedSince(MonthStartUtc), Token), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountActivePlayersSince(MonthStartUtc, Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetRecentSessions(5, Token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetGameStateCounts(Token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostPlayedGames(4, Token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostPlayedGamesSince(4, YearStartUtc, Token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostPlayedGamesSince(4, MonthStartUtc, Token), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetWinTotals(Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.AnyAsync(It.IsAny<WonCoopSessionsSpec>(), Token), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetRecentlyAddedGames(3, Token), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false), Times.Once);
        if (shelfOfShameEnabled)
        {
            _shameServiceMock.Verify(x => x.GetShameStatistics(null), Times.Once);
        }
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true), Times.Once);

        if (userLookedUp)
        {
            _userRepositoryMock.Verify(x => x.FirstOrDefaultAsync(LinkedPlayer(), Token), Times.Once);
        }

        if (playerLinked)
        {
            _playerRepositoryMock.Verify(x => x.GetPlayCountsSince(7, MonthStartUtc, Token), Times.Once);
        }

        if (gameNightsEnabled)
        {
            _gameNightRepositoryMock.Verify(x => x.FirstOrDefaultAsync(NextNight(), Token), Times.Once);
        }

        if (nightPlanned)
        {
            _gameNightRepositoryMock.Verify(x => x.CountAsync(FutureNights(), Token), Times.Once);
        }
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnCorrectScalarValues()
    {
        _gameRepositoryMock.Setup(x => x.CountAsync(Token)).ReturnsAsync(25);
        _playerRepositoryMock.Setup(x => x.CountAsync(Token)).ReturnsAsync(10);
        _sessionRepositoryMock.Setup(x => x.CountAsync(Token)).ReturnsAsync(100);
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime(Token)).ReturnsAsync(5000.0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPayedAsync(Token)).ReturnsAsync(887.5m);
        _gameStatisticsRepositoryMock.Setup(x => x.CountPricedOwnedGamesAsync(Token)).ReturnsAsync(25);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount(Token)).ReturnsAsync(15);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime(Token)).ReturnsAsync(50.0);
        _sessionRepositoryMock.Setup(x => x.CountAsync(StartedSince(MonthStartUtc), Token)).ReturnsAsync(14);
        _playerRepositoryMock.Setup(x => x.CountActivePlayersSince(MonthStartUtc, Token)).ReturnsAsync(5);

        var result = await _sut.GetStatistics(null, Token);

        result.TotalGames.Should().Be(25);
        result.ActivePlayers.Should().Be(10);
        result.SessionsPlayed.Should().Be(100);
        result.TotalPlayedTime.Should().Be(5000.0);
        result.TotalCollectionValue.Should().Be(887.5m);
        result.PricedGameCount.Should().Be(25);
        result.ExpansionsOwned.Should().Be(15);
        result.AvgSessionTime.Should().Be(50.0);
        result.SessionsThisMonth.Should().Be(14);
        result.ActivePlayersThisMonth.Should().Be(5);

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldCountTheCalendarMonthFromTheLocalFirstOfTheMonth()
    {
        SessionsStartedSinceSpec? captured = null;
        _sessionRepositoryMock
            .Setup(x => x.CountAsync(StartedSince(MonthStartUtc), Token))
            .Callback<Ardalis.Specification.ISpecification<Session>, CancellationToken>((spec, _) => captured = (SessionsStartedSinceSpec)spec)
            .ReturnsAsync(0);

        await _sut.GetStatistics(null, Token);

        var sessions = new List<Session>
        {
            new SessionBuilder().Between(MonthStartUtc.AddMinutes(-1), MonthStartUtc.AddHours(1)).Build(),
            new SessionBuilder().Between(MonthStartUtc, MonthStartUtc.AddHours(1)).Build()
        };
        captured!.Evaluate(sessions).Should().ContainSingle().Which.Start.Should().Be(MonthStartUtc);

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnNullForPrices_WhenNoGamesHavePrices()
    {
        var result = await _sut.GetStatistics(null, Token);

        result.TotalCollectionValue.Should().BeNull();
        result.PricedGameCount.Should().Be(0);

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentActivitiesWithEveryWinner()
    {
        var game = new Game("Catan") { Id = 1 };
        game.UpdateImage("catan.jpg");
        var session = new SessionBuilder()
            .ForGame(game)
            .EndingAt(DateTime.UtcNow)
            .LastingMinutes(90)
            .WithWinner(new Player("John Doe") { Id = 1 })
            .WithWinner(new Player("Jane Doe") { Id = 2 })
            .WithPlayer(new Player("Loser") { Id = 3 })
            .Build();

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5, Token)).ReturnsAsync([session]);

        var result = await _sut.GetStatistics(null, Token);

        result.RecentActivities.Should().HaveCount(1);
        var activity = result.RecentActivities[0];
        activity.GameId.Should().Be(1);
        activity.GameTitle.Should().Be("Catan");
        activity.GameImage.Should().Be("catan.jpg");
        activity.PlayerCount.Should().Be(3);
        activity.DurationInMinutes.Should().Be(90);
        activity.Winners.Select(x => (x.Id, x.Name)).Should().Equal((1, "John Doe"), (2, "Jane Doe"));

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentActivityWithoutWinners_WhenNobodyWon()
    {
        var session = new SessionBuilder()
            .ForGame(new Game("Catan") { Id = 1 })
            .EndingAt(DateTime.UtcNow)
            .Lasting(TimeSpan.FromHours(1))
            .WithPlayer(1)
            .Build();

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5, Token)).ReturnsAsync([session]);

        var result = await _sut.GetStatistics(null, Token);

        result.RecentActivities.Should().ContainSingle().Which.Winners.Should().BeEmpty();

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnAWinnerWithAnEmptyNameAndNoImage_WhenThePlayerIsNotLoaded()
    {
        var session = new SessionBuilder()
            .ForGame(new Game("Catan") { Id = 1 })
            .EndingAt(DateTime.UtcNow)
            .LastingMinutes(30)
            .WithWinner(9)
            .Build();

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5, Token)).ReturnsAsync([session]);

        var result = await _sut.GetStatistics(null, Token);

        var winner = result.RecentActivities.Should().ContainSingle().Which.Winners.Should().ContainSingle().Subject;
        winner.Id.Should().Be(9);
        winner.Name.Should().BeEmpty();
        winner.Image.Should().BeNull();

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnTheTeamResult_InsteadOfWinners_ForCoopSessions()
    {
        var session = new SessionBuilder()
            .ForGame(new Game("Pandemic") { Id = 1 })
            .EndingAt(DateTime.UtcNow)
            .LastingMinutes(45)
            .WithWinner(new Player("John Doe") { Id = 1 })
            .Build();
        session.MarkAsCoop(true, null);

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5, Token)).ReturnsAsync([session]);

        var result = await _sut.GetStatistics(null, Token);

        var activity = result.RecentActivities.Should().ContainSingle().Subject;
        activity.IsCoop.Should().BeTrue();
        activity.TeamWon.Should().BeTrue();
        activity.Winners.Should().BeEmpty();

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetStatistics_ShouldSayWhetherTopPlayerWinsIncludeCoopWins(bool hasCoopWins)
    {
        _sessionRepositoryMock.Setup(x => x.AnyAsync(It.IsAny<WonCoopSessionsSpec>(), Token)).ReturnsAsync(hasCoopWins);

        var result = await _sut.GetStatistics(null, Token);

        result.TopPlayersIncludeCoopWins.Should().Be(hasCoopWins);

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnCollectionAndOwnedCount()
    {
        var groupedGames = new List<GameStateChart>
        {
            new() { Type = GameState.Owned, GameCount = 3 },
            new() { Type = GameState.Wanted, GameCount = 2 }
        };

        _gameStatisticsRepositoryMock.Setup(x => x.GetGameStateCounts(Token)).ReturnsAsync(groupedGames);

        var result = await _sut.GetStatistics(null, Token);

        result.Collection.Should().BeSameAs(groupedGames);
        result.Collection.Should().HaveCount(2);
        result.Collection.Should().Contain(x => x.Type == GameState.Owned && x.GameCount == 3);
        result.Collection.Should().Contain(x => x.Type == GameState.Wanted && x.GameCount == 2);
        result.OwnedGames.Should().Be(3);

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnZeroOwned_WhenNoGameIsOwned()
    {
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetGameStateCounts(Token))
            .ReturnsAsync([new GameStateChart { Type = GameState.Wanted, GameCount = 2 }]);

        var result = await _sut.GetStatistics(null, Token);

        result.OwnedGames.Should().Be(0);

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnMostPlayedGamesForAllTimeThisYearAndThisMonth()
    {
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetMostPlayedGames(4, Token))
            .ReturnsAsync(new List<(int GameId, string Title, string? Image, int PlayCount)> { (1, "Catan", "catan.jpg", 20), (2, "Ticket to Ride", "ttr.jpg", 15) });
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetMostPlayedGamesSince(4, YearStartUtc, Token))
            .ReturnsAsync(new List<(int GameId, string Title, string? Image, int PlayCount)> { (2, "Ticket to Ride", "ttr.jpg", 3) });
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetMostPlayedGamesSince(4, MonthStartUtc, Token))
            .ReturnsAsync(new List<(int GameId, string Title, string? Image, int PlayCount)> { (1, "Catan", "catan.jpg", 5) });

        var result = await _sut.GetStatistics(null, Token);

        result.MostPlayedGames.Should().HaveCount(2);
        result.MostPlayedGames[0].Id.Should().Be(1);
        result.MostPlayedGames[0].Title.Should().Be("Catan");
        result.MostPlayedGames[0].Image.Should().Be("catan.jpg");
        result.MostPlayedGames[0].TotalSessions.Should().Be(20);
        result.MostPlayedGamesThisYear.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MostPlayedGameDto { Id = 2, Title = "Ticket to Ride", Image = "ttr.jpg", TotalSessions = 3 });
        result.MostPlayedGamesThisMonth.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MostPlayedGameDto { Id = 1, Title = "Catan", Image = "catan.jpg", TotalSessions = 5 });

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnTopThreeWinners()
    {
        _playerRepositoryMock
            .Setup(x => x.GetWinTotals(Token))
            .ReturnsAsync(new List<(int Id, string Name, string? Image, int PlayCount, int WinCount)> { (2, "Jane", null, 20, 12), (1, "John", "john.jpg", 25, 15) });

        var result = await _sut.GetStatistics(null, Token);

        result.TopPlayers.Should().HaveCount(2);
        result.TopPlayers[0].Id.Should().Be(1);
        result.TopPlayers[0].Name.Should().Be("John");
        result.TopPlayers[0].Image.Should().Be("john.jpg");
        result.TopPlayers[0].PlayCount.Should().Be(25);
        result.TopPlayers[0].WinCount.Should().Be(15);
        result.TopPlayers[1].Image.Should().BeNull();
        result.TopPlayers.Select(x => x.Rank).Should().Equal(1, 2);

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldRankTopPlayersByWinsThenWinRateThenSessions_SharingTheRankOnEqualStats()
    {
        _playerRepositoryMock
            .Setup(x => x.GetWinTotals(Token))
            .ReturnsAsync(new List<(int Id, string Name, string? Image, int PlayCount, int WinCount)>
            {
                (1, "Jules", null, 30, 13),
                (2, "Bram", null, 26, 13),
                (3, "Sofie", null, 20, 20),
                (4, "Anna", null, 26, 13),
                (5, "Kathleen", null, 10, 2)
            });

        var result = await _sut.GetStatistics(null, Token);

        result.TopPlayers.Select(x => x.Name).Should().Equal("Sofie", "Anna", "Bram");
        result.TopPlayers.Select(x => x.Rank).Should().Equal(1, 2, 2);

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnRecentAddedGames()
    {
        var game1 = new Game("New Game 1") { Id = 1 };
        game1.UpdateAdditionDate(UtcNow.AddDays(-1));
        game1.UpdateBuyingPrice(49.99m);

        var game2 = new Game("New Game 2") { Id = 2 };
        game2.UpdateAdditionDate(UtcNow.AddDays(-2));

        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(3, Token)).ReturnsAsync([game1, game2]);

        var result = await _sut.GetStatistics(null, Token);

        result.RecentAddedGames.Should().HaveCount(2);
        result.RecentAddedGames[0].Id.Should().Be(1);
        result.RecentAddedGames[0].Title.Should().Be("New Game 1");
        result.RecentAddedGames[0].AdditionDate.Should().Be(UtcNow.AddDays(-1));
        result.RecentAddedGames[0].Price.Should().Be(49.99m);
        result.RecentAddedGames[1].Price.Should().BeNull();

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnLinkedPlayerWithThisMonthsCounts_WhenUserIsLinked()
    {
        _userRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(LinkedPlayer(), Token))
            .ReturnsAsync(new DashboardLinkedPlayerDto { Id = 7, Name = "Mikhaël" });
        _playerRepositoryMock.Setup(x => x.GetPlayCountsSince(7, MonthStartUtc, Token)).ReturnsAsync((9, 4));

        var result = await _sut.GetStatistics(UserId, Token);

        result.LinkedPlayer.Should().BeEquivalentTo(new DashboardLinkedPlayerDto
        {
            Id = 7,
            Name = "Mikhaël",
            SessionsThisMonth = 9,
            WinsThisMonth = 4
        });

        VerifyGetStatisticsCalls(userLookedUp: true, playerLinked: true);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnNoLinkedPlayer_WhenUserHasNoPlayer()
    {
        _userRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(LinkedPlayer(), Token))
            .ReturnsAsync((DashboardLinkedPlayerDto?)null);

        var result = await _sut.GetStatistics(UserId, Token);

        result.LinkedPlayer.Should().BeNull();

        VerifyGetStatisticsCalls(userLookedUp: true);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetStatistics_ShouldSkipTheUserLookup_WhenThereIsNoUserId(string? userId)
    {
        var result = await _sut.GetStatistics(userId, Token);

        result.LinkedPlayer.Should().BeNull();

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnTheNextGameNightAndThePlannedCount()
    {
        var night = new DashboardGameNightDto { Id = 3, Title = "Strategy night", AcceptedCount = 4 };
        NextGameNightSpec? captured = null;
        _gameNightRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(NextNight(), Token))
            .Callback<Ardalis.Specification.ISpecification<GameNight, DashboardGameNightDto>, CancellationToken>((spec, _) => captured = (NextGameNightSpec)spec)
            .ReturnsAsync(night);
        _gameNightRepositoryMock
            .Setup(x => x.CountAsync(FutureNights(), Token))
            .ReturnsAsync(3);

        var result = await _sut.GetStatistics(null, Token);

        result.NextGameNight.Should().BeSameAs(night);
        result.PlannedGameNights.Should().Be(3);
        var stillPlaying = GameNight.Create("Still playing", string.Empty, UtcNow.AddHours(-2), 1, 1);
        var longAgo = GameNight.Create("Long ago", string.Empty, UtcNow - GameNight.PlayWindow - TimeSpan.FromMinutes(1), 1, 1);
        captured!.IsSatisfiedBy(stillPlaying).Should().BeTrue();
        captured.IsSatisfiedBy(longAgo).Should().BeFalse();

        VerifyGetStatisticsCalls(nightPlanned: true);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldNotCountPlannedNights_WhenNoNightIsPlanned()
    {
        var result = await _sut.GetStatistics(null, Token);

        result.NextGameNight.Should().BeNull();
        result.PlannedGameNights.Should().Be(0);

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldSkipGameNights_WhenTheFeatureIsDisabled()
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true))
            .ReturnsAsync(false);

        var result = await _sut.GetStatistics(null, Token);

        result.NextGameNight.Should().BeNull();
        result.PlannedGameNights.Should().Be(0);

        VerifyGetStatisticsCalls(gameNightsEnabled: false);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnTheShelfOfShameSummary_WhenEnabled()
    {
        _shameServiceMock
            .Setup(x => x.GetShameStatistics(null))
            .ReturnsAsync(new ShameStatistics { Count = 9, NeverPlayedCount = 5, TotalValue = 365m, PlannedCount = 1 });
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false))
            .ReturnsAsync(true);

        var result = await _sut.GetStatistics(null, Token);

        result.ShelfOfShame.Should().BeEquivalentTo(new DashboardShelfOfShameDto { Count = 9, NeverPlayedCount = 5, TotalValue = 365m });

        VerifyGetStatisticsCalls(shelfOfShameEnabled: true);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnNoShelfOfShame_WhenDisabled()
    {
        var result = await _sut.GetStatistics(null, Token);

        result.ShelfOfShame.Should().BeNull();

        VerifyGetStatisticsCalls();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldUseTheLocalMonthAndYear_WhenUtcIsStillInThePreviousOne()
    {
        var localNewYear = new DateTime(2027, 1, 1);
        var localYearStartUtc = new DateTime(2026, 12, 31, 22, 0, 0, DateTimeKind.Utc);
        _dateTimeProviderMock.Setup(x => x.Now).Returns(localNewYear.AddMinutes(30));
        _dateTimeProviderMock.Setup(x => x.ConvertToUtc(localNewYear)).Returns(localYearStartUtc);
        _sessionRepositoryMock.Setup(x => x.CountAsync(StartedSince(localYearStartUtc), Token)).ReturnsAsync(6);
        _playerRepositoryMock.Setup(x => x.CountActivePlayersSince(localYearStartUtc, Token)).ReturnsAsync(2);
        _gameStatisticsRepositoryMock
            .Setup(x => x.GetMostPlayedGamesSince(4, localYearStartUtc, Token))
            .ReturnsAsync(new List<(int GameId, string Title, string? Image, int PlayCount)> { (1, "Catan", null, 1) });

        var result = await _sut.GetStatistics(null, Token);

        result.SessionsThisMonth.Should().Be(6);
        result.ActivePlayersThisMonth.Should().Be(2);
        result.MostPlayedGamesThisMonth.Should().ContainSingle().Which.Id.Should().Be(1);
        result.MostPlayedGamesThisYear.Should().ContainSingle().Which.Id.Should().Be(1);
        _dateTimeProviderMock.Verify(x => x.ConvertToUtc(localNewYear), Times.Exactly(2));
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostPlayedGamesSince(4, localYearStartUtc, Token), Times.Exactly(2));
        _gameStatisticsRepositoryMock.Verify(x => x.GetMostPlayedGames(4, Token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetTotalPayedAsync(Token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.CountPricedOwnedGamesAsync(Token), Times.Once);
        _gameStatisticsRepositoryMock.Verify(x => x.GetGameStateCounts(Token), Times.Once);
        _gameRepositoryMock.Verify(x => x.CountAsync(Token), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetTotalExpansionCount(Token), Times.Once);
        _gameRepositoryMock.Verify(x => x.GetRecentlyAddedGames(3, Token), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountAsync(Token), Times.Once);
        _playerRepositoryMock.Verify(x => x.CountActivePlayersSince(localYearStartUtc, Token), Times.Once);
        _playerRepositoryMock.Verify(x => x.GetWinTotals(Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.CountAsync(StartedSince(localYearStartUtc), Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetTotalPlayTime(Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetMeanPlayTime(Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.GetRecentSessions(5, Token), Times.Once);
        _sessionRepositoryMock.Verify(x => x.AnyAsync(It.IsAny<WonCoopSessionsSpec>(), Token), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false), Times.Once);
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.FirstOrDefaultAsync(NextNight(), Token), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.Now, Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Once);
        VerifyNoOtherCalls();
    }

    private void SetupDefaultMocks()
    {
        _gameRepositoryMock.Setup(x => x.CountAsync(Token)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.CountAsync(Token)).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.CountAsync(Token)).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.GetTotalPlayTime(Token)).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.GetMeanPlayTime(Token)).ReturnsAsync(0);
        _gameStatisticsRepositoryMock.Setup(x => x.GetTotalPayedAsync(Token)).ReturnsAsync((decimal?)null);
        _gameStatisticsRepositoryMock.Setup(x => x.CountPricedOwnedGamesAsync(Token)).ReturnsAsync(0);
        _gameRepositoryMock.Setup(x => x.GetTotalExpansionCount(Token)).ReturnsAsync(0);
        _sessionRepositoryMock.Setup(x => x.CountAsync(StartedSince(MonthStartUtc), Token)).ReturnsAsync(0);
        _playerRepositoryMock.Setup(x => x.CountActivePlayersSince(MonthStartUtc, Token)).ReturnsAsync(0);

        _sessionRepositoryMock.Setup(x => x.GetRecentSessions(5, Token)).ReturnsAsync([]);
        _gameStatisticsRepositoryMock.Setup(x => x.GetGameStateCounts(Token)).ReturnsAsync([]);
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGames(4, Token)).ReturnsAsync(new List<(int, string, string?, int)>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGamesSince(4, MonthStartUtc, Token)).ReturnsAsync(new List<(int, string, string?, int)>());
        _gameStatisticsRepositoryMock.Setup(x => x.GetMostPlayedGamesSince(4, YearStartUtc, Token)).ReturnsAsync(new List<(int, string, string?, int)>());
        _playerRepositoryMock.Setup(x => x.GetWinTotals(Token)).ReturnsAsync(new List<(int, string, string?, int, int)>());
        _sessionRepositoryMock.Setup(x => x.AnyAsync(It.IsAny<WonCoopSessionsSpec>(), Token)).ReturnsAsync(false);
        _gameRepositoryMock.Setup(x => x.GetRecentlyAddedGames(3, Token)).ReturnsAsync([]);

        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true))
            .ReturnsAsync(true);
        _gameNightRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(NextNight(), Token))
            .ReturnsAsync((DashboardGameNightDto?)null);
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.ShelfOfShameEnabled, false))
            .ReturnsAsync(false);
    }
}
