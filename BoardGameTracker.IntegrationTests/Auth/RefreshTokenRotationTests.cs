using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public class RefreshTokenRotationTests
{
    private const string Username = "refresh-rotation";
    private readonly IntegrationFixture _fixture;

    public RefreshTokenRotationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Refresh_ShouldRotate_IssueTheImageCookie_AndRevokeTheWholeFamilyWhenAnOldTokenIsReplayed()
    {
        using (var admin = await _fixture.CreateAdminClientAsync())
        {
            var register = await admin.PostAsJsonAsync("/api/auth/register", new RegisterRequest(Username, $"{Username}@example.com", IntegrationFixture.UserPassword, Constants.AuthRoles.User), TestContext.Current.CancellationToken);
            register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        using var client = _fixture.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(Username, IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var first = (await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;

        var refreshed = await RefreshAsync(client, first.RefreshToken);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshed.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{ProfileImageCookie.Name}=", StringComparison.Ordinal));
        var second = (await refreshed.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
        second.RefreshToken.Should().NotBe(first.RefreshToken);
        second.User.Username.Should().Be(Username);

        (await RefreshAsync(client, first.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var third = await RefreshAsync(client, second.RefreshToken);
        third.StatusCode.Should().Be(HttpStatusCode.OK, "a replay within the grace period is a concurrent refresh, not a theft");
        var latest = (await third.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;

        await using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE auth."RefreshTokens" SET "RevokedAt" = "RevokedAt" - interval '5 minutes'
                WHERE "UserId" = {first.User.Id} AND "ReplacedByToken" IS NOT NULL
                """, TestContext.Current.CancellationToken);
        }

        (await RefreshAsync(client, first.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(client, latest.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldEndTheTokenTheSessionWasRefreshedInto_EvenWhenSentTheOldToken()
    {
        using var client = _fixture.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var first = (await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
        var refreshed = await RefreshAsync(client, first.RefreshToken);
        var second = (await refreshed.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout")
        {
            Content = JsonContent.Create(new LogoutRequest(first.RefreshToken)),
        };
        logout.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", second.AccessToken);
        (await client.SendAsync(logout, TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await RefreshAsync(client, second.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldHandOutOneSuccessor_WhenTheSameTokenIsUsedTwiceAtOnce()
    {
        using var client = _fixture.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var session = (await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;

        var results = await Task.WhenAll(RefreshAsync(client, session.RefreshToken), RefreshAsync(client, session.RefreshToken));

        results.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Unauthorized]);
    }

    private static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken) =>
        client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(refreshToken), TestContext.Current.CancellationToken);
}
