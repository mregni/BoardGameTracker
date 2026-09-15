using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.IntegrationTests.Infrastructure;

public sealed class FakeIdentityProvider : IAsyncDisposable
{
    private const string AccessToken = "fake-idp-access-token";
    private readonly WebApplication _app;
    private readonly ConcurrentDictionary<string, (string Challenge, string RedirectUri)> _codes = new();

    private FakeIdentityProvider(WebApplication app)
    {
        _app = app;
    }

    public string Authority { get; private set; } = string.Empty;

    public Dictionary<string, object> User { get; } = new()
    {
        ["sub"] = "fake-sub-1",
        ["email"] = "jane@example.com",
        ["preferred_username"] = "jane",
        ["name"] = "Jane Doe",
    };

    public static async Task<FakeIdentityProvider> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        var provider = new FakeIdentityProvider(app);

        app.MapGet("/.well-known/openid-configuration", () => Results.Json(new
        {
            issuer = provider.Authority,
            authorization_endpoint = $"{provider.Authority}/authorize",
            token_endpoint = $"{provider.Authority}/token",
            userinfo_endpoint = $"{provider.Authority}/userinfo",
        }));

        app.MapGet("/authorize", (HttpRequest request) =>
        {
            var code = Guid.NewGuid().ToString("N");
            provider._codes[code] = (request.Query["code_challenge"].ToString(), request.Query["redirect_uri"].ToString());
            var separator = request.Query["redirect_uri"].ToString().Contains('?') ? "&" : "?";
            return Results.Redirect($"{request.Query["redirect_uri"]}{separator}code={code}&state={Uri.EscapeDataString(request.Query["state"].ToString())}");
        });

        app.MapPost("/token", async (HttpRequest request) =>
        {
            var form = await request.ReadFormAsync();
            if (form["grant_type"] != "authorization_code" || !provider._codes.TryRemove(form["code"].ToString(), out var issued))
            {
                return Results.BadRequest(new { error = "invalid_grant" });
            }

            var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(form["code_verifier"].ToString())))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            if (challenge != issued.Challenge || form["redirect_uri"] != issued.RedirectUri)
            {
                return Results.BadRequest(new { error = "invalid_grant" });
            }

            return Results.Json(new { access_token = AccessToken, token_type = "Bearer" });
        });

        app.MapGet("/userinfo", (HttpRequest request) =>
            request.Headers.Authorization.ToString() == $"Bearer {AccessToken}"
                ? Results.Json(provider.User)
                : Results.Unauthorized());

        await app.StartAsync();
        provider.Authority = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return provider;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
