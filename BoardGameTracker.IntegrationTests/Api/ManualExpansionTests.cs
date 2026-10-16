using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class ManualExpansionTests
{
    private readonly IntegrationFixture _fixture;

    public ManualExpansionTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ManualExpansion_ShouldBeCreatedListedAndDeleted_WithoutABggId()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var created = await user.PostAsJsonAsync("/api/game", new CreateGameCommand { Title = "Expandable", State = GameState.Owned, HasScoring = false });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var game = await created.Content.ReadFromJsonAsync<GameDto>(IntegrationFixture.Json);

        var added = await user.PostAsJsonAsync($"/api/game/{game!.Id}/expansions/manual", new CreateExpansionCommand { Title = " Promo pack " });
        added.StatusCode.Should().Be(HttpStatusCode.Created, await added.Content.ReadAsStringAsync());
        var expansion = await added.Content.ReadFromJsonAsync<ExpansionDto>(IntegrationFixture.Json);
        expansion!.Title.Should().Be("Promo pack");
        expansion.BggId.Should().BeNull();

        var duplicate = await user.PostAsJsonAsync($"/api/game/{game.Id}/expansions/manual", new CreateExpansionCommand { Title = "promo PACK" });
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await duplicate.Content.ReadFromJsonAsync<ProblemDetails>(IntegrationFixture.Json))!.Title.Should().Be(Constants.Errors.ExpansionAlreadyExists);

        var loaded = await user.GetFromJsonAsync<GameDto>($"/api/game/{game.Id}", IntegrationFixture.Json);
        loaded!.Expansions.Should().ContainSingle(x => x.Title == "Promo pack" && x.BggId == null);

        var deleted = await user.DeleteAsync($"/api/game/{game.Id}/expansion/{expansion.Id}");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await user.GetFromJsonAsync<GameDto>($"/api/game/{game.Id}", IntegrationFixture.Json))!.Expansions.Should().BeEmpty();
    }

    [Fact]
    public async Task ManualExpansion_ShouldRequireWriteAccess()
    {
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var response = await reader.PostAsJsonAsync("/api/game/1/expansions/manual", new CreateExpansionCommand { Title = "Promo" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
