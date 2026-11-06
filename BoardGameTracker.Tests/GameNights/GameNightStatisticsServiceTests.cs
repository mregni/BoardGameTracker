using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
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
using Microsoft.Extensions.Logging.Abstractions;
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
    private static readonly CancellationToken Token = new CancellationTokenSource().Token;

    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IConfigRepository> _configRepositoryMock;
    private readonly FixedClock _clock = new();
    private readonly GameNightStatisticsService _service;
    private GameNightStatisticsSpec? _capturedSpec;

    public GameNightStatisticsServiceTests()
    {
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _configRepositoryMock = new Mock<IConfigRepository>();

        _service = new GameNightStatisticsService(
            _gameNightRepositoryMock.Object,
            _configRepositoryMock.Object,
            _clock,
            NullLogger<GameNightStatisticsService>.Instance);
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
        SetupNightsFrom(null, nights);
    }

    private void SetupNightsFrom(DateTime? from, params GameNightStatisticsNight[] nights)
    {
        _gameNightRepositoryMock
            .Setup(x => x.ListAsync(Window(from), Token))
            .Callback<ISpecification<GameNight, GameNightStatisticsNight>, CancellationToken>((spec, _) => _capturedSpec = (GameNightStatisticsSpec)spec)
            .ReturnsAsync(nights.ToList());
    }

    private ISpecification<GameNight, GameNightStatisticsNight> Window(DateTime? from) =>
        It.Is<ISpecification<GameNight, GameNightStatisticsNight>>(s => CoversTheWindow(s, from, _clock.UtcNow));

    private static bool CoversTheWindow(ISpecification<GameNight, GameNightStatisticsNight> spec, DateTime? from, DateTime now)
    {
        var latestStart = now + GameNight.PlayOpensBefore;
        var earliestStart = from ?? new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return spec is GameNightStatisticsSpec
            && spec.IsSatisfiedBy(GameNight.Create("Latest", "", latestStart, 1, 1))
            && !spec.IsSatisfiedBy(GameNight.Create("Too late", "", latestStart.AddMinutes(1), 1, 1))
            && spec.IsSatisfiedBy(GameNight.Create("Earliest", "", earliestStart, 1, 1))
            && spec.IsSatisfiedBy(GameNight.Create("Too early", "", earliestStart.AddMinutes(-1), 1, 1)) == (from == null);
    }

    private void VerifyLoaded(DateTime? from = null)
    {
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true), Times.Once);
        _gameNightRepositoryMock.Verify(
            x => x.ListAsync(Window(from), Token),
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
            AttendanceOverrides = attendees.Select(x => new AttendanceOverride(x, true)).ToList(),
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

        var act = () => _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        await act.Should().ThrowAsync<FeatureDisabledException>();
        _configRepositoryMock.Verify(x => x.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnAnEmptyOverview_WhenNoNightWasHeld()
    {
        SetupEnabled();
        SetupNights(Night(1, Now.AddDays(3), 1, [1]));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.TiedNights.Should().Be(1);
        result.Winners.Select(x => (x.PlayerId, x.Rank, x.NightsWon, x.NightsAttended, x.GamesWon))
            .Should().Equal((1, 1, 2, 4, 3), (2, 2, 1, 3, 2), (3, 2, 1, 3, 1));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldCountEveryoneWhoPlayedAsAttended_WhateverTheirRsvp()
    {
        SetupEnabled();
        SetupNights(Night(1, January, 1, [], Played(10, January, 30, (1, false), (2, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Winners.Should().ContainSingle(x => x.PlayerId == 2 && x.NightsWon == 1 && x.NightsAttended == 1);
        result.Attendance.Select(x => x.PlayerId).Should().Equal(1, 2);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldListTheNightWinnerHistory_NewestFirst_WithTheirStreak()
    {
        SetupEnabled();
        SetupNights(Season());

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

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

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Records.MostGames!.GameId.Should().BeNull();
        result.Records.BiggestTable!.Value.Should().Be(1);
        result.AverageTable.Should().Be(1);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldDeriveTheTableFromWhoPlayed_RunningGamesTheHostAndOverrides()
    {
        SetupEnabled();
        var night = Night(1, January, 1, [],
            Played(10, January, 60, (2, true), (3, false), (4, false)),
            Played(11, January.AddHours(1), 60, (2, true), (5, false)));
        night.RunningPlayerIds = [6];
        night.AttendanceOverrides = [new AttendanceOverride(7, true), new AttendanceOverride(1, false)];
        night.EndedAt = January.AddHours(4);
        SetupNights(night);

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Attendance.Select(x => x.PlayerId).Should().BeEquivalentTo([2, 3, 4, 5, 6, 7]);
        result.Records.BiggestTable!.Value.Should().Be(6);
        result.AverageTable.Should().Be(6);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldMarkANightInProgressAsSoFar_AndKeepItOutOfTheStandingsAndStreaks()
    {
        SetupEnabled();
        var tonight = Now.AddHours(-3);
        SetupNights(
            Night(1, January, 1, [], Played(10, January, 30, (2, true), (1, false))),
            Night(2, February, 1, [], Played(10, February, 30, (2, true), (1, false))),
            Night(3, tonight, 1, [], Played(10, tonight, 30, (2, true), (1, false)), Played(11, tonight.AddHours(1), 30, (1, true), (2, false), (3, false))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.History.Select(x => (x.GameNightId, x.InProgress)).Should().Equal((3, true), (2, false), (1, false));
        result.History[0].WinnerIds.Should().Equal(1, 2);
        result.History[0].WinnerStreak.Should().Be(0);
        result.History[1].WinnerStreak.Should().Be(2);
        result.TiedNights.Should().Be(0);
        result.Winners.Should().ContainSingle();
        result.Winners[0].Should().Match<NightWinnerStatisticDto>(x => x.PlayerId == 2 && x.NightsWon == 2 && x.NightsAttended == 2);
        result.Streaks.CurrentWinner!.PlayerId.Should().Be(2);
        result.Streaks.CurrentWinner.Length.Should().Be(2);
        result.Streaks.CurrentWinner.To.Should().Be(February);
        result.Streaks.CurrentAttendance!.Length.Should().Be(2);
        result.NightsWithSessions.Should().Be(3);
        result.GamesPlayed.Should().Be(4);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(GameNightStatisticsPeriod.AllTime, new[] { 1, 2, 3, 4 })]
    [InlineData(GameNightStatisticsPeriod.ThisYear, new[] { 3, 4 })]
    [InlineData(GameNightStatisticsPeriod.Last12Months, new[] { 2, 3, 4 })]
    public async Task GetStatistics_ShouldFilterTheNightsByPeriod(GameNightStatisticsPeriod period, int[] expectedIds)
    {
        DateTime? from = period switch
        {
            GameNightStatisticsPeriod.ThisYear => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            GameNightStatisticsPeriod.Last12Months => Now.AddMonths(-12),
            _ => null
        };
        SetupEnabled();
        SetupNightsFrom(from);
        var nights = new[]
        {
            GameNight.Create("Long ago", "", new DateTime(2024, 3, 1, 20, 0, 0, DateTimeKind.Utc), 1, 1),
            GameNight.Create("Last autumn", "", new DateTime(2025, 11, 1, 20, 0, 0, DateTimeKind.Utc), 1, 1),
            GameNight.Create("New year", "", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1, 1),
            GameNight.Create("This morning", "", Now.AddHours(-1), 1, 1),
            GameNight.Create("Tomorrow", "", Now.AddHours(13), 1, 1)
        };
        for (var i = 0; i < nights.Length; i++)
        {
            nights[i].Id = i + 1;
        }

        var result = await _service.GetStatistics(period, Token);

        result.Period.Should().Be(period);
        _capturedSpec!.Evaluate(nights).Select(x => x.Id).Should().Equal(expectedIds);
        VerifyLoaded(from);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldStartThisYearAtTheLocalNewYear_WhenUtcIsStillInTheOldYear()
    {
        _clock.UtcNow = new DateTime(2026, 12, 31, 23, 30, 0, DateTimeKind.Utc);
        _clock.Offset = TimeSpan.FromHours(1);
        var localNewYearInUtc = new DateTime(2026, 12, 31, 23, 0, 0, DateTimeKind.Utc);
        SetupEnabled();
        SetupNightsFrom(localNewYearInUtc);

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.ThisYear, Token);

        result.Period.Should().Be(GameNightStatisticsPeriod.ThisYear);
        VerifyLoaded(localNewYearInUtc);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldCountANightAsHeld_ExactlyWhenThePlayWindowCloses()
    {
        var closed = Now - GameNight.PlayWindow;
        SetupEnabled();
        SetupNights(Night(1, closed, 1, [1]), Night(2, closed.AddMinutes(1), 1, [1]));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.TotalNights.Should().Be(1);
        result.Nights.Should().Be(0);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldKeepANightInProgress_UntilThePlayWindowCloses()
    {
        var closed = Now - GameNight.PlayWindow;
        var open = closed.AddMinutes(1);
        SetupEnabled();
        SetupNights(Night(1, closed, 1, [1], Played(10, closed, 30, (1, true))), Night(2, open, 1, [1], Played(10, open, 30, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.History.Select(x => (x.GameNightId, x.InProgress)).Should().Equal((2, true), (1, false));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldHaveNoAverageGap_WithASingleNight()
    {
        SetupEnabled();
        SetupNights(Night(1, January, 1, [1], Played(10, January, 30, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Nights.Should().Be(1);
        result.AverageDaysBetweenNights.Should().BeNull();
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldOrderTheNightsByStartThenId_WhateverOrderTheyArrive()
    {
        SetupEnabled();
        SetupNights(
            Night(3, February, 1, [1], Played(10, February, 30, (1, true))),
            Night(2, January, 1, [1], Played(10, January, 30, (1, true))),
            Night(1, January, 1, [1], Played(10, January, 30, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.History.Select(x => x.GameNightId).Should().Equal(3, 2, 1);
        result.RecentNights.Select(x => x.GameNightId).Should().Equal(1, 2, 3);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldOrderEqualNightWinners_ByTheirWinRate()
    {
        SetupEnabled();
        SetupNights(
            Night(1, January, 9, [], Played(10, January, 30, (1, true))),
            Night(2, February, 9, [], Played(10, February, 30, (2, true))),
            Night(3, March, 9, [], Played(10, March, 30, (1, false))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Winners.Select(x => (x.PlayerId, x.Rank, x.NightsWon, x.NightsAttended)).Should().Equal((2, 1, 1, 1), (1, 1, 1, 2));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldOrderEqualNightWinners_ByGamesWonThenPlayerId()
    {
        SetupEnabled();
        SetupNights(
            Night(1, January, 9, [], Played(10, January, 30, (5, true)), Played(11, January, 30, (5, true))),
            Night(2, February, 9, [], Played(10, February, 30, (4, true))),
            Night(3, March, 9, [], Played(10, March, 30, (3, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Winners.Select(x => (x.PlayerId, x.GamesWon)).Should().Equal((5, 2), (3, 1), (4, 1));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldSkipRanks_AfterSharedPlaces()
    {
        SetupEnabled();
        SetupNights(
            Night(1, January, 9, [], Played(10, January, 30, (1, true))),
            Night(2, February, 9, [], Played(10, February, 30, (1, true))),
            Night(3, March, 9, [], Played(10, March, 30, (2, true))),
            Night(4, April, 9, [], Played(10, April, 30, (2, true))),
            Night(5, May, 9, [], Played(10, May, 30, (3, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Winners.Select(x => (x.PlayerId, x.Rank)).Should().Equal((1, 1), (2, 1), (3, 3));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldNotCallACoopLossShared()
    {
        var coopLoss = Played(13, January, 60, (1, false), (2, false));
        coopLoss.IsCoop = true;
        SetupEnabled();
        SetupNights(Night(1, January, 1, [1, 2], coopLoss));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        var night = result.History.Single();
        night.CoopOnly.Should().BeTrue();
        night.SharedThroughCoop.Should().BeFalse();
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldPreferTheLaterStreak_WhenTwoAreEquallyLong()
    {
        SetupEnabled();
        SetupNights(
            Night(1, January, 9, [], Played(10, January, 30, (1, true))),
            Night(2, February, 9, [], Played(11, February, 30, (1, true))),
            Night(3, March, 9, [], Played(12, March, 30, (2, true))),
            Night(4, April, 9, [], Played(13, April, 30, (2, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Streaks.LongestWinner.Should().BeEquivalentTo(new { PlayerId = 2, Length = 2, From = March, To = April });
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldPreferTheLowestPlayerId_WhenStreaksEndTogether()
    {
        SetupEnabled();
        SetupNights(
            Night(1, January, 9, [], Played(10, January, 30, (2, true), (1, false)), Played(11, January, 30, (1, true), (2, false))),
            Night(2, February, 9, [], Played(12, February, 30, (2, true), (1, false)), Played(13, February, 30, (1, true), (2, false))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Streaks.LongestWinner!.PlayerId.Should().Be(1);
        result.Streaks.CurrentWinner!.PlayerId.Should().Be(1);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldOrderGames_ByPlaysThenNightsThenId()
    {
        SetupEnabled();
        SetupNights(
            Night(1, January, 1, [],
                Played(22, January, 10, (1, true)),
                Played(20, January, 10, (1, true)),
                Played(20, January, 10, (1, true)),
                Played(21, January, 10, (1, true))),
            Night(2, February, 1, [],
                Played(21, February, 10, (1, true)),
                Played(19, February, 10, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Games.Select(x => (x.GameId, x.TimesPlayed, x.Nights)).Should().Equal((21, 2, 2), (20, 2, 1), (19, 1, 1), (22, 1, 1));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldCountAPlannedGameOncePerNight_MostPlannedFirst()
    {
        var first = Night(1, January, 1, [], Played(10, January, 10, (1, true)));
        first.PlannedGameIds = [30, 30, 31, 32];
        var second = Night(2, February, 1, [], Played(10, February, 10, (1, true)));
        second.PlannedGameIds = [32, 31, 10];
        SetupEnabled();
        SetupNights(first, second);

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.PlannedNeverPlayed.Select(x => (x.GameId, x.TimesPlanned)).Should().Equal((31, 2), (32, 2), (30, 1));
        result.Games.Single().TimesPlanned.Should().Be(1);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldOrderAttendance_ByNightsThenHostedThenId()
    {
        SetupEnabled();
        SetupNights(
            Night(1, January, 7, [3, 2, 1], Played(10, January, 10, (7, true))),
            Night(2, February, 7, [3, 2], Played(10, February, 10, (7, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Attendance.Select(x => (x.PlayerId, x.NightsAttended, x.NightsHosted))
            .Should().Equal((7, 2, 2), (2, 2, 0), (3, 2, 0), (1, 1, 0));
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldMeasureTheLongestNight_FromTheFirstStartToTheLastEnd()
    {
        SetupEnabled();
        SetupNights(Night(1, January, 1, [], Played(10, January, 30, (1, true)), Played(11, January.AddHours(2), 30, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Records.LongestNight!.Value.Should().Be(150);
        result.TotalMinutes.Should().Be(60);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldNameNoTopGame_WhenTwoGamesTiedAboveOnePlay()
    {
        SetupEnabled();
        SetupNights(Night(1, January, 1, [],
            Played(10, January, 10, (1, true)),
            Played(10, January, 10, (1, true)),
            Played(11, January, 10, (1, true)),
            Played(11, January, 10, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Records.MostGames!.Value.Should().Be(4);
        result.Records.MostGames.GameId.Should().BeNull();
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldGiveTiedRecordsToTheEarliestNight()
    {
        SetupEnabled();
        SetupNights(
            Night(1, February, 5, [1], Played(10, February, 60, (1, true))),
            Night(2, January, 5, [1], Played(10, January, 60, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Records.LongestNight!.GameNightId.Should().Be(2);
        result.Records.MostGames!.GameNightId.Should().Be(2);
        result.Records.BiggestTable!.GameNightId.Should().Be(2);
        result.Records.LatestFinish!.GameNightId.Should().Be(2);
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetStatistics_ShouldJudgeTheLatestFinish_ByTheLocalClock()
    {
        _clock.Offset = TimeSpan.FromHours(1);
        var pastMidnightLocally = new DateTime(2026, 2, 6, 23, 30, 0, DateTimeKind.Utc);
        SetupEnabled();
        SetupNights(
            Night(1, January, 1, [], Played(10, January.AddMinutes(90), 60, (1, true))),
            Night(2, pastMidnightLocally, 1, [], Played(11, pastMidnightLocally, 15, (1, true))));

        var result = await _service.GetStatistics(GameNightStatisticsPeriod.AllTime, Token);

        result.Records.LatestFinish.Should().BeEquivalentTo(new
        {
            GameNightId = 1,
            Value = 60d,
            GameId = (int?)10,
            At = (DateTime?)January.AddMinutes(150)
        });
        VerifyLoaded();
        VerifyNoOtherCalls();
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = GameNightStatisticsServiceTests.Now;
        public TimeSpan Offset { get; set; }
        public DateTime Now => UtcNow + Offset;
        public TimeZoneInfo TimeZone => TimeZoneInfo.Utc;
        public DateTime ConvertToLocalTime(DateTime utcDateTime) => utcDateTime + Offset;
        public DateTime ConvertToUtc(DateTime localDateTime) => DateTime.SpecifyKind(localDateTime - Offset, DateTimeKind.Utc);
    }
}
