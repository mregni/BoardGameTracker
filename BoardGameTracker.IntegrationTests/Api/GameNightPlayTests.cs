using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class GameNightPlayTests
{
    private readonly IntegrationFixture _fixture;

    public GameNightPlayTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FinishingARunningGame_ShouldLeaveOneSessionOfTheNight_AndNoRunningGame()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, gameId, hostId, guestId) = await CreateNightAsync(user);

        var started = await user.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", new StartRunningGameCommand { GameId = gameId, PlayerIds = [hostId, guestId] });
        started.StatusCode.Should().Be(HttpStatusCode.Created, await started.Content.ReadAsStringAsync());
        var runningGame = (await started.Content.ReadFromJsonAsync<RunningGameDto>(IntegrationFixture.Json))!;

        var during = await GetDetailAsync(user, gameNightId);
        during.RunningGames.Should().ContainSingle(x => x.Id == runningGame.Id && x.PlayerIds.Count == 2);
        during.Sessions.Should().BeEmpty();

        var finished = await user.PostAsJsonAsync($"/api/gamenight/running-games/{runningGame.Id}/finish", new FinishRunningGameCommand
        {
            PlayerSessions =
            [
                new CreatePlayerSessionCommand { PlayerId = hostId, Score = 82, Won = true },
                new CreatePlayerSessionCommand { PlayerId = guestId, Score = 71 },
            ],
            Comment = "Close one",
        });
        finished.StatusCode.Should().Be(HttpStatusCode.OK, await finished.Content.ReadAsStringAsync());
        var session = (await finished.Content.ReadFromJsonAsync<SessionDto>(IntegrationFixture.Json))!;
        session.GameNightId.Should().Be(gameNightId);
        session.Comment.Should().Be("Close one");
        session.LocationId.Should().NotBeNull();

        var after = await GetDetailAsync(user, gameNightId);
        after.RunningGames.Should().BeEmpty();
        after.Sessions.Should().ContainSingle(x => x.Id == session.Id);
        after.GameNight.Summary.GamesPlayed.Should().Be(1);
        after.GameNight.Summary.WinnerIds.Should().Equal(hostId);
    }

    [Fact]
    public async Task DiscardingARunningGame_ShouldLeaveNoSessionAndNoRunningGame()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, gameId, hostId, _) = await CreateNightAsync(user);
        var started = await user.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", new StartRunningGameCommand { GameId = gameId, PlayerIds = [hostId] });
        var runningGame = (await started.Content.ReadFromJsonAsync<RunningGameDto>(IntegrationFixture.Json))!;

        (await user.DeleteAsync($"/api/gamenight/running-games/{runningGame.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await GetDetailAsync(user, gameNightId);
        detail.RunningGames.Should().BeEmpty();
        detail.Sessions.Should().BeEmpty();
        (await user.DeleteAsync($"/api/gamenight/running-games/{runningGame.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var finishDiscarded = await user.PostAsJsonAsync($"/api/gamenight/running-games/{runningGame.Id}/finish", new FinishRunningGameCommand
        {
            PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = hostId }],
        });
        finishDiscarded.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LoggingAFinishedGame_ShouldAddASessionToTheNight()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, gameId, hostId, guestId) = await CreateNightAsync(user);

        var logged = await LogAsync(user, gameNightId, gameId, DateTime.UtcNow.AddMinutes(-50), 45, (hostId, true), (guestId, false));

        logged.GameNightId.Should().Be(gameNightId);
        logged.Minutes.Should().Be(45);
        var detail = await GetDetailAsync(user, gameNightId);
        detail.Sessions.Should().ContainSingle(x => x.Id == logged.Id && x.PlayerSessions.Count == 2);
        detail.GameNight.Summary.GamesPlayed.Should().Be(1);
        detail.GameNight.Summary.TotalMinutes.Should().Be(45);
    }

    [Fact]
    public async Task TheNightWinner_ShouldBeShared_WhenPlayersWonAsOften()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, gameId, hostId, guestId) = await CreateNightAsync(user);

        await LogAsync(user, gameNightId, gameId, DateTime.UtcNow.AddHours(-2), 30, (hostId, true), (guestId, false));
        await LogAsync(user, gameNightId, gameId, DateTime.UtcNow.AddHours(-1), 30, (hostId, false), (guestId, true));

        var tied = (await GetDetailAsync(user, gameNightId)).GameNight.Summary;
        tied.WinnerIds.Should().BeEquivalentTo([hostId, guestId]);
        tied.MostWins.Should().Be(1);

        await LogAsync(user, gameNightId, gameId, DateTime.UtcNow.AddMinutes(-40), 30, (hostId, false), (guestId, true));

        var decided = (await GetDetailAsync(user, gameNightId)).GameNight.Summary;
        decided.WinnerIds.Should().Equal(guestId);
        decided.MostWins.Should().Be(2);
        decided.GamesPlayed.Should().Be(3);
    }

    [Fact]
    public async Task DeletingTheNight_ShouldKeepItsSessions_AndDropItsRunningGames()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, gameId, hostId, _) = await CreateNightAsync(user);

        var logged = await user.PostAsJsonAsync("/api/session", new CreateSessionCommand
        {
            GameId = gameId,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 40,
            GameNightId = gameNightId,
            PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = hostId, Won = true }],
        });
        logged.StatusCode.Should().Be(HttpStatusCode.Created, await logged.Content.ReadAsStringAsync());
        var session = (await logged.Content.ReadFromJsonAsync<SessionDto>(IntegrationFixture.Json))!;
        var started = await user.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", new StartRunningGameCommand { GameId = gameId, PlayerIds = [hostId] });
        var runningGame = (await started.Content.ReadFromJsonAsync<RunningGameDto>(IntegrationFixture.Json))!;

        (await user.DeleteAsync($"/api/gamenight/{gameNightId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var kept = await user.GetFromJsonAsync<SessionDto>($"/api/session/{session.Id}", IntegrationFixture.Json);
        kept!.GameNightId.Should().BeNull();
        (await user.DeleteAsync($"/api/gamenight/running-games/{runningGame.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EndingTheNight_ShouldWaitForRunningGames_AndCloseItUntilReopened()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, gameId, hostId, _) = await CreateNightAsync(user);
        var start = new StartRunningGameCommand { GameId = gameId, PlayerIds = [hostId] };
        var started = await user.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", start);
        var runningGame = (await started.Content.ReadFromJsonAsync<RunningGameDto>(IntegrationFixture.Json))!;

        (await user.PostAsync($"/api/gamenight/{gameNightId}/end", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await user.DeleteAsync($"/api/gamenight/running-games/{runningGame.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await user.PostAsync($"/api/gamenight/{gameNightId}/end", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetDetailAsync(user, gameNightId)).GameNight.EndedAt.Should().NotBeNull();
        (await user.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", start)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var logged = await user.PostAsJsonAsync("/api/session", new CreateSessionCommand
        {
            GameId = gameId,
            Start = DateTime.UtcNow.AddHours(-1),
            Minutes = 30,
            GameNightId = gameNightId,
            PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = hostId }],
        });
        logged.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await user.PostAsync($"/api/gamenight/{gameNightId}/reopen", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetDetailAsync(user, gameNightId)).GameNight.EndedAt.Should().BeNull();
        (await user.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", start)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await user.PostAsync("/api/gamenight/999999/end", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PlayingOnANightAWeekAway_ShouldBeRefused_UntilTheNightOpens()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, gameId, hostId, _) = await CreateNightAsync(user, DateTime.UtcNow.AddDays(7));

        var started = await user.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", new StartRunningGameCommand { GameId = gameId, PlayerIds = [hostId] });
        var logged = await user.PostAsJsonAsync("/api/session", new CreateSessionCommand
        {
            GameId = gameId,
            Start = DateTime.UtcNow.AddHours(-1),
            Minutes = 30,
            GameNightId = gameNightId,
            PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = hostId }],
        });
        var ended = await user.PostAsync($"/api/gamenight/{gameNightId}/end", null);

        started.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await started.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.GameNightNotStarted);
        logged.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await logged.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.GameNightNotStarted);
        ended.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ended.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.GameNightNotStarted);
        var detail = await GetDetailAsync(user, gameNightId);
        detail.GameNight.EndedAt.Should().BeNull();
        detail.RunningGames.Should().BeEmpty();
        detail.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task FinishingAGame_ShouldBeRefused_WhenTheNightWasMovedAWeekAhead()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, gameId, hostId, guestId) = await CreateNightAsync(user);
        var started = await user.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", new StartRunningGameCommand { GameId = gameId, PlayerIds = [hostId] });
        var runningGame = (await started.Content.ReadFromJsonAsync<RunningGameDto>(IntegrationFixture.Json))!;
        var night = (await GetDetailAsync(user, gameNightId)).GameNight;
        var moved = await user.PutAsJsonAsync("/api/gamenight", new UpdateGameNightCommand
        {
            Id = gameNightId,
            Title = night.Title,
            StartDate = DateTime.UtcNow.AddDays(7),
            HostId = hostId,
            LocationId = night.LocationId,
            InvitedPlayerIds = [guestId],
        });
        moved.StatusCode.Should().Be(HttpStatusCode.OK, await moved.Content.ReadAsStringAsync());

        var finished = await user.PostAsJsonAsync($"/api/gamenight/running-games/{runningGame.Id}/finish", new FinishRunningGameCommand
        {
            PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = hostId }],
        });

        finished.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await finished.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.GameNightNotStarted);
        (await GetDetailAsync(user, gameNightId)).RunningGames.Should().ContainSingle(x => x.Id == runningGame.Id);
    }

    [Fact]
    public async Task StartingAGame_ShouldBeRefused_ForAReader()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        using var reader = await _fixture.CreateClientAsAsync("reader");
        var (gameNightId, gameId, hostId, _) = await CreateNightAsync(user);

        var response = await reader.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", new StartRunningGameCommand { GameId = gameId, PlayerIds = [hostId] });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await reader.GetAsync($"/api/gamenight/{gameNightId}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TheTable_ShouldFollowWhoPlayed_PlusTheHost_AndAttendanceOverrides()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, gameId, hostId, guestId) = await CreateNightAsync(user);
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var declinedId = await CreateAsync<PlayerDto>(user, "/api/player", new CreatePlayerCommand { Name = "Bram " + suffix }, x => x.Id);
        var watcherId = await CreateAsync<PlayerDto>(user, "/api/player", new CreatePlayerCommand { Name = "Lotte " + suffix }, x => x.Id);

        (await GetDetailAsync(user, gameNightId)).GameNight.Summary.AttendeeIds.Should().Equal(hostId);

        var started = await user.PostAsJsonAsync($"/api/gamenight/{gameNightId}/running-games", new StartRunningGameCommand { GameId = gameId, PlayerIds = [declinedId] });
        started.StatusCode.Should().Be(HttpStatusCode.Created, await started.Content.ReadAsStringAsync());
        await LogAsync(user, gameNightId, gameId, DateTime.UtcNow.AddHours(-2), 40, (guestId, true));

        var playing = await GetDetailAsync(user, gameNightId);
        playing.GameNight.Summary.AttendeeIds.Should().Equal(hostId, guestId, declinedId);
        playing.GameNight.Summary.RunningGames.Should().Be(1);
        playing.GameNight.Summary.AttendanceRecorded.Should().BeFalse();

        var marked = await user.PutAsJsonAsync($"/api/gamenight/{gameNightId}/attendance", new UpdateAttendanceCommand { PresentPlayerIds = [guestId, declinedId, watcherId] });
        marked.StatusCode.Should().Be(HttpStatusCode.NoContent, await marked.Content.ReadAsStringAsync());

        var after = await GetDetailAsync(user, gameNightId);
        after.GameNight.Summary.AttendeeIds.Should().BeEquivalentTo([guestId, declinedId, watcherId]);
        after.GameNight.Summary.AttendanceRecorded.Should().BeTrue();
        var listed = await user.GetFromJsonAsync<List<GameNightDto>>("/api/gamenight", IntegrationFixture.Json);
        listed!.Single(x => x.Id == gameNightId).Summary.AttendeeIds.Should().BeEquivalentTo([guestId, declinedId, watcherId]);

        (await user.PutAsJsonAsync($"/api/gamenight/{gameNightId}/attendance", new UpdateAttendanceCommand { PresentPlayerIds = [999999] })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await user.PutAsJsonAsync("/api/gamenight/999999/attendance", new UpdateAttendanceCommand { PresentPlayerIds = [hostId] })).StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var reader = await _fixture.CreateClientAsAsync("reader");
        (await reader.PutAsJsonAsync($"/api/gamenight/{gameNightId}/attendance", new UpdateAttendanceCommand { PresentPlayerIds = [hostId] })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MarkingAttendance_ShouldBeRefused_ForANightThatHasNotOpened()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, _, hostId, _) = await CreateNightAsync(user, DateTime.UtcNow.AddDays(7));

        var response = await user.PutAsJsonAsync($"/api/gamenight/{gameNightId}/attendance", new UpdateAttendanceCommand { PresentPlayerIds = [hostId] });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReopeningANight_ShouldBeRefused_OnceItsPlayWindowHasPassed()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var (gameNightId, _, _, _) = await CreateNightAsync(user, DateTime.UtcNow.AddHours(-14));
        (await user.PostAsync($"/api/gamenight/{gameNightId}/end", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await user.PostAsync($"/api/gamenight/{gameNightId}/reopen", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetDetailAsync(user, gameNightId)).GameNight.EndedAt.Should().NotBeNull();
    }

    private static async Task<GameNightDetailDto> GetDetailAsync(HttpClient user, int gameNightId)
    {
        var response = await user.GetAsync($"/api/gamenight/{gameNightId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<GameNightDetailDto>(IntegrationFixture.Json))!;
    }

    private static async Task<SessionDto> LogAsync(HttpClient user, int gameNightId, int gameId, DateTime start, int minutes, params (int PlayerId, bool Won)[] players)
    {
        var response = await user.PostAsJsonAsync("/api/session", new CreateSessionCommand
        {
            GameId = gameId,
            Start = start,
            Minutes = minutes,
            GameNightId = gameNightId,
            PlayerSessions = players.Select(x => new CreatePlayerSessionCommand { PlayerId = x.PlayerId, Won = x.Won }).ToList(),
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SessionDto>(IntegrationFixture.Json))!;
    }

    private static async Task<int> CreateAsync<TDto>(HttpClient user, string url, object command, Func<TDto, int> id)
    {
        var response = await user.PostAsJsonAsync(url, command);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return id((await response.Content.ReadFromJsonAsync<TDto>(IntegrationFixture.Json))!);
    }

    private static async Task<(int GameNightId, int GameId, int HostId, int GuestId)> CreateNightAsync(HttpClient user, DateTime? startDate = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var hostId = await CreateAsync<PlayerDto>(user, "/api/player", new CreatePlayerCommand { Name = "Host " + suffix }, x => x.Id);
        var guestId = await CreateAsync<PlayerDto>(user, "/api/player", new CreatePlayerCommand { Name = "Guest " + suffix }, x => x.Id);
        var locationId = await CreateAsync<LocationDto>(user, "/api/location", new CreateLocationCommand { Name = "Table " + suffix }, x => x.Id);
        var gameId = await CreateAsync<GameDto>(user, "/api/game", new CreateGameCommand { Title = "Azul " + suffix, State = GameState.Owned, HasScoring = true }, x => x.Id);
        var gameNightId = await CreateAsync<GameNightDto>(user, "/api/gamenight", new CreateGameNightCommand
        {
            Title = "Strategy night",
            StartDate = startDate ?? DateTime.UtcNow.AddHours(-3),
            HostId = hostId,
            LocationId = locationId,
            InvitedPlayerIds = [guestId],
        }, x => x.Id);

        return (gameNightId, gameId, hostId, guestId);
    }
}
