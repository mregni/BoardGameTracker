using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public class OidcService : IOidcService
{
    private readonly MainDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OidcService> _logger;

    public OidcService(
        MainDbContext context,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<OidcService> logger)
    {
        _context = context;
        _userManager = userManager;
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IList<OidcProviderInfo>> GetEnabledProvidersAsync()
    {
        var providers = await _context.OidcProviders
            .Where(p => p.Enabled)
            .Select(p => new OidcProviderInfo(p.Name, p.DisplayName, p.IconUrl, p.ButtonColor))
            .ToListAsync();

        return providers;
    }

    public async Task<string> GetAuthorizationUrlAsync(string providerName, string redirectUri, string? state = null)
    {
        var provider = await GetProviderOrThrow(providerName);
        var discovery = await GetDiscoveryDocumentAsync(provider);

        var authEndpoint = provider.AuthorizationEndpoint ?? discovery.AuthorizationEndpoint;

        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Store code verifier in cache for callback
        var cacheKey = $"oidc_pkce_{state ?? providerName}";
        _cache.Set(cacheKey, codeVerifier, TimeSpan.FromMinutes(10));

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = provider.ClientId,
            ["response_type"] = "code",
            ["scope"] = provider.Scopes,
            ["redirect_uri"] = redirectUri,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        if (state != null)
        {
            queryParams["state"] = state;
        }

        var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{authEndpoint}?{queryString}";
    }

    public async Task<LoginResponse> HandleCallbackAsync(string providerName, string code, string redirectUri)
    {
        var provider = await GetProviderOrThrow(providerName);
        var discovery = await GetDiscoveryDocumentAsync(provider);

        var tokenEndpoint = provider.TokenEndpoint ?? discovery.TokenEndpoint;

        // Exchange code for tokens
        var cacheKey = $"oidc_pkce_{providerName}";
        var codeVerifier = _cache.Get<string>(cacheKey);
        _cache.Remove(cacheKey);

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = provider.ClientId,
        };

        if (!string.IsNullOrEmpty(provider.ClientSecret))
        {
            tokenRequest["client_secret"] = provider.ClientSecret;
        }

        if (codeVerifier != null)
        {
            tokenRequest["code_verifier"] = codeVerifier;
        }

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("OIDC token exchange failed for {Provider}: {Error}", providerName, error);
            throw new InvalidOperationException($"Token exchange failed: {error}");
        }

        var tokenResponse = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        var accessToken = tokenResponse.GetProperty("access_token").GetString()!;

        // Get user info
        var userInfoEndpoint = provider.UserInfoEndpoint ?? discovery.UserInfoEndpoint;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userInfoResponse = await client.GetAsync(userInfoEndpoint);
        var userInfo = await JsonSerializer.DeserializeAsync<JsonElement>(await userInfoResponse.Content.ReadAsStreamAsync());

        var providerKey = userInfo.GetProperty("sub").GetString()!;
        var email = GetClaimValue(userInfo, provider.EmailClaimType ?? "email");
        var username = GetClaimValue(userInfo, provider.UsernameClaimType ?? "preferred_username") ?? email;
        var displayName = GetClaimValue(userInfo, provider.DisplayNameClaimType ?? "name");

        // Find or create user
        var externalLogin = await _context.ExternalLogins
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Provider == providerName && x.ProviderKey == providerKey);

        ApplicationUser user;

        if (externalLogin != null)
        {
            user = externalLogin.User!;
            externalLogin.UpdateLastUsed();
        }
        else if (provider.AutoProvisionUsers)
        {
            // Check if user exists by email
            user = email != null ? await _userManager.FindByEmailAsync(email) : null!;

            if (user == null)
            {
                user = new ApplicationUser(
                    username ?? $"{providerName}_{providerKey}",
                    email ?? $"{providerKey}@{providerName}.external",
                    displayName);

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                await _userManager.AddToRoleAsync(user, Constants.AuthRoles.Reader);
                _logger.LogInformation("Auto-provisioned user {Username} from {Provider}", user.UserName, providerName);
            }

            var newExternalLogin = new ExternalLogin(user.Id, providerName, providerKey, displayName);
            _context.ExternalLogins.Add(newExternalLogin);
        }
        else
        {
            throw new InvalidOperationException("User not found and auto-provisioning is disabled");
        }

        user.UpdateLastLogin();
        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(user);
        var jwt = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        return new LoginResponse(
            jwt,
            refreshToken.Token,
            _tokenService.GetAccessTokenExpiry(),
            new UserInfo(user.Id, user.UserName!, user.DisplayName, roles));
    }

    public async Task LinkExternalLoginAsync(string userId, string providerName, string providerKey, string? displayName = null)
    {
        var existing = await _context.ExternalLogins
            .FirstOrDefaultAsync(x => x.Provider == providerName && x.ProviderKey == providerKey);

        if (existing != null)
        {
            throw new InvalidOperationException("This external account is already linked to a user");
        }

        var externalLogin = new ExternalLogin(userId, providerName, providerKey, displayName);
        _context.ExternalLogins.Add(externalLogin);
        await _context.SaveChangesAsync();
    }

    public async Task UnlinkExternalLoginAsync(string userId, int externalLoginId)
    {
        var externalLogin = await _context.ExternalLogins
            .FirstOrDefaultAsync(x => x.Id == externalLoginId && x.UserId == userId);

        if (externalLogin == null)
        {
            throw new InvalidOperationException("External login not found");
        }

        _context.ExternalLogins.Remove(externalLogin);
        await _context.SaveChangesAsync();
    }

    private async Task<OidcProvider> GetProviderOrThrow(string providerName)
    {
        var provider = await _context.OidcProviders
            .FirstOrDefaultAsync(p => p.Name == providerName && p.Enabled);

        return provider ?? throw new InvalidOperationException($"OIDC provider '{providerName}' not found or disabled");
    }

    private async Task<DiscoveryDocument> GetDiscoveryDocumentAsync(OidcProvider provider)
    {
        var cacheKey = $"oidc_discovery_{provider.Name}";

        if (_cache.TryGetValue<DiscoveryDocument>(cacheKey, out var cached))
        {
            return cached!;
        }

        var client = _httpClientFactory.CreateClient();
        var discoveryUrl = $"{provider.Authority.TrimEnd('/')}/.well-known/openid-configuration";

        var response = await client.GetAsync(discoveryUrl);
        response.EnsureSuccessStatusCode();

        var doc = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());

        var discovery = new DiscoveryDocument
        {
            AuthorizationEndpoint = doc.GetProperty("authorization_endpoint").GetString()!,
            TokenEndpoint = doc.GetProperty("token_endpoint").GetString()!,
            UserInfoEndpoint = doc.GetProperty("userinfo_endpoint").GetString()!,
        };

        _cache.Set(cacheKey, discovery, TimeSpan.FromHours(1));
        return discovery;
    }

    private static string? GetClaimValue(JsonElement userInfo, string claimType)
    {
        return userInfo.TryGetProperty(claimType, out var value) ? value.GetString() : null;
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private class DiscoveryDocument
    {
        public string AuthorizationEndpoint { get; init; } = string.Empty;
        public string TokenEndpoint { get; init; } = string.Empty;
        public string UserInfoEndpoint { get; init; } = string.Empty;
    }
}
