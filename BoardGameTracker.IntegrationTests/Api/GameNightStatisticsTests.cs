using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class GameNightStatisticsTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly DateTime _today = DateTime.UtcNow.Date.AddHours(-4);
    private int _annaId;
    private int _bramId;
    private int _cleoId;
    private int _azulId;
    private int _brassId;
    private int _everdellId;
    private int _firstNightId;
    private int _secondNightId;
    private int _endedNightId;

    public GameNightStatisticsTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDataAsync();
        await using var scope = _fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var anna = new Player("Anna");
        var bram = new Player("Bram");
        var cleo = new Player("Cleo");
        var azul = new Game("Azul", true, GameState.Owned);
        var brass = new Game("Brass", true, GameState.Owned);
        var everdell = new Game("Everdell", true, GameState.Owned);
        var home = new Location("Home");
        db.AddRange(anna, bram, cleo, azul, brass, everdell, home);
        await db.SaveChangesAsync();

        var longAgo = Night(db, "Long ago", _today.AddYears(-2), anna, home, [anna], [azul]);
        var first = Night(db, "First night", _today.AddDays(-21), anna, home, [anna, bram, cleo], [azul, everdell]);
        var second = Night(db, "Second night", _today.AddDays(-14), bram, home, [anna, bram], [everdell]);
        var ended = Night(db, "Ended night", _today.AddDays(-7), cleo, home, [bram, cleo], []);
        ended.End(_today.AddDays(-7).AddHours(3));
        ended.SetAttendance([bram.Id, cleo.Id]);
        Night(db, "Old night without sessions", _today.AddDays(-10), anna, home, [anna], []);
        Night(db, "Upcoming night", _today.AddDays(5), anna, home, [anna, bram], [brass]);
        await db.SaveChangesAsync();

        Play(db, longAgo, azul, longAgo.StartDate, 60, (anna, true));
        Play(db, first, azul, first.StartDate, 60, (anna, true), (bram, false), (cleo, false));
        Play(db, first, brass, first.StartDate.AddHours(1), 90, (anna, false), (bram, true), (cleo, false));
        Play(db, first, azul, first.StartDate.AddHours(3), 30, (anna, false), (bram, true));
        Play(db, second, azul, second.StartDate, 45, (anna, true), (bram, false));
        Play(db, second, brass, second.StartDate.AddHours(1), 45, (anna, false), (bram, true));
        await db.SaveChangesAsync();

        _annaId = anna.Id;
        _bramId = bram.Id;
        _cleoId = cleo.Id;
        _azulId = azul.Id;
        _brassId = brass.Id;
        _everdellId = everdell.Id;
        _firstNightId = first.Id;
        _secondNightId = second.Id;
        _endedNightId = ended.Id;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Statistics_ShouldSummariseTheNightsOfThePeriod_ForAReader()
    {
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var response = await reader.GetAsync("/api/gamenight/statistics?period=last12Months");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var statistics = (await response.Content.ReadFromJsonAsync<GameNightStatisticsDto>(IntegrationFixture.Json))!;
        statistics.Period.Should().Be(GameNightStatisticsPeriod.Last12Months);
        statistics.TotalNights.Should().Be(4);
        statistics.NightsWithSessions.Should().Be(2);
        statistics.Nights.Should().Be(3);
        statistics.GamesPlayed.Should().Be(5);
        statistics.TotalMinutes.Should().Be(270);
        statistics.AverageTable.Should().BeApproximately(7d / 3, 0.0001);
        statistics.AverageDaysBetweenNights.Should().BeApproximately(7, 0.0001);
        statistics.TiedNights.Should().Be(1);

        statistics.Winners.Select(x => (x.PlayerId, x.Rank, x.NightsWon, x.NightsAttended, x.GamesWon))
            .Should().Equal((_bramId, 1, 2, 3, 3), (_annaId, 2, 1, 2, 2));
        statistics.History.Select(x => x.GameNightId).Should().Equal(_endedNightId, _secondNightId, _firstNightId);
        statistics.History[1].WinnerIds.Should().BeEquivalentTo([_annaId, _bramId]);
        statistics.History[1].WinnerStreak.Should().Be(2);
        statistics.History[2].WinnerIds.Should().Equal(_bramId);
        statistics.History[2].MostWins.Should().Be(2);

        statistics.Games.Select(x => (x.GameId, x.TimesPlayed, x.Nights, x.TimesPlanned))
            .Should().Equal((_azulId, 3, 2, 1), (_brassId, 2, 2, 0));
        statistics.PlannedNeverPlayed.Select(x => (x.GameId, x.TimesPlanned)).Should().Equal((_everdellId, 2));

        statistics.Streaks.CurrentWinner!.PlayerId.Should().Be(_bramId);
        statistics.Streaks.CurrentWinner.Length.Should().Be(2);
        statistics.Streaks.CurrentAttendance!.PlayerId.Should().Be(_bramId);
        statistics.Streaks.CurrentAttendance.Length.Should().Be(3);
        statistics.Streaks.LongestGame!.GameId.Should().Be(_azulId);

        statistics.Attendance.Select(x => (x.PlayerId, x.NightsAttended, x.NightsHosted))
            .Should().Equal((_bramId, 3, 1), (_annaId, 2, 1), (_cleoId, 2, 1));
        statistics.Attendance[0].RecentNights.Should().Equal(true, true, true);
        statistics.RecentNights.Select(x => x.GameNightId).Should().Equal(_firstNightId, _secondNightId, _endedNightId);

        statistics.Records.LongestNight!.GameNightId.Should().Be(_firstNightId);
        statistics.Records.LongestNight.Value.Should().Be(210);
        statistics.Records.MostGames!.GameNightId.Should().Be(_firstNightId);
        statistics.Records.MostGames.GameId.Should().Be(_azulId);
        statistics.Records.BiggestTable!.GameNightId.Should().Be(_firstNightId);
        statistics.Records.BiggestTable.Value.Should().Be(3);
        statistics.Records.LatestFinish!.GameNightId.Should().Be(_firstNightId);
        statistics.Records.LatestFinish.GameId.Should().Be(_azulId);
    }

    [Fact]
    public async Task Statistics_ShouldIncludeOlderNights_ForAllTime()
    {
        using var user = await _fixture.CreateClientAsAsync("user");

        var statistics = await user.GetFromJsonAsync<GameNightStatisticsDto>("/api/gamenight/statistics", IntegrationFixture.Json);

        statistics!.Period.Should().Be(GameNightStatisticsPeriod.AllTime);
        statistics.Nights.Should().Be(4);
        statistics.TotalNights.Should().Be(5);
        statistics.GamesPlayed.Should().Be(6);
    }

    [Fact]
    public async Task Statistics_ShouldShareACoopNight_WithEveryoneOnTheWinningTeam()
    {
        int coopNightId;
        await using (var scope = _fixture.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var anna = await db.Players.SingleAsync(x => x.Id == _annaId);
            var bram = await db.Players.SingleAsync(x => x.Id == _bramId);
            var cleo = await db.Players.SingleAsync(x => x.Id == _cleoId);
            var azul = await db.Games.SingleAsync(x => x.Id == _azulId);
            var home = await db.Locations.FirstAsync();
            var night = Night(db, "Co-op night", _today.AddDays(-3), anna, home, [anna, bram, cleo], []);
            await db.SaveChangesAsync();
            var session = new Session(azul.Id, night.StartDate, night.StartDate.AddMinutes(50), string.Empty);
            session.SetGameNight(night.Id);
            session.AddPlayerSession(anna.Id, null, false, false);
            session.AddPlayerSession(bram.Id, null, false, false);
            session.AddPlayerSession(cleo.Id, null, false, false);
            session.MarkAsCoop(true, null);
            db.Sessions.Add(session);
            await db.SaveChangesAsync();
            coopNightId = night.Id;
        }

        using var user = await _fixture.CreateClientAsAsync("user");

        var statistics = await user.GetFromJsonAsync<GameNightStatisticsDto>("/api/gamenight/statistics", IntegrationFixture.Json);

        var coopNight = statistics!.History.Single(x => x.GameNightId == coopNightId);
        coopNight.WinnerIds.Should().BeEquivalentTo([_annaId, _bramId, _cleoId]);
        coopNight.CoopOnly.Should().BeTrue();
        coopNight.SharedThroughCoop.Should().BeTrue();
        statistics.History.Single(x => x.GameNightId == _firstNightId).CoopOnly.Should().BeFalse();
    }

    [Fact]
    public async Task Statistics_ShouldRefuseAnonymousCallers()
    {
        using var anonymous = _fixture.CreateClient();

        var response = await anonymous.GetAsync("/api/gamenight/statistics");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static GameNight Night(MainDbContext db, string title, DateTime start, Player host, Location location, Player[] coming, Game[] planned)
    {
        var night = GameNight.Create(title, string.Empty, start, host.Id, location.Id);
        night.SetInvitedPlayers(coming.Select(x => GameNightRsvp.Create(x.Id, GameNightRsvpState.Accepted)));
        night.SetSuggestedGames(planned);
        db.GameNights.Add(night);
        return night;
    }

    private static void Play(MainDbContext db, GameNight night, Game game, DateTime start, int minutes, params (Player Player, bool Won)[] players)
    {
        var session = new Session(game.Id, start, start.AddMinutes(minutes), string.Empty);
        session.SetGameNight(night.Id);
        foreach (var (player, won) in players)
        {
            session.AddPlayerSession(player.Id, null, false, won);
        }

        db.Sessions.Add(session);
    }
}
