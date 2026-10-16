using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class IntegrationStatusTests
{
    private const string StatusPath = "/api/settings/integrations/status";
    private const string BggTestPath = "/api/settings/integrations/bgg/test";
    private const string RejectedKey = "integration-rejected-bgg-key";

    private readonly IntegrationFixture _fixture;

    public IntegrationStatusTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("GET", StatusPath)]
    [InlineData("POST", BggTestPath)]
    public async Task Anonymous_ShouldGet401(string method, string path)
    {
        using var client = _fixture.CreateClient();

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("reader", "GET", StatusPath)]
    [InlineData("reader", "POST", BggTestPath)]
    [InlineData("user", "GET", StatusPath)]
    [InlineData("user", "POST", BggTestPath)]
    public async Task NonAdmins_ShouldGet403(string username, string method, string path)
    {
        using var client = await _fixture.CreateClientAsAsync(username);

        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_ShouldSeeEveryIntegrationStatus_WithoutTheKeys()
    {
        using var admin = await _fixture.CreateAdminClientAsync();
        try
        {
            await SaveBggKeyAsync(admin, RejectedKey);

            using var rejected = await admin.PostAsync(BggTestPath, null);
            var rejectedBody = await rejected.Content.ReadAsStringAsync();
            using var all = await admin.GetAsync(StatusPath);
            var allBody = await all.Content.ReadAsStringAsync();

            rejected.StatusCode.Should().Be(HttpStatusCode.OK);
            rejectedBody.Should().NotContain(RejectedKey);
            var bgg = await rejected.Content.ReadFromJsonAsync<IntegrationStatusDto>(IntegrationFixture.Json);
            bgg!.Integration.Should().Be(IntegrationKind.Bgg);
            bgg.Status.Should().Be(IntegrationConnectionStatus.Unauthorized);
            bgg.MessageKey.Should().Be("unauthorized");

            all.StatusCode.Should().Be(HttpStatusCode.OK);
            allBody.Should().NotContain(RejectedKey);
            var statuses = (await all.Content.ReadFromJsonAsync<List<IntegrationStatusDto>>(IntegrationFixture.Json))!;
            statuses.Select(s => s.Integration).Should().Equal(IntegrationKind.Bgg, IntegrationKind.ChangeDetection);
            statuses[0].Status.Should().Be(IntegrationConnectionStatus.Unauthorized);
            statuses[1].Status.Should().Be(IntegrationConnectionStatus.NotConfigured);

            await SaveBggKeyAsync(admin, FakeBggHandler.ValidKey);
            using var afterSave = await admin.GetAsync(StatusPath);
            var afterSaveBody = await afterSave.Content.ReadAsStringAsync();

            afterSaveBody.Should().NotContain(FakeBggHandler.ValidKey);
            var refreshed = await afterSave.Content.ReadFromJsonAsync<List<IntegrationStatusDto>>(IntegrationFixture.Json);
            refreshed![0].Status.Should().Be(IntegrationConnectionStatus.Ok);
        }
        finally
        {
            await SaveBggKeyAsync(admin, null);
        }
    }

    [Fact]
    public async Task Admin_ShouldGet404_ForAnUnknownIntegration()
    {
        using var admin = await _fixture.CreateAdminClientAsync();

        using var response = await admin.PostAsync("/api/settings/integrations/unknown/test", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task SaveBggKeyAsync(HttpClient admin, string? key)
    {
        var settings = await admin.GetFromJsonAsync<UIResourceDto>("/api/settings", IntegrationFixture.Json);
        settings!.BggApiKey = key;
        using var response = await admin.PutAsJsonAsync("/api/settings", settings, IntegrationFixture.Json);
        response.EnsureSuccessStatusCode();
    }
}
