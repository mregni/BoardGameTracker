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
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BoardGameTracker.IntegrationTests.Infrastructure;

public sealed class FakeIdentityProvider : IAsyncDisposable
{
    private const string AccessToken = "fake-idp-access-token";
    private const string KeyId = "fake-key-1";
    private static readonly JsonWebTokenHandler TokenHandler = new() { SetDefaultTimesOnTokenCreation = false };

    private readonly WebApplication _app;
    private readonly RSA _signingKey = RSA.Create(2048);
    private readonly RSA _attackerKey = RSA.Create(2048);
    private readonly ConcurrentDictionary<string, (string Challenge, string RedirectUri, string Nonce)> _codes = new();

    private FakeIdentityProvider(WebApplication app)
    {
        _app = app;
    }

    public string Authority { get; private set; } = string.Empty;

    public string ClientId { get; set; } = "bgt-client";

    public string? ClientSecret { get; set; }

    public bool TamperIdToken { get; set; }

    public string? IssuerParameterOverride { get; set; }

    public string? LastIdToken { get; private set; }

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

        app.MapGet("/.well-known/openid-configuration", () => Results.Json(new Dictionary<string, object>
        {
            ["issuer"] = provider.Authority,
            ["authorization_endpoint"] = $"{provider.Authority}/authorize",
            ["token_endpoint"] = $"{provider.Authority}/token",
            ["userinfo_endpoint"] = $"{provider.Authority}/userinfo",
            ["end_session_endpoint"] = $"{provider.Authority}/logout",
            ["jwks_uri"] = $"{provider.Authority}/certs",
            ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
            ["authorization_response_iss_parameter_supported"] = true,
        }));

        app.MapGet("/certs", () =>
        {
            var parameters = provider._signingKey.ExportParameters(false);
            return Results.Json(new
            {
                keys = new[]
                {
                    new { kty = "RSA", use = "sig", alg = "RS256", kid = KeyId, n = Base64UrlEncoder.Encode(parameters.Modulus), e = Base64UrlEncoder.Encode(parameters.Exponent) },
                },
            });
        });

        app.MapGet("/authorize", (HttpRequest request) =>
        {
            var code = Guid.NewGuid().ToString("N");
            provider._codes[code] = (request.Query["code_challenge"].ToString(), request.Query["redirect_uri"].ToString(), request.Query["nonce"].ToString());
            var separator = request.Query["redirect_uri"].ToString().Contains('?') ? "&" : "?";
            var issuer = Uri.EscapeDataString(provider.IssuerParameterOverride ?? provider.Authority);
            return Results.Redirect($"{request.Query["redirect_uri"]}{separator}code={code}&state={Uri.EscapeDataString(request.Query["state"].ToString())}&iss={issuer}");
        });

        app.MapPost("/token", async (HttpRequest request) =>
        {
            var form = await request.ReadFormAsync();
            if (form["client_id"] != provider.ClientId || (provider.ClientSecret != null && form["client_secret"] != provider.ClientSecret))
            {
                return Results.Json(new { error = "invalid_client" }, statusCode: StatusCodes.Status401Unauthorized);
            }

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

            provider.LastIdToken = provider.IssueIdToken(issued.Nonce);
            return Results.Json(new { access_token = AccessToken, token_type = "Bearer", id_token = provider.LastIdToken });
        });

        app.MapGet("/userinfo", (HttpRequest request) =>
            request.Headers.Authorization.ToString() == $"Bearer {AccessToken}"
                ? Results.Json(provider.User)
                : Results.Unauthorized());

        await app.StartAsync();
        provider.Authority = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return provider;
    }

    private string IssueIdToken(string nonce)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var claims = new Dictionary<string, object>(User)
        {
            ["iss"] = Authority,
            ["aud"] = ClientId,
            ["nonce"] = nonce,
            ["iat"] = now,
            ["nbf"] = now,
            ["exp"] = now + 300,
        };

        var key = new RsaSecurityKey(TamperIdToken ? _attackerKey : _signingKey) { KeyId = KeyId };
        return TokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Claims = claims,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
        });
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        _signingKey.Dispose();
        _attackerKey.Dispose();
    }
}
