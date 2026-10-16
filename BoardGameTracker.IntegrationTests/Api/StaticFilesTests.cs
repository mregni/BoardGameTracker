using System.Net;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class StaticFilesTests
{
    private readonly IntegrationFixture _fixture;

    public StaticFilesTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/games/12")]
    [InlineData("/settings/account")]
    public async Task ClientRoutes_ShouldServeIndexHtml_WithoutCaching(string path)
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
        body.Should().Contain(IntegrationFixture.IndexMarker);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.CacheControl.NoCache.Should().BeTrue();
    }

    [Fact]
    public async Task HashedAssets_ShouldBeImmutable()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/assets/app.js");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be(IntegrationFixture.AssetMarker);
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromDays(365));
        response.Headers.CacheControl.ToString().Should().Contain("immutable");
    }

    [Theory]
    [InlineData("/assets/missing.js")]
    [InlineData("/favicon-does-not-exist.ico")]
    public async Task MissingFiles_ShouldNotFallBackToIndex(string path)
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api/does-not-exist")]
    [InlineData("/api/game/1/nested/unknown")]
    public async Task UnknownApiRoutes_ShouldStay404(string path)
    {
        using var admin = await _fixture.CreateAdminClientAsync();

        var response = await admin.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Should().NotContain(IntegrationFixture.IndexMarker);
    }

    [Fact]
    public async Task HeadRequest_ShouldServeIndexHtml()
    {
        using var client = _fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Head, "/players");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }
}
