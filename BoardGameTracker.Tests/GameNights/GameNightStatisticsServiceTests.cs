using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights;
using BoardGameTracker.Core.GameNights.Specifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class GameNightStatisticsServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime January = new(2026, 1, 9, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime February = new(2026, 2, 6, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime March = new(2026, 3, 6, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime April = new(2026, 4, 3, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime May = new(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly GameNightStatisticsService _service;
    private GameNightStatisticsSpec? _capturedSpec;

    public GameNightStatisticsServiceTests()
    {
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _configRepositoryMock = new Mock<IConfigRepository>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(Now);
        _dateTimeProviderMock.Setup(x => x.ConvertToLocalTime(It.IsAny<DateTime>())).Returns<DateTime>(x => x);
        _dateTimeProviderMock.Setup(x => x.ConvertToUtc(It.IsAny<DateTime>())).Returns<DateTime>(x => DateTime.SpecifyKind(x, DateTimeKind.Utc));

        _service = new GameNightStatisticsService(
            _gameNightRepositoryMock.Object,
            _configRepositoryMock.Object,
            _dateTimeProviderMock.Object,
            new Mock<ILogger<GameNightStatisticsService>>().Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _configRepositoryMock.VerifyNoOtherCalls();
    }

    private void SetupEnabled(bool enabled = true)
    {
        _configRepositoryMock
            .Setup(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true))
            .ReturnsAsync(enabled);
    }

    private void SetupNights(params GameNightStatisticsNight[] nights)
    {
        _gameNightRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ISpecification<GameNight, GameNightStatisticsNight>>(), It.IsAny<CancellationToken>()))
            .Callback<ISpecification<GameNight, GameNightStatisticsNight>, CancellationToken>((spec, _) => _capturedSpec = (GameNightStatisticsSpec)spec)
            .ReturnsAsync(nights.ToList());
    }

    private void VerifyLoaded()
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true), Times.Once);
        _gameNightRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ISpecification<GameNight, GameNightStatisticsNight>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static GameNightStatisticsNight Night(int id, DateTime start, int hostId, int[] attendees, params GameNightStatisticsSession[] sessions)
    {
        return new GameNightStatisticsNight
        {
            Id = id,
            Title = $"Night {id}",
            StartDate = start,
            HostId = hostId,
            LocationId = 7,
            AttendeeIds = attendees.ToList(),
            Sessions = sessions.ToList()
        };
    }

    private static GameNightStatisticsSession Played(int gameId, DateTime start, int minutes, params (int PlayerId, bool Won)[] results)
    {
        return new GameNightStatisticsSession
        {
            GameId = gameId,
            Start = start,
            End = start.AddMinutes(minutes),
            Results = results.Select(x => new NightResult(x.PlayerId, x.Won)).ToList()
        };
    }

    private static GameNightStatisticsNight[] Season()
    {
        var first = Night(1, January, 2, [2, 3, 1],
            Played(10, January, 60, (2, true), (3, false), (1, false)),
            Played(11, January.AddHours(1), 60, (2, true), (1, false)),
            Played(10, January.AddHours(2), 60, (1, true), (2, false)));
        first.PlannedGameIds = [10, 12];

        var second = Night(2, February, 1, [2, 1],
            Played(10, February.AddHours(3).AddMinutes(30), 60, (1, true), (2, false)));
        second.PlannedGameIds = [12];

        var third = Night(3, March, 2, [1, 2, 3, 4],
            Played(10, March, 60, (1, true), (2, false)),
            Played(11, March.AddMinutes(90), 60, (3, true), (4, false)));

        var ended = Night(4, April, 3, [1, 3]);
        ended.EndedAt = April.AddHours(3);

        var unlinked = Night(5, May, 1, [1, 2]);
        var upcoming = Night(6, Now.AddHours(2), 1, [1]);

        return [first, second, third, ended, unlinked, upcoming];
    }

    [Fact]
    public async Task GetStatistics_ShouldThrow_WhenGameNightsAreDisabled()
    {
        SetupEnabled(false);

        var act = () => _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        await act.Should().ThrowAsync<FeatureDisabledException>();
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnAnEmptyOverview_WhenNoNightWasHeld()
    {
        SetupEnabled();
        SetupNights(Night(1, Now.AddDays(3), 1, [1]));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.Period.Should().Be(GameNightStatisticsPeriod.AllTime);
        result.TotalNights.Should().Be(0);
        result.Nights.Should().Be(0);
        result.GamesPlayed.Should().Be(0);
        result.AverageTable.Should().Be(0);
        result.AverageDaysBetweenNights.Should().BeNull();
        result.Winners.Should().BeEmpty();
        result.History.Should().BeEmpty();
        result.Games.Should().BeEmpty();
        result.PlannedNeverPlayed.Should().BeEmpty();
        result.Attendance.Should().BeEmpty();
        result.RecentNights.Should().BeEmpty();
        result.Streaks.CurrentWinner.Should().BeNull();
        result.Streaks.LongestWinner.Should().BeNull();
        result.Streaks.CurrentAttendance.Should().BeNull();
        result.Streaks.LongestGame.Should().BeNull();
        result.Records.LongestNight.Should().BeNull();
        result.Records.MostGames.Should().BeNull();
        result.Records.BiggestTable.Should().BeNull();
        result.Records.LatestFinish.Should().BeNull();
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldCountNightsWithSessionsOrEnded_AndHintAtUnloggedNights()
    {
        SetupEnabled();
        SetupNights(Season());

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.TotalNights.Should().Be(5);
        result.NightsWithSessions.Should().Be(3);
        result.Nights.Should().Be(4);
        result.GamesPlayed.Should().Be(6);
        result.TotalMinutes.Should().Be(360);
        result.AverageTable.Should().Be(2.75);
        result.AverageDaysBetweenNights.Should().Be(28);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldRankNightWinners_SharingTiesAndCountingOneGameNights()
    {
        SetupEnabled();
        SetupNights(Season());

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.TiedNights.Should().Be(1);
        result.Winners.Select(x => (x.PlayerId, x.Rank, x.NightsWon, x.NightsAttended, x.GamesWon))
            .Should().Equal((1, 1, 2, 4, 3), (2, 2, 1, 3, 2), (3, 2, 1, 3, 1));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldCountAWonNightAsAttended_WhenTheWinnerWasNotMarkedComing()
    {
        SetupEnabled();
        SetupNights(Night(1, January, 1, [1], Played(10, January, 30, (1, false), (2, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.Winners.Should().ContainSingle(x => x.PlayerId == 2 && x.NightsWon == 1 && x.NightsAttended == 1);
        result.Attendance.Select(x => x.PlayerId).Should().Equal(1);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldListTheNightWinnerHistory_NewestFirst_WithTheirStreak()
    {
        SetupEnabled();
        SetupNights(Season());

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.History.Select(x => x.GameNightId).Should().Equal(4, 3, 2, 1);
        result.History.Select(x => x.WinnerStreak).Should().Equal(0, 2, 1, 1);
        result.History[1].WinnerIds.Should().Equal(1, 3);
        result.History[1].MostWins.Should().Be(1);
        result.History[1].GamesPlayed.Should().Be(2);
        result.History[1].LocationId.Should().Be(7);
        result.History[2].WinnerIds.Should().Equal(1);
        result.History[3].WinnerIds.Should().Equal(2);
        result.History[3].MostWins.Should().Be(2);
        result.History[0].WinnerIds.Should().BeEmpty();
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldAggregateGames_WithPlannedCounts_AndPlannedButNeverPlayed()
    {
        SetupEnabled();
        SetupNights(Season());

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.Games.Select(x => (x.GameId, x.TimesPlayed, x.Nights, x.TimesPlanned))
            .Should().Equal((10, 4, 3, 1), (11, 2, 2, 0));
        result.PlannedNeverPlayed.Select(x => (x.GameId, x.TimesPlanned)).Should().Equal((12, 2));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldComputeStreaks_OverTheOrderedNights()
    {
        SetupEnabled();
        SetupNights(Season());

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.Streaks.CurrentWinner.Should().BeEquivalentTo(new { PlayerId = 1, Length = 2, From = February, To = March });
        result.Streaks.LongestWinner.Should().BeEquivalentTo(new { PlayerId = 1, Length = 2, From = February, To = March });
        result.Streaks.CurrentAttendance.Should().BeEquivalentTo(new { PlayerId = 1, Length = 4, From = January, To = April });
        result.Streaks.LongestGame.Should().BeEquivalentTo(new { GameId = 10, Length = 3, From = January, To = March, IsCurrent = true });
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldKeepTheRecordStreak_WhenSomeoneElseWonTheLastNights()
    {
        SetupEnabled();
        var nights = Enumerable.Range(0, 5)
            .Select(i => Night(i + 1, January.AddDays(7 * i), 1, [1, 2],
                Played(10 + i, January.AddDays(7 * i), 30, (i < 3 ? 1 : 2, true))))
            .ToArray();
        SetupNights(nights);

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.Streaks.CurrentWinner.Should().BeEquivalentTo(new { PlayerId = 2, Length = 2, From = January.AddDays(21), To = January.AddDays(28) });
        result.Streaks.LongestWinner.Should().BeEquivalentTo(new { PlayerId = 1, Length = 3, From = January, To = January.AddDays(14) });
        result.Streaks.LongestGame.Should().BeNull();
        result.History.Select(x => x.WinnerStreak).Should().Equal(2, 1, 3, 2, 1);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldShareTheNight_WithEveryoneAtTheTable_WhenTheTeamWonACoopNight()
    {
        SetupEnabled();
        var coopWin = Played(13, January, 60, (1, true), (2, true), (3, true));
        coopWin.IsCoop = true;
        var coopLoss = Played(13, January.AddHours(1), 60, (1, false), (2, false), (3, false));
        coopLoss.IsCoop = true;
        var mixedCoop = Played(13, February, 60, (1, true), (2, true));
        mixedCoop.IsCoop = true;
        SetupNights(
            Night(1, January, 1, [1, 2, 3], coopWin, coopLoss),
            Night(2, February, 1, [1, 2], mixedCoop, Played(10, February.AddHours(1), 30, (1, true), (2, false))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        var coopNight = result.History.Single(x => x.GameNightId == 1);
        coopNight.WinnerIds.Should().Equal(1, 2, 3);
        coopNight.MostWins.Should().Be(1);
        coopNight.CoopOnly.Should().BeTrue();
        coopNight.SharedThroughCoop.Should().BeTrue();
        var mixedNight = result.History.Single(x => x.GameNightId == 2);
        mixedNight.WinnerIds.Should().Equal(1);
        mixedNight.CoopOnly.Should().BeFalse();
        mixedNight.SharedThroughCoop.Should().BeFalse();
        result.TiedNights.Should().Be(1);
        result.Winners.Select(x => (x.PlayerId, x.NightsWon)).Should().Equal((1, 2), (3, 1), (2, 1));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldBreakTheWinnerStreak_WhenNobodyWonANight()
    {
        SetupEnabled();
        SetupNights(
            Night(1, January, 1, [1], Played(10, January, 30, (1, true))),
            Night(2, February, 1, [1], Played(10, February, 30, (1, false))),
            Night(3, March, 1, [1], Played(10, March, 30, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.Streaks.CurrentWinner.Should().BeNull();
        result.Streaks.LongestWinner.Should().BeNull();
        result.History[1].WinnerIds.Should().BeEmpty();
        result.Streaks.LongestGame.Should().BeEquivalentTo(new { GameId = 10, Length = 3 });
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReportOnlyNightsAttended_WithTheLastNightsAsDots()
    {
        SetupEnabled();
        SetupNights(Season());

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.RecentNights.Select(x => x.GameNightId).Should().Equal(1, 2, 3, 4);
        result.RecentNights[0].Title.Should().Be("Night 1");
        result.RecentNights[0].StartDate.Should().Be(January);
        result.Attendance.Select(x => (x.PlayerId, x.NightsAttended, x.NightsHosted))
            .Should().Equal((1, 4, 1), (2, 3, 2), (3, 3, 1), (4, 1, 0));
        result.Attendance[1].RecentNights.Should().Equal(true, true, true, false);
        result.Attendance[3].RecentNights.Should().Equal(false, false, true, false);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldKeepOnlyTheLastTwelveNights_ForTheDots()
    {
        SetupEnabled();
        var nights = Enumerable.Range(0, 14)
            .Select(i => Night(i + 1, January.AddDays(7 * i), 1, i % 2 == 0 ? [1] : [1, 2], Played(10, January.AddDays(7 * i), 30, (1, true))))
            .ToArray();
        SetupNights(nights);

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.RecentNights.Select(x => x.GameNightId).Should().Equal(Enumerable.Range(3, 12));
        result.Attendance.Single(x => x.PlayerId == 2).RecentNights.Should().HaveCount(12).And.StartWith(false);
        result.Streaks.CurrentWinner.Should().BeEquivalentTo(new { PlayerId = 1, Length = 14 });
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldFindTheRecords()
    {
        SetupEnabled();
        SetupNights(Season());

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.Records.LongestNight.Should().BeEquivalentTo(new { GameNightId = 1, Title = "Night 1", StartDate = January, GamesPlayed = 3, Value = 180d });
        result.Records.MostGames.Should().BeEquivalentTo(new { GameNightId = 1, GamesPlayed = 3, Value = 3d, GameId = (int?)10 });
        result.Records.BiggestTable.Should().BeEquivalentTo(new { GameNightId = 3, Value = 4d });
        result.Records.LatestFinish.Should().BeEquivalentTo(new
        {
            GameNightId = 2,
            Value = 60d,
            GameId = (int?)10,
            At = (DateTime?)February.AddHours(4).AddMinutes(30)
        });
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldNotNameATopGame_WhenNoGameWasPlayedMoreThanTheOthers()
    {
        SetupEnabled();
        SetupNights(Night(1, January, 1, [], Played(10, January, 30, (1, true)), Played(11, January, 30, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime);

        result.Records.MostGames!.GameId.Should().BeNull();
        result.Records.BiggestTable.Should().BeNull();
        result.AverageTable.Should().Be(0);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GameNightStatisticsPeriod.AllTime, new[] { 1, 2, 3, 4 })]
    [InlineData(GameNightStatisticsPeriod.ThisYear, new[] { 3, 4 })]
    [InlineData(GameNightStatisticsPeriod.Last12Months, new[] { 2, 3, 4 })]
    public async Task GetStatistics_ShouldFilterTheNightsByPeriod(GameNightStatisticsPeriod period, int[] expectedIds)
    {
        SetupEnabled();
        SetupNights();
        var nights = new[]
        {
            GameNight.Create("Long ago", "", new DateTime(2024, 3, 1, 20, 0, 0, DateTimeKind.Utc), 1, 1),
            GameNight.Create("Last autumn", "", new DateTime(2025, 11, 1, 20, 0, 0, DateTimeKind.Utc), 1, 1),
            GameNight.Create("New year", "", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1, 1),
            GameNight.Create("This morning", "", Now.AddHours(-1), 1, 1),
            GameNight.Create("Tonight", "", Now.AddHours(8), 1, 1)
        };
        for (var i = 0; i < nights.Length; i++)
        {
            nights[i].Id = i + 1;
        }

        var result = await _service.GetStatistics(period);

        result.Period.Should().Be(period);
        _capturedSpec!.Evaluate(nights).Select(x => x.Id).Should().Equal(expectedIds);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }
}
