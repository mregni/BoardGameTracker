using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
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
public class GameNightSuggestionTests
{
    private readonly IntegrationFixture _fixture;

    public GameNightSuggestionTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddAndRemove_ShouldChangeOnlyThatSuggestion_AndBeIdempotent()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var kept = await CreateGameAsync(user, "Kept " + Suffix());
        var planned = await CreateGameAsync(user, "Planned " + Suffix());
        var night = await CreateGameNightAsync(user, DateTime.UtcNow.AddDays(3), kept);

        var first = await user.PutAsync($"/api/gamenight/{night.Id}/suggestions/{planned}", null);
        var again = await user.PutAsync($"/api/gamenight/{night.Id}/suggestions/{planned}", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        again.StatusCode.Should().Be(HttpStatusCode.OK, await again.Content.ReadAsStringAsync());
        (await again.Content.ReadFromJsonAsync<GameNightDto>(IntegrationFixture.Json))!.SuggestedGames
            .Select(x => x.Id).Should().BeEquivalentTo([kept, planned]);
        (await SuggestedGameIdsAsync(night.Id)).Should().BeEquivalentTo([kept, planned]);

        var removed = await user.DeleteAsync($"/api/gamenight/{night.Id}/suggestions/{planned}");
        var removedAgain = await user.DeleteAsync($"/api/gamenight/{night.Id}/suggestions/{planned}");

        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        removedAgain.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SuggestedGameIdsAsync(night.Id)).Should().Equal(kept);
    }

    [Fact]
    public async Task Add_ShouldRefuseAPastNight_AMissingNight_AndAGameThatIsNotOwned()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var owned = await CreateGameAsync(user, "Owned " + Suffix());
        var wanted = await CreateGameAsync(user, "Wanted " + Suffix(), GameState.Wanted);
        var upcoming = await CreateGameNightAsync(user, DateTime.UtcNow.AddDays(3));
        var past = await CreateGameNightAsync(user, DateTime.UtcNow.AddDays(-3));

        var pastNight = await user.PutAsync($"/api/gamenight/{past.Id}/suggestions/{owned}", null);
        var pastRemove = await user.DeleteAsync($"/api/gamenight/{past.Id}/suggestions/{owned}");
        var missingNight = await user.PutAsync($"/api/gamenight/999999/suggestions/{owned}", null);
        var missingGame = await user.PutAsync($"/api/gamenight/{upcoming.Id}/suggestions/999999", null);
        var notOwned = await user.PutAsync($"/api/gamenight/{upcoming.Id}/suggestions/{wanted}", null);

        pastNight.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await pastNight.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.GameNightOver);
        pastRemove.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        missingNight.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missingGame.StatusCode.Should().Be(HttpStatusCode.NotFound);
        notOwned.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await notOwned.Content.ReadAsStringAsync()).Should().Contain(Constants.Errors.SuggestedGameNotOwned);
        (await SuggestedGameIdsAsync(upcoming.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Suggestions_ShouldBeClosedToReaders()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        using var reader = await _fixture.CreateClientAsAsync("reader");
        var game = await CreateGameAsync(user, "Reader " + Suffix());
        var night = await CreateGameNightAsync(user, DateTime.UtcNow.AddDays(3));

        (await reader.PutAsync($"/api/gamenight/{night.Id}/suggestions/{game}", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await reader.DeleteAsync($"/api/gamenight/{night.Id}/suggestions/{game}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Shames_ShouldShowThePlannedNight_AndTheNextNightInTheStatistics()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var game = await CreateGameAsync(user, "Dusty " + Suffix(), additionDate: DateTime.UtcNow.AddYears(-3), price: 40m);
        var night = await CreateGameNightAsync(user, DateTime.UtcNow.AddMinutes(30));

        await SetConfigAsync(Constants.AppConfig.ShelfOfShameEnabled, "true");
        try
        {
            (await user.PutAsync($"/api/gamenight/{night.Id}/suggestions/{game}", null)).StatusCode.Should().Be(HttpStatusCode.OK);

            var shames = await user.GetFromJsonAsync<List<ShameDto>>("/api/game/shames?months=12", IntegrationFixture.Json);
            var statistics = await user.GetFromJsonAsync<ShameStatisticsDto>("/api/game/shames/statistics?months=1", IntegrationFixture.Json);
            var tooLong = await user.GetAsync("/api/game/shames?months=5000");

            var shame = shames!.Single(x => x.Id == game);
            shame.PlannedGameNightId.Should().Be(night.Id);
            shame.PlannedFor.Should().BeCloseTo(night.StartDate, TimeSpan.FromSeconds(1));
            shame.PlayCount.Should().Be(0);
            shame.MinPlayers.Should().Be(2);
            shame.MaxPlayers.Should().Be(4);
            statistics!.Months.Should().Be(Constants.AppConfig.DefaultShelfOfShameMonths);
            statistics.NextGameNight.Should().NotBeNull();
            statistics.PlannedCount.Should().BeGreaterThanOrEqualTo(1);
            statistics.OwnedCount.Should().BeGreaterThanOrEqualTo(1);
            statistics.CollectionValue.Should().BeGreaterThanOrEqualTo(40m);
            tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        finally
        {
            await SetConfigAsync(Constants.AppConfig.ShelfOfShameEnabled, "false");
        }
    }

    private static string Suffix() => Guid.NewGuid().ToString("N")[..6];

    private async Task<List<int>> SuggestedGameIdsAsync(int gameNightId)
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        return await context.GameNights
            .Where(x => x.Id == gameNightId)
            .SelectMany(x => x.SuggestedGames.Select(g => g.Id))
            .ToListAsync();
    }

    private async Task SetConfigAsync(string key, string value)
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var updated = await context.Config
            .Where(c => c.Key == key)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Value, value));
        if (updated == 0)
        {
            context.Config.Add(new Config { Key = key, Value = value });
            await context.SaveChangesAsync();
        }
    }

    private static async Task<int> CreateGameAsync(HttpClient user, string title, GameState state = GameState.Owned, DateTime? additionDate = null, decimal? price = null)
    {
        var response = await user.PostAsJsonAsync("/api/game", new CreateGameCommand
        {
            Title = title,
            State = state,
            HasScoring = true,
            MinPlayers = 2,
            MaxPlayers = 4,
            AdditionDate = additionDate,
            BuyingPrice = price
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<GameDto>(IntegrationFixture.Json))!.Id;
    }

    private static async Task<GameNightDto> CreateGameNightAsync(HttpClient user, DateTime startDate, params int[] suggestedGameIds)
    {
        var host = await user.PostAsJsonAsync("/api/player", new CreatePlayerCommand { Name = "Host " + Suffix() });
        host.StatusCode.Should().Be(HttpStatusCode.Created, await host.Content.ReadAsStringAsync());
        var hostId = (await host.Content.ReadFromJsonAsync<PlayerDto>(IntegrationFixture.Json))!.Id;

        var location = await user.PostAsJsonAsync("/api/location", new CreateLocationCommand { Name = "Table " + Suffix() });
        location.StatusCode.Should().Be(HttpStatusCode.Created, await location.Content.ReadAsStringAsync());
        var locationId = (await location.Content.ReadFromJsonAsync<LocationDto>(IntegrationFixture.Json))!.Id;

        var created = await user.PostAsJsonAsync("/api/gamenight", new CreateGameNightCommand
        {
            Title = "Night " + Suffix(),
            StartDate = startDate,
            HostId = hostId,
            LocationId = locationId,
            SuggestedGameIds = [.. suggestedGameIds],
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        return (await created.Content.ReadFromJsonAsync<GameNightDto>(IntegrationFixture.Json))!;
    }
}
