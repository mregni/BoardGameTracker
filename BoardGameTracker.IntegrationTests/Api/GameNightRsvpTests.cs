using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class GameNightRsvpTests
{
    private readonly IntegrationFixture _fixture;

    public GameNightRsvpTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Rsvp_ShouldOnlyBeReachableAnonymously_ThroughTheGameNightsOwnLink()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        using var anonymous = _fixture.CreateClient();
        var gameNight = await CreateGameNightAsync(user);
        var rsvp = gameNight.InvitedPlayers.Single(x => x.State == GameNightRsvpState.Pending);
        var accept = new UpdateRsvpCommand { Id = rsvp.Id, State = GameNightRsvpState.Accepted };

        var withoutLink = await anonymous.PutAsJsonAsync("/api/gamenight/rsvp", accept);
        var wrongLink = await anonymous.PutAsJsonAsync($"/api/gamenight/link/{Guid.NewGuid()}/rsvp", accept);
        var rightLink = await anonymous.PutAsJsonAsync($"/api/gamenight/link/{gameNight.LinkId}/rsvp", accept);

        withoutLink.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrongLink.StatusCode.Should().Be(HttpStatusCode.NotFound);
        rightLink.StatusCode.Should().Be(HttpStatusCode.OK, await rightLink.Content.ReadAsStringAsync());
        (await rightLink.Content.ReadFromJsonAsync<GameNightRsvpDto>(IntegrationFixture.Json))!.State.Should().Be(GameNightRsvpState.Accepted);
    }

    [Fact]
    public async Task Rsvp_ShouldNotNeedALink_ForASignedInUser()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var gameNight = await CreateGameNightAsync(user);
        var rsvp = gameNight.InvitedPlayers.Single(x => x.State == GameNightRsvpState.Pending);

        var response = await user.PutAsJsonAsync("/api/gamenight/rsvp", new UpdateRsvpCommand { Id = rsvp.Id, State = GameNightRsvpState.Declined });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<GameNightRsvpDto>(IntegrationFixture.Json))!.State.Should().Be(GameNightRsvpState.Declined);
    }

    [Fact]
    public async Task Link_ShouldHideThePricesOfSuggestedGames_AndGateManualsLikeTheRsvp()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        using var anonymous = _fixture.CreateClient();
        var game = await user.PostAsJsonAsync("/api/game", new CreateGameCommand { Title = "Brass", State = GameState.Owned, HasScoring = true, BuyingPrice = 64.95m, ShopUrl = "https://shop.example.com/brass" });
        game.StatusCode.Should().Be(HttpStatusCode.Created, await game.Content.ReadAsStringAsync());
        var gameId = (await game.Content.ReadFromJsonAsync<GameDto>(IntegrationFixture.Json))!.Id;
        var gameNight = await CreateGameNightAsync(user, gameId);

        var link = await anonymous.GetAsync($"/api/gamenight/link/{gameNight.LinkId}");

        link.StatusCode.Should().Be(HttpStatusCode.OK);
        var suggested = (await link.Content.ReadFromJsonAsync<GameNightDto>(IntegrationFixture.Json))!.SuggestedGames.Should().ContainSingle().Subject;
        suggested.Title.Should().Be("Brass");
        suggested.BuyingPrice.Should().BeNull();
        suggested.ShopUrl.Should().BeNull();
        (await anonymous.GetAsync($"/api/manual/gamenight/{gameNight.LinkId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        await SetRsvpAuthenticationAsync(true);
        try
        {
            (await anonymous.GetAsync($"/api/manual/gamenight/{gameNight.LinkId}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await anonymous.GetAsync($"/api/manual/gamenight/{gameNight.LinkId}/manual/1/download")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await user.GetAsync($"/api/manual/gamenight/{gameNight.LinkId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await SetRsvpAuthenticationAsync(false);
        }
    }

    private async Task SetRsvpAuthenticationAsync(bool enabled)
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        await context.Config
            .Where(c => c.Key == Constants.AppConfig.RsvpAuthenticationEnabled)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Value, enabled ? "true" : "false"));
    }

    private static async Task<GameNightDto> CreateGameNightAsync(HttpClient user, int? suggestedGameId = null)
    {
        var host = await user.PostAsJsonAsync("/api/player", new CreatePlayerCommand { Name = "Host " + Guid.NewGuid().ToString("N")[..6] });
        host.StatusCode.Should().Be(HttpStatusCode.Created, await host.Content.ReadAsStringAsync());
        var hostId = (await host.Content.ReadFromJsonAsync<PlayerDto>(IntegrationFixture.Json))!.Id;

        var guest = await user.PostAsJsonAsync("/api/player", new CreatePlayerCommand { Name = "Guest " + Guid.NewGuid().ToString("N")[..6] });
        var guestId = (await guest.Content.ReadFromJsonAsync<PlayerDto>(IntegrationFixture.Json))!.Id;

        var location = await user.PostAsJsonAsync("/api/location", new CreateLocationCommand { Name = "Table " + Guid.NewGuid().ToString("N")[..6] });
        location.StatusCode.Should().Be(HttpStatusCode.Created, await location.Content.ReadAsStringAsync());
        var locationId = (await location.Content.ReadFromJsonAsync<LocationDto>(IntegrationFixture.Json))!.Id;

        var created = await user.PostAsJsonAsync("/api/gamenight", new CreateGameNightCommand
        {
            Title = "Friday night",
            StartDate = DateTime.UtcNow.AddDays(3),
            HostId = hostId,
            LocationId = locationId,
            InvitedPlayerIds = [guestId],
            SuggestedGameIds = suggestedGameId is { } id ? [id] : [],
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        return (await created.Content.ReadFromJsonAsync<GameNightDto>(IntegrationFixture.Json))!;
    }
}
