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
    public async Task Login_ShouldSetAnHttpOnlyLaxRefreshCookieOnTheAuthPath_AndKeepTheTokenOutOfTheBody()
    {
        using var client = _fixture.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie = RefreshSetCookie(login);
        cookie.Should().MatchRegex("^bgt_refresh=[^;]+; max-age=604800; path=/api/auth; samesite=lax; httponly$");
        var body = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("accessToken").And.NotContainEquivalentOf("refreshToken").And.NotContain(CookieValue(cookie));
    }

    [Fact]
    public async Task Refresh_ShouldWorkWithTheCookieAlone_AndRotateIt()
    {
        using var browser = _fixture.CreateBrowserClient();
        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var first = CookieValue(RefreshSetCookie(login));

        var refreshed = await RefreshAsync(browser);

        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = CookieValue(RefreshSetCookie(refreshed));
        rotated.Should().NotBe(first);
        refreshed.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith($"{ProfileImageCookie.Name}=", StringComparison.Ordinal));
        var session = (await refreshed.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
        session.AccessToken.Should().NotBeNullOrEmpty();
        session.User.Username.Should().Be("user");

        (await RefreshAsync(browser)).StatusCode.Should().Be(HttpStatusCode.OK, "the browser now sends the rotated cookie");
    }

    [Fact]
    public async Task Refresh_ShouldBeRefused_WithoutTheSessionHeader_OrWithoutACookie()
    {
        using var browser = _fixture.CreateBrowserClient();
        await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);

        var withoutHeader = await browser.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken);
        withoutHeader.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await withoutHeader.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(Constants.Errors.MissingSessionHeader);

        using var anonymous = _fixture.CreateClient();
        var withoutCookie = await RefreshAsync(anonymous);
        withoutCookie.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await withoutCookie.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(Constants.Errors.InvalidRefreshToken);
    }

    [Fact]
    public async Task Refresh_ShouldContinueAfterALostResponse_ButRevokeTheWholeFamilyOnceALaterTokenWasUsed()
    {
        using (var admin = await _fixture.CreateAdminClientAsync())
        {
            var register = await admin.PostAsJsonAsync("/api/auth/register", new RegisterRequest(Username, $"{Username}@example.com", IntegrationFixture.UserPassword, Constants.AuthRoles.User), TestContext.Current.CancellationToken);
            register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        using var client = _fixture.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(Username, IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var first = CookieValue(RefreshSetCookie(login));

        var refreshed = await RefreshWithAsync(first);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        var lost = CookieValue(RefreshSetCookie(refreshed));
        lost.Should().NotBe(first);

        var replay = await RefreshWithAsync(first);
        replay.StatusCode.Should().Be(HttpStatusCode.OK, "the successor was never used, so the first response was lost");
        var continued = CookieValue(RefreshSetCookie(replay));
        continued.Should().NotBe(lost);
        var lateCopy = await RefreshWithAsync(lost);
        lateCopy.StatusCode.Should().Be(HttpStatusCode.OK, "a late copy of the lost response still continues the session");
        var tip = CookieValue(RefreshSetCookie(lateCopy));

        var used = await RefreshWithAsync(tip);
        used.StatusCode.Should().Be(HttpStatusCode.OK);
        var latest = CookieValue(RefreshSetCookie(used));

        var theft = await RefreshWithAsync(first);
        theft.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "a later token of this session was used, so the old token is stolen");
        (await theft.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(Constants.Errors.InvalidRefreshToken);
        (await RefreshWithAsync(latest)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "replaying a used token ends every session");
        (await RefreshWithAsync(continued)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldKeepTheBrowserSignedIn_WhenAReloadThrewAwayTheRefreshResponse()
    {
        const string username = "refresh-reload";
        var userId = await RegisterAsync(username);

        using var otherDevice = _fixture.CreateClient();
        var otherLogin = await otherDevice.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var otherSession = CookieValue(RefreshSetCookie(otherLogin));

        using var browser = _fixture.CreateBrowserClient();
        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var stale = CookieValue(RefreshSetCookie(login));

        var aborted = await RefreshWithAsync(stale);
        aborted.StatusCode.Should().Be(HttpStatusCode.OK, "the server rotated the token, but the reload threw this response away");

        var reload = await RefreshAsync(browser);
        reload.StatusCode.Should().Be(HttpStatusCode.OK, await reload.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        CookieValue(RefreshSetCookie(reload)).Should().NotBe(stale);
        (await RefreshAsync(browser)).StatusCode.Should().Be(HttpStatusCode.OK, "the browser now holds a working cookie");

        await AgeRotationsAsync(userId);

        (await RefreshWithAsync(otherSession)).StatusCode.Should().Be(HttpStatusCode.OK, "a lost response never touches other devices");
        (await RefreshAsync(browser)).StatusCode.Should().Be(HttpStatusCode.OK, "the age of a rotation does not matter");
    }

    [Fact]
    public async Task Refresh_ShouldKeepBothTabsSignedIn_WhenTheyRefreshAtTheSameMoment()
    {
        const string username = "refresh-two-tabs";
        await RegisterAsync(username);

        using var browser = _fixture.CreateBrowserClient();
        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        for (var round = 0; round < 5; round++)
        {
            var tabs = await Task.WhenAll(RefreshLikeTheAppAsync(browser), RefreshLikeTheAppAsync(browser));

            tabs.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
            (await RefreshAsync(browser)).StatusCode.Should().Be(HttpStatusCode.OK, "the cookie jar holds a working token after both tabs refreshed");
        }
    }

    private static async Task<HttpResponseMessage> RefreshLikeTheAppAsync(HttpClient browser)
    {
        var first = await RefreshAsync(browser);
        if (first.StatusCode != HttpStatusCode.Unauthorized)
        {
            return first;
        }

        var body = await first.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain(Constants.Errors.RefreshTokenSuperseded);
        first.Headers.Contains("Set-Cookie").Should().BeFalse("losing the race must not delete the cookie the winner just set");
        await Task.Delay(300, TestContext.Current.CancellationToken);
        return await RefreshAsync(browser);
    }

    [Fact]
    public async Task Refresh_ShouldStillEndEverySession_WhenAStolenTokenIsReplayedAfterTheThiefUsedItsSuccessor()
    {
        const string username = "refresh-theft";
        var userId = await RegisterAsync(username);

        using var otherDevice = _fixture.CreateClient();
        var otherLogin = await otherDevice.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var otherSession = CookieValue(RefreshSetCookie(otherLogin));

        using var victim = _fixture.CreateBrowserClient();
        var login = await victim.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var stolen = CookieValue(RefreshSetCookie(login));

        var thiefFirst = await RefreshWithAsync(stolen);
        var thiefSecond = await RefreshWithAsync(CookieValue(RefreshSetCookie(thiefFirst)));
        thiefSecond.StatusCode.Should().Be(HttpStatusCode.OK);
        var thiefLatest = CookieValue(RefreshSetCookie(thiefSecond));

        await AgeRotationsAsync(userId);

        var victimReload = await RefreshAsync(victim);
        victimReload.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        RefreshSetCookie(victimReload).Should().StartWith("bgt_refresh=;");
        (await RefreshWithAsync(thiefLatest)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the thief's chain is revoked");
        (await RefreshWithAsync(otherSession)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "real reuse still ends every session");
    }

    private async Task<string> RegisterAsync(string username)
    {
        using var admin = await _fixture.CreateAdminClientAsync();
        var register = await admin.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, $"{username}@example.com", IntegrationFixture.UserPassword, Constants.AuthRoles.User), TestContext.Current.CancellationToken);
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return (await register.Content.ReadFromJsonAsync<UserDto>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!.Id;
    }

    private async Task AgeRotationsAsync(string userId)
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE auth."RefreshTokens" SET "RevokedAt" = "RevokedAt" - interval '5 minutes'
            WHERE "UserId" = {userId} AND "ReplacedByToken" IS NOT NULL
            """, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Logout_ShouldRevokeTheCookieToken_AndClearTheCookie_EvenWithoutAnAccessToken()
    {
        using var browser = _fixture.CreateBrowserClient();
        var login = await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var issued = CookieValue(RefreshSetCookie(login));

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add(RequireSessionHeaderAttribute.HeaderName, RequireSessionHeaderAttribute.HeaderValue);
        var loggedOut = await browser.SendAsync(logout, TestContext.Current.CancellationToken);

        loggedOut.StatusCode.Should().Be(HttpStatusCode.NoContent);
        RefreshSetCookie(loggedOut).Should().StartWith("bgt_refresh=;").And.Contain("expires=Thu, 01 Jan 1970").And.Contain("path=/api/auth;");
        (await RefreshAsync(browser)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the browser dropped the cookie");
        (await RefreshWithAsync(issued)).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the token was revoked server-side");
    }

    [Fact]
    public async Task Logout_ShouldEndTheTokenTheSessionWasRefreshedInto_EvenWhenSentTheOldToken()
    {
        using var client = _fixture.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var first = CookieValue(RefreshSetCookie(login));
        var refreshed = await RefreshWithAsync(first);
        var second = CookieValue(RefreshSetCookie(refreshed));
        var session = (await refreshed.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;

        using var raw = _fixture.CreateClient();
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logout.Headers.Add(RequireSessionHeaderAttribute.HeaderName, RequireSessionHeaderAttribute.HeaderValue);
        logout.Headers.Add("Cookie", $"{AuthSessionCookies.RefreshCookieName}={first}");
        logout.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);
        (await raw.SendAsync(logout, TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await RefreshWithAsync(second)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldBeRefused_WithoutTheSessionHeader()
    {
        using var browser = _fixture.CreateBrowserClient();
        await browser.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);

        var logout = await browser.PostAsync("/api/auth/logout", null, TestContext.Current.CancellationToken);

        logout.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        logout.Headers.Contains("Set-Cookie").Should().BeFalse();
        (await RefreshAsync(browser)).StatusCode.Should().Be(HttpStatusCode.OK, "the session survived the forged logout");
    }

    [Fact]
    public async Task Refresh_ShouldNeverEndTheSession_WhenTheSameTokenIsUsedTwiceAtOnce()
    {
        using var client = _fixture.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("user", IntegrationFixture.UserPassword), TestContext.Current.CancellationToken);
        var token = CookieValue(RefreshSetCookie(login));

        var results = await Task.WhenAll(RefreshWithAsync(token), RefreshWithAsync(token));

        results.Should().Contain(r => r.StatusCode == HttpStatusCode.OK);
        foreach (var loser in results.Where(r => r.StatusCode != HttpStatusCode.OK))
        {
            loser.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await loser.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(Constants.Errors.RefreshTokenSuperseded, "the losing tab is told to retry");
            loser.Headers.Contains("Set-Cookie").Should().BeFalse();
        }

        var latest = CookieValue(RefreshSetCookie(results.Last(r => r.StatusCode == HttpStatusCode.OK)));
        (await RefreshWithAsync(latest)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string RefreshSetCookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith($"{AuthSessionCookies.RefreshCookieName}=", StringComparison.Ordinal));

    private static string CookieValue(string setCookie) =>
        setCookie.Split(';')[0][(AuthSessionCookies.RefreshCookieName.Length + 1)..];

    private static async Task<HttpResponseMessage> RefreshAsync(HttpClient client)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add(RequireSessionHeaderAttribute.HeaderName, RequireSessionHeaderAttribute.HeaderValue);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> RefreshWithAsync(string refreshToken)
    {
        using var client = _fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add(RequireSessionHeaderAttribute.HeaderName, RequireSessionHeaderAttribute.HeaderValue);
        request.Headers.Add("Cookie", $"{AuthSessionCookies.RefreshCookieName}={refreshToken}");
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
