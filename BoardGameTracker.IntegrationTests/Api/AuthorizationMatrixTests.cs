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
public class AuthorizationMatrixTests
{
    private readonly IntegrationFixture _fixture;

    public AuthorizationMatrixTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("GET", "/api/game")]
    [InlineData("GET", "/api/player")]
    [InlineData("GET", "/api/dashboard/statistics")]
    [InlineData("PUT", "/api/settings")]
    public async Task Anonymous_ShouldGet401_OnProtectedEndpoints(string method, string path)
    {
        using var client = _fixture.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/settings")]
    [InlineData("/api/auth/status")]
    [InlineData("/api/health")]
    public async Task Anonymous_ShouldGet200_OnPublicEndpoints(string path)
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reader_ShouldRead_ButNotWrite()
    {
        using var reader = await _fixture.CreateClientAsAsync("reader");

        var list = await reader.GetAsync("/api/game");
        var create = await reader.PostAsJsonAsync("/api/game", NewGame("Reader game"));

        list.StatusCode.Should().Be(HttpStatusCode.OK);
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User_ShouldCreateGames_ButNotChangeSettings()
    {
        using var user = await _fixture.CreateClientAsAsync("user");

        var create = await user.PostAsJsonAsync("/api/game", NewGame("User game"));
        var settings = await user.PutAsJsonAsync("/api/settings", new UIResourceDto());

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        settings.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_ShouldReadEnvironment_WhileUserGets403()
    {
        using var admin = await _fixture.CreateAdminClientAsync();
        using var user = await _fixture.CreateClientAsAsync("user");

        var adminResponse = await admin.GetAsync("/api/settings/environment");
        var userResponse = await user.GetAsync("/api/settings/environment");

        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        userResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnknownApiRoute_ShouldGet404_NotTheSpaFallback()
    {
        using var admin = await _fixture.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static CreateGameCommand NewGame(string title) => new() { Title = title, State = GameState.Owned, HasScoring = true };
}
