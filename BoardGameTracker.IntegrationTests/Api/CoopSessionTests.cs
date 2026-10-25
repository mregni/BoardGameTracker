using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class CoopSessionTests
{
    private readonly IntegrationFixture _fixture;

    public CoopSessionTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LoggingCoopSessions_ShouldShareTheTeamResult_AndCountItInEveryStatistic()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var aliceId = await CreateAsync<PlayerDto>(user, "/api/player", new CreatePlayerCommand { Name = "Alice " + suffix }, x => x.Id);
        var bobId = await CreateAsync<PlayerDto>(user, "/api/player", new CreatePlayerCommand { Name = "Bob " + suffix }, x => x.Id);
        var game = await PostAsync<GameDto>(user, "/api/game", new CreateGameCommand { Title = "Pandemic " + suffix, State = GameState.Owned, HasCoopMode = true, CoopByDefault = true });
        game.HasCoopMode.Should().BeTrue();
        game.CoopByDefault.Should().BeTrue();

        var won = await PostAsync<SessionDto>(user, "/api/session", CoopSession(game.Id, true, 42, aliceId, bobId));
        won.IsCoop.Should().BeTrue();
        won.TeamWon.Should().BeTrue();
        won.TeamScore.Should().Be(42);
        won.PlayerSessions.Should().HaveCount(2).And.OnlyContain(x => x.Won && x.Score == null);
        won.PlayerSessions.Single(x => x.PlayerId == aliceId).FirstPlay.Should().BeTrue();

        var lost = await PostAsync<SessionDto>(user, "/api/session", CoopSession(game.Id, false, null, aliceId, bobId));
        lost.PlayerSessions.Should().OnlyContain(x => !x.Won);

        var gameStats = await user.GetFromJsonAsync<GameStatisticsResponse>($"/api/game/{game.Id}/statistics", IntegrationFixture.Json);
        gameStats!.GameStats.PlayCount.Should().Be(2);
        gameStats.GameStats.CoopWinCount.Should().Be(1);
        gameStats.GameStats.CoopLossCount.Should().Be(1);
        gameStats.GameStats.HighScore.Should().BeNull();
        gameStats.GameStats.ScoredSessionCount.Should().Be(0);

        var aliceStats = await user.GetFromJsonAsync<PlayerStatistics>($"/api/player/{aliceId}/statistics", IntegrationFixture.Json);
        aliceStats!.PlayCount.Should().Be(2);
        aliceStats.WinCount.Should().Be(1);
        aliceStats.CoopWinCount.Should().Be(1);
        aliceStats.CoopLossCount.Should().Be(1);

        var leaderboard = await user.GetFromJsonAsync<LeaderboardDto>("/api/leaderboard", IntegrationFixture.Json);
        leaderboard!.Players.Should().Contain(x => x.PlayerId == aliceId && x.WinCount == 1 && x.PlayCount == 2);
        leaderboard.Players.Should().Contain(x => x.PlayerId == bobId && x.WinCount == 1 && x.PlayCount == 2);

        using var compare = JsonDocument.Parse(await user.GetStringAsync($"/api/compare/{aliceId}/{bobId}"));
        var directWins = compare.RootElement.GetProperty("directWins");
        directWins.GetProperty("playerOne").GetInt32().Should().Be(0);
        directWins.GetProperty("playerTwo").GetInt32().Should().Be(0);
        compare.RootElement.GetProperty("totalSessionsTogether").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task LoggingACoopSession_ShouldBeRefused_ForAGameWithoutCoopMode_OrWithoutATeamResult()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var playerId = await CreateAsync<PlayerDto>(user, "/api/player", new CreatePlayerCommand { Name = "Carol " + suffix }, x => x.Id);
        var competitive = await PostAsync<GameDto>(user, "/api/game", new CreateGameCommand { Title = "Azul " + suffix, State = GameState.Owned });
        var coop = await PostAsync<GameDto>(user, "/api/game", new CreateGameCommand { Title = "Hanabi " + suffix, State = GameState.Owned, HasCoopMode = true });

        var notCoop = await user.PostAsJsonAsync("/api/session", CoopSession(competitive.Id, true, null, playerId));
        notCoop.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await notCoop.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.CoopNotSupported);

        var noResult = await user.PostAsJsonAsync("/api/session", CoopSession(coop.Id, null, null, playerId));
        noResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await noResult.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.CoopResultRequired);
    }

    [Fact]
    public async Task FinishingACoopGameOnAGameNight_ShouldMakeTheWholeTableTheNightWinners()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var hostId = await CreateAsync<PlayerDto>(user, "/api/player", new CreatePlayerCommand { Name = "Host " + suffix }, x => x.Id);
        var guestId = await CreateAsync<PlayerDto>(user, "/api/player", new CreatePlayerCommand { Name = "Guest " + suffix }, x => x.Id);
        var locationId = await CreateAsync<LocationDto>(user, "/api/location", new CreateLocationCommand { Name = "Table " + suffix }, x => x.Id);
        var game = await PostAsync<GameDto>(user, "/api/game", new CreateGameCommand { Title = "Spirit Island " + suffix, State = GameState.Owned, HasCoopMode = true, CoopByDefault = true });
        var gameNightId = await CreateAsync<GameNightDto>(user, "/api/gamenight", new CreateGameNightCommand
        {
            Title = "Co-op night",
            StartDate = DateTime.UtcNow.AddHours(-3),
            HostId = hostId,
            LocationId = locationId,
            InvitedPlayerIds = [guestId],
        }, x => x.Id);

        var runningGame = await PostAsync<RunningGameDto>(user, $"/api/gamenight/{gameNightId}/running-games", new StartRunningGameCommand { GameId = game.Id, PlayerIds = [hostId, guestId] });
        var finished = await user.PostAsJsonAsync($"/api/gamenight/running-games/{runningGame.Id}/finish", new FinishRunningGameCommand
        {
            PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = hostId }, new CreatePlayerSessionCommand { PlayerId = guestId }],
            IsCoop = true,
            TeamWon = true,
        });
        finished.StatusCode.Should().Be(HttpStatusCode.OK, await finished.Content.ReadAsStringAsync());

        var detail = await user.GetFromJsonAsync<GameNightDetailDto>($"/api/gamenight/{gameNightId}", IntegrationFixture.Json);
        detail!.Sessions.Should().ContainSingle(x => x.IsCoop && x.TeamWon == true);
        detail.GameNight.Summary.WinnerIds.Should().BeEquivalentTo([hostId, guestId]);
    }

    private static CreateSessionCommand CoopSession(int gameId, bool? teamWon, double? teamScore, params int[] playerIds)
    {
        return new CreateSessionCommand
        {
            GameId = gameId,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 45,
            IsCoop = true,
            TeamWon = teamWon,
            TeamScore = teamScore,
            PlayerSessions = playerIds
                .Select((id, index) => new CreatePlayerSessionCommand { PlayerId = id, FirstPlay = index == 0, Score = 10 })
                .ToList(),
        };
    }

    private static async Task<TDto> PostAsync<TDto>(HttpClient user, string url, object command)
    {
        var response = await user.PostAsJsonAsync(url, command);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<TDto>(IntegrationFixture.Json))!;
    }

    private static async Task<int> CreateAsync<TDto>(HttpClient user, string url, object command, Func<TDto, int> id)
    {
        return id(await PostAsync<TDto>(user, url, command));
    }
}
