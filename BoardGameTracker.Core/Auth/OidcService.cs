using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
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

    public async Task<OidcProviderInfo?> GetEnabledProviderAsync()
    {
        var provider = await _context.OidcProviders
            .Where(p => p.Enabled)
            .Select(p => new OidcProviderInfo(p.Name, p.DisplayName, p.IconUrl, p.ButtonColor))
            .FirstOrDefaultAsync();

        return provider;
    }

    public async Task<bool> HasEnabledProviderAsync()
    {
        return await _context.OidcProviders.AnyAsync(p => p.Enabled);
    }

    public async Task<string> GetAuthorizationUrlAsync(string providerName, string redirectUri, string? state = null)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new InvalidOperationException("Invalid redirect URI");
        }

        var provider = await GetProviderOrThrow(providerName);
        var discovery = await GetDiscoveryDocumentAsync(provider);

        var authEndpoint = provider.AuthorizationEndpoint ?? discovery.AuthorizationEndpoint;

        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

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

        _logger.LogInformation("Generated OIDC authorization URL for provider {Provider}", providerName);

        return $"{authEndpoint}?{queryString}";
    }

    public async Task<LoginResponse> HandleCallbackAsync(string providerName, string code, string redirectUri, string? state = null)
    {
        _logger.LogInformation("Processing OIDC callback for provider {Provider}", providerName);

        var provider = await GetProviderOrThrow(providerName);
        var userInfo = await ExchangeCodeAndGetUserInfo(provider, providerName, code, redirectUri, state);

        var providerKey = userInfo.GetProperty("sub").GetString()!;
        var email = GetClaimValue(userInfo, provider.EmailClaimType ?? "email");
        var username = GetClaimValue(userInfo, provider.UsernameClaimType ?? "preferred_username") ?? email;
        var displayName = GetClaimValue(userInfo, provider.DisplayNameClaimType ?? "name");

        var externalLogin = await _context.ExternalLogins
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Provider == providerName && x.ProviderKey == providerKey);

        ApplicationUser user;

        if (externalLogin != null)
        {
            user = externalLogin.User!;
            externalLogin.UpdateLastUsed();
            _logger.LogInformation("Existing OIDC user {Username} logged in via {Provider}", user.UserName, providerName);
        }
        else if (provider.AutoProvisionUsers)
        {
            // Check if user exists by email - do NOT silently link (issue #8)
            var existingUser = email != null ? await _userManager.FindByEmailAsync(email) : null;

            if (existingUser != null)
            {
                _logger.LogWarning("OIDC user with email {Email} matches existing local user {Username}. Requires manual linking", email, existingUser.UserName);
                throw new DomainException("An account with this email already exists. Please log in with your existing credentials and link your OIDC account from your profile.");
            }

            user = new ApplicationUser(
                username ?? $"{providerName}_{providerKey}",
                email ?? $"{providerKey}@{providerName}.external",
                displayName);

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to auto-provision OIDC user {Username}: {Errors}", username, string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new InvalidOperationException(
                    $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await AssignRolesFromClaims(user, provider, userInfo);

            var newExternalLogin = new ExternalLogin(user.Id, providerName, providerKey, displayName);
            _context.ExternalLogins.Add(newExternalLogin);
            _logger.LogInformation("Auto-provisioned user {Username} from {Provider}", user.UserName, providerName);
        }
        else
        {
            _logger.LogWarning("OIDC login failed for {Provider}: user not found and auto-provisioning is disabled", providerName);
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
            user.ToUserInfo(roles));
    }

    public async Task<LoginResponse> HandleLinkCallbackAsync(string userId, string providerName, string code, string redirectUri, string? state = null)
    {
        _logger.LogInformation("Processing OIDC link callback for user {UserId} with provider {Provider}", userId, providerName);

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        var provider = await GetProviderOrThrow(providerName);
        var userInfo = await ExchangeCodeAndGetUserInfo(provider, providerName, code, redirectUri, state);

        var providerKey = userInfo.GetProperty("sub").GetString()!;
        var displayName = GetClaimValue(userInfo, provider.DisplayNameClaimType ?? "name");

        var existing = await _context.ExternalLogins
            .FirstOrDefaultAsync(x => x.Provider == providerName && x.ProviderKey == providerKey);

        if (existing != null)
        {
            throw new DomainException("This external account is already linked to a user");
        }

        var externalLogin = new ExternalLogin(userId, providerName, providerKey, displayName);
        _context.ExternalLogins.Add(externalLogin);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Linked OIDC provider {Provider} to user {Username}", providerName, user.UserName);

        var roles = await _userManager.GetRolesAsync(user);
        var jwt = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        return new LoginResponse(
            jwt,
            refreshToken.Token,
            _tokenService.GetAccessTokenExpiry(),
            user.ToUserInfo(roles));
    }

    public async Task<List<ExternalLoginDto>> GetExternalLoginsAsync(string userId)
    {
        return await _context.ExternalLogins
            .Where(x => x.UserId == userId)
            .Select(x => x.ToDto())
            .ToListAsync();
    }

    public async Task UnlinkExternalLoginAsync(string userId, int externalLoginId)
    {
        var externalLogin = await _context.ExternalLogins
            .FirstOrDefaultAsync(x => x.Id == externalLoginId && x.UserId == userId)
            ?? throw new EntityNotFoundException(nameof(ExternalLogin), externalLoginId);

        _context.ExternalLogins.Remove(externalLogin);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Unlinked external login {LoginId} (provider: {Provider}) from user {UserId}",
            externalLoginId, externalLogin.Provider, userId);
    }

    private async Task<JsonElement> ExchangeCodeAndGetUserInfo(OidcProvider provider, string providerName, string code, string redirectUri, string? state)
    {
        var discovery = await GetDiscoveryDocumentAsync(provider);
        var tokenEndpoint = provider.TokenEndpoint ?? discovery.TokenEndpoint;

        var cacheKey = $"oidc_pkce_{state ?? providerName}";
        var codeVerifier = _cache.Get<string>(cacheKey)
            ?? throw new InvalidOperationException("Invalid or expired authentication session");
        _cache.Remove(cacheKey);

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = provider.ClientId,
            ["code_verifier"] = codeVerifier,
        };

        if (!string.IsNullOrEmpty(provider.ClientSecret))
        {
            tokenRequest["client_secret"] = provider.ClientSecret;
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

        var userInfoEndpoint = provider.UserInfoEndpoint ?? discovery.UserInfoEndpoint;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userInfoResponse = await client.GetAsync(userInfoEndpoint);

        if (!userInfoResponse.IsSuccessStatusCode)
        {
            var error = await userInfoResponse.Content.ReadAsStringAsync();
            _logger.LogError("Failed to fetch user info from {Provider}: {StatusCode} {Error}", providerName, userInfoResponse.StatusCode, error);
            throw new InvalidOperationException("Failed to retrieve user information from OIDC provider");
        }

        return await JsonSerializer.DeserializeAsync<JsonElement>(await userInfoResponse.Content.ReadAsStreamAsync());
    }

    private async Task AssignRolesFromClaims(ApplicationUser user, OidcProvider provider, JsonElement userInfo)
    {
        var role = Constants.AuthRoles.User;

        if (!string.IsNullOrEmpty(provider.RolesClaimType) && !string.IsNullOrEmpty(provider.AdminGroupValue))
        {
            var groupsClaim = GetClaimValue(userInfo, provider.RolesClaimType);
            if (groupsClaim != null && groupsClaim.Contains(provider.AdminGroupValue, StringComparison.OrdinalIgnoreCase))
            {
                role = Constants.AuthRoles.Admin;
                _logger.LogInformation("Assigning Admin role to OIDC user {Username} based on group claim", user.UserName);
            }
        }

        await _userManager.AddToRoleAsync(user, role);
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
        if (!userInfo.TryGetProperty(claimType, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.Array
            ? string.Join(",", value.EnumerateArray().Select(v => v.GetString()))
            : value.GetString();
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
