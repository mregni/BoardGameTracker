using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
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

    private static async Task<GameNightDto> CreateGameNightAsync(HttpClient user)
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
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        return (await created.Content.ReadFromJsonAsync<GameNightDto>(IntegrationFixture.Json))!;
    }
}
