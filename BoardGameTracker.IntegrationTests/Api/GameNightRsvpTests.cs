using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Email.Interfaces;
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
    public async Task Rsvp_ShouldStoreMaybe_SeparatelyFromNoAnswer()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        using var anonymous = _fixture.CreateClient();
        var gameNight = await CreateGameNightAsync(user);
        var rsvp = gameNight.InvitedPlayers.Single(x => x.State == GameNightRsvpState.Pending);

        var response = await anonymous.PutAsJsonAsync($"/api/gamenight/link/{gameNight.LinkId}/rsvp", new UpdateRsvpCommand { Id = rsvp.Id, State = GameNightRsvpState.Maybe });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<GameNightRsvpDto>(IntegrationFixture.Json))!.State.Should().Be(GameNightRsvpState.Maybe);
        var detail = await user.GetFromJsonAsync<GameNightDetailDto>($"/api/gamenight/{gameNight.Id}", IntegrationFixture.Json);
        detail!.GameNight.InvitedPlayers.Single(x => x.Id == rsvp.Id).State.Should().Be(GameNightRsvpState.Maybe);
        (await user.GetStringAsync($"/api/gamenight/{gameNight.Id}")).Should().Contain("\"maybe\"");
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

    [Fact]
    public async Task Rsvp_ShouldSayTheHostWasNotified_OnlyWhenAnEmailWentOut()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var email = new RecordingEmailService();
        using var mailing = _fixture.CreateClientWith(services => services.AddSingleton<IEmailService>(email));
        using var silent = _fixture.CreateClient();
        var withEmail = await CreateGameNightAsync(user, hostEmail: $"host-{Guid.NewGuid():N}@example.com");
        var withoutEmail = await CreateGameNightAsync(user);
        var notified = withEmail.InvitedPlayers.Single(x => x.State == GameNightRsvpState.Pending);
        var unnotified = withoutEmail.InvitedPlayers.Single(x => x.State == GameNightRsvpState.Pending);

        var sent = await mailing.PutAsJsonAsync($"/api/gamenight/link/{withEmail.LinkId}/rsvp", new UpdateRsvpCommand { Id = notified.Id, State = GameNightRsvpState.Accepted });
        var noHostEmail = await mailing.PutAsJsonAsync($"/api/gamenight/link/{withoutEmail.LinkId}/rsvp", new UpdateRsvpCommand { Id = unnotified.Id, State = GameNightRsvpState.Maybe });
        var noSmtp = await silent.PutAsJsonAsync($"/api/gamenight/link/{withEmail.LinkId}/rsvp", new UpdateRsvpCommand { Id = notified.Id, State = GameNightRsvpState.Declined });

        sent.StatusCode.Should().Be(HttpStatusCode.OK, await sent.Content.ReadAsStringAsync());
        (await sent.Content.ReadFromJsonAsync<GameNightRsvpUpdateDto>(IntegrationFixture.Json))!.HostNotified.Should().BeTrue();
        (await noHostEmail.Content.ReadFromJsonAsync<GameNightRsvpUpdateDto>(IntegrationFixture.Json))!.HostNotified.Should().BeFalse();
        var declined = (await noSmtp.Content.ReadFromJsonAsync<GameNightRsvpUpdateDto>(IntegrationFixture.Json))!;
        declined.HostNotified.Should().BeFalse();
        declined.State.Should().Be(GameNightRsvpState.Declined);
        email.Recipients.Should().ContainSingle().Which.Should().StartWith("host-");
    }

    private async Task SetRsvpAuthenticationAsync(bool enabled)
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        await context.Config
            .Where(c => c.Key == Constants.AppConfig.RsvpAuthenticationEnabled)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Value, enabled ? "true" : "false"));
    }

    private static async Task<GameNightDto> CreateGameNightAsync(HttpClient user, int? suggestedGameId = null, string? hostEmail = null)
    {
        var host = await user.PostAsJsonAsync("/api/player", new CreatePlayerCommand { Name = "Host " + Guid.NewGuid().ToString("N")[..6], Email = hostEmail });
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
