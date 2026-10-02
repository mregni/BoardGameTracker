using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class PipelineTests
{
    private readonly IntegrationFixture _fixture;

    public PipelineTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EveryController_ShouldResolveFromTheContainer()
    {
        var controllerTypes = typeof(BoardGameTracker.Api.Controllers.GameController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
        await using var scope = _fixture.CreateScope();

        var unresolved = controllerTypes
            .Where(t => ActivatorUtilities.CreateInstance(scope.ServiceProvider, t) is null)
            .ToList();

        controllerTypes.Should().NotBeEmpty();
        unresolved.Should().BeEmpty();
    }

    [Fact]
    public void EveryJsonAction_ShouldDeclareItsResponseType()
    {
        var document = _fixture.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        var undeclared = document.Paths!
            .SelectMany(path => path.Value.Operations!.Select(operation => (Path: path.Key, Method: operation.Key, Operation: operation.Value)))
            .Where(x => x.Operation.Responses!.TryGetValue("200", out var ok) && (ok.Content == null || ok.Content.Count == 0))
            .Select(x => $"{x.Method.Method} {x.Path}")
            .ToList();

        undeclared.Should().BeEquivalentTo(
            "GET /api/manual/{id}/download",
            "GET /api/manual/{id}/page/{page}/image",
            "GET /api/manual/gamenight/{linkId}/manual/{manualId}/download");
    }

    [Fact]
    public async Task ModelValidation_ShouldReturnProblemDetails_WithTheFieldErrors()
    {
        using var admin = await _fixture.CreateAdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/game", new CreateGameCommand { Title = "Half range", State = GameState.Owned, MinPlayers = 2 });
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(IntegrationFixture.Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        problem!.Errors.Keys.Select(k => k.ToLowerInvariant()).Should().Contain(["minplayers", "maxplayers"]);
        problem.Errors.Values.SelectMany(v => v).Should().Contain(Constants.Errors.PlayerRangeIncomplete);
    }

    [Theory]
    [InlineData("/api/game/0")]
    [InlineData("/api/player/-1")]
    public async Task ValidateIdFilter_ShouldReject_NonPositiveIds(string path)
    {
        using var admin = await _fixture.CreateAdminClientAsync();

        var response = await admin.GetAsync(path);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(IntegrationFixture.Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem!.Title.Should().Contain("Must be greater than 0");
    }

    [Fact]
    public async Task UnhandledNotFound_ShouldReturnProblemDetails_WithTraceId()
    {
        using var admin = await _fixture.CreateAdminClientAsync();

        var response = await admin.GetAsync("/api/game/999999");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task LoginRateLimiter_ShouldAnswer429_WithRetryAfter_AfterTenAttempts()
    {
        using var client = _fixture.CreateClient();
        var responses = new List<HttpResponseMessage>();

        for (var attempt = 0; attempt < 11; attempt++)
        {
            responses.Add(await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody-" + attempt, "wrong-password")));
        }

        responses.Take(10).Select(x => x.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
        responses[10].StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        responses[10].Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginRateLimiter_ShouldIgnoreAForgedXForwardedFor_WhenNoProxyIsTrusted()
    {
        using var client = _fixture.CreateClient();
        HttpResponseMessage? limited = null;

        for (var attempt = 0; attempt < 12 && limited == null; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new LoginRequest("spoofer-" + attempt, "wrong-password")),
            };
            request.Headers.Add("X-Forwarded-For", $"198.51.100.{attempt + 1}");
            var response = await client.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                limited = response;
            }
        }

        limited.Should().NotBeNull("a changing X-Forwarded-For must not open a new limiter partition");
    }

    [Fact]
    public async Task CreateGame_ShouldRoundTrip_ThroughTheRealPipeline()
    {
        using var admin = await _fixture.CreateAdminClientAsync();

        var created = await admin.PostAsJsonAsync("/api/game", new CreateGameCommand
        {
            Title = "Pipeline game",
            State = GameState.Owned,
            HasScoring = true,
            MinPlayers = 2,
            MaxPlayers = 4,
            BuyingPrice = 39.99m,
        });
        var dto = await created.Content.ReadFromJsonAsync<GameDto>(IntegrationFixture.Json);
        var fetched = await admin.GetFromJsonAsync<GameDto>($"/api/game/{dto!.Id}", IntegrationFixture.Json);

        created.StatusCode.Should().Be(HttpStatusCode.Created);
        fetched!.Title.Should().Be("Pipeline game");
        fetched.MinPlayers.Should().Be(2);
        fetched.MaxPlayers.Should().Be(4);
        fetched.BuyingPrice.Should().Be(39.99m);
        fetched.State.Should().Be(GameState.Owned);
    }
}
