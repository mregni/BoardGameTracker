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
public class StorageTests
{
    private readonly IntegrationFixture _fixture;

    public StorageTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    private static async Task<T> Read<T>(HttpResponseMessage response, HttpStatusCode expected)
    {
        response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(IntegrationFixture.Json))!;
    }

    [Fact]
    public async Task Storage_ShouldMoveGamesUp_AndLetThemGoWithTheirLocation()
    {
        using var user = await _fixture.CreateClientAsAsync("user");
        var location = await Read<LocationDto>(await user.PostAsJsonAsync("/api/location", new CreateLocationCommand { Name = $"Trailer {Guid.NewGuid():N}" }), HttpStatusCode.Created);
        var room = await Read<StorageLocationDto>(await user.PostAsJsonAsync("/api/storage", new CreateStorageLocationCommand { Name = "Bench", LocationId = location.Id, Layout = StorageLayout.Free }), HttpStatusCode.Created);
        var kallax = await Read<StorageLocationDto>(await user.PostAsJsonAsync("/api/storage", new CreateStorageLocationCommand
        {
            Name = "Kallax", LocationId = location.Id, ParentId = room.Id, Layout = StorageLayout.Grid, Rows = 2, Columns = 2
        }), HttpStatusCode.Created);

        var nodes = await user.GetFromJsonAsync<List<StorageLocationDto>>("/api/storage", IntegrationFixture.Json);
        var cell = nodes!.Single(x => x.ParentId == kallax.Id && x.Row == 2 && x.Column == 2);
        nodes!.Count(x => x.ParentId == kallax.Id).Should().Be(4);

        var game = await Read<GameDto>(await user.PostAsJsonAsync("/api/game", new CreateGameCommand { Title = "Stored", State = GameState.Owned }), HttpStatusCode.Created);
        var assigned = await user.PutAsJsonAsync("/api/storage/assign", new AssignStorageCommand
        {
            StorageLocationId = cell.Id,
            Items = [new StorageItemReference { Type = StorageItemType.Game, Id = game.Id }]
        });
        assigned.StatusCode.Should().Be(HttpStatusCode.NoContent, await assigned.Content.ReadAsStringAsync());
        (await user.GetFromJsonAsync<LocationDto>($"/api/location/{location.Id}", IntegrationFixture.Json))!.StoredCount.Should().Be(1);

        var deleted = await user.DeleteAsync($"/api/storage/{kallax.Id}");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var moved = await user.GetFromJsonAsync<GameDto>($"/api/game/{game.Id}", IntegrationFixture.Json);
        moved!.StorageLocationId.Should().Be(room.Id);
        moved.StoredAtLocationId.Should().Be(location.Id);
        (await user.GetFromJsonAsync<List<StorageLocationDto>>("/api/storage", IntegrationFixture.Json))!.Should().NotContain(x => x.Id == kallax.Id || x.Id == cell.Id);

        (await user.DeleteAsync($"/api/location/{location.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var homeless = await user.GetFromJsonAsync<GameDto>($"/api/game/{game.Id}", IntegrationFixture.Json);
        homeless!.StoredAtLocationId.Should().BeNull();
        homeless.StorageLocationId.Should().BeNull();
        (await user.GetFromJsonAsync<List<StorageLocationDto>>("/api/storage", IntegrationFixture.Json))!.Should().NotContain(x => x.Id == room.Id);
    }

    [Fact]
    public async Task Storage_ShouldRequireWriteAccess()
    {
        using var reader = await _fixture.CreateClientAsAsync("reader");

        (await reader.GetAsync("/api/storage")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await reader.PostAsJsonAsync("/api/storage", new CreateStorageLocationCommand { Name = "Box", LocationId = 1 })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await reader.PutAsJsonAsync("/api/storage/assign", new AssignStorageCommand())).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await reader.DeleteAsync("/api/storage/1")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
