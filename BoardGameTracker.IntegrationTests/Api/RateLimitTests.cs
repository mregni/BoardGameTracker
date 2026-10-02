using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public class RateLimitTests
{
    private const string SharedAddress = "10.250.0.1";
    private readonly IntegrationFixture _fixture;

    public RateLimitTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RulesAssistant_ShouldLimitEachUser_EvenWhenUsersShareAnAddress()
    {
        using (var admin = await _fixture.CreateAdminClientAsync())
        {
            foreach (var name in new[] { "rag-limit-a", "rag-limit-b" })
            {
                var register = await admin.PostAsJsonAsync("/api/auth/register", new RegisterRequest(name, $"{name}@example.com", IntegrationFixture.UserPassword, Constants.AuthRoles.User), TestContext.Current.CancellationToken);
                register.StatusCode.Should().Be(HttpStatusCode.Created);
            }
        }

        using var first = OnSharedAddress(await _fixture.CreateClientAsAsync("rag-limit-a"));
        using var second = OnSharedAddress(await _fixture.CreateClientAsAsync("rag-limit-b"));
        using var anonymous = OnSharedAddress(_fixture.CreateClient());

        for (var i = 0; i < 10; i++)
        {
            (await AskAsync(first)).Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        (await AskAsync(first)).Should().Be(HttpStatusCode.TooManyRequests);
        (await AskAsync(second)).Should().NotBe(HttpStatusCode.TooManyRequests);
        (await AskAsync(anonymous)).Should().Be(HttpStatusCode.Unauthorized);
    }

    private static HttpClient OnSharedAddress(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove(TestClientAddressStartupFilter.HeaderName);
        client.DefaultRequestHeaders.Add(TestClientAddressStartupFilter.HeaderName, SharedAddress);
        return client;
    }

    private static async Task<HttpStatusCode> AskAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/rag/game/1/ask", new { question = "How many cards does each player start with?" }, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }
}
