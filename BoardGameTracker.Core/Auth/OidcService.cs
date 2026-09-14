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
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public class OidcService : IOidcService
{
    public const string HttpClientName = "oidc";
    public static readonly TimeSpan PendingAuthorizationLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan HandoffLifetime = TimeSpan.FromMinutes(1);

    private readonly MainDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ISecretProtector _secretProtector;
    private readonly ILogger<OidcService> _logger;

    public OidcService(
        MainDbContext context,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ISecretProtector secretProtector,
        ILogger<OidcService> logger)
    {
        _context = context;
        _userManager = userManager;
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _secretProtector = secretProtector;
        _logger = logger;
    }

    public async Task<OidcProviderInfo?> GetEnabledProviderAsync()
    {
        return await _context.OidcProviders
            .Where(p => p.Enabled)
            .OrderBy(p => p.Id)
            .Select(p => new OidcProviderInfo(p.Name, p.DisplayName, p.IconUrl, p.ButtonColor))
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasEnabledProviderAsync()
    {
        return await _context.OidcProviders.AnyAsync(p => p.Enabled);
    }

    public Task<OidcAuthorizationRequest> StartLoginAsync(string providerName, string publicBaseUrl, string? redirectPath)
    {
        return StartAsync(providerName, publicBaseUrl, OidcFlow.Login, null, LocalPath.Normalize(redirectPath));
    }

    public Task<OidcAuthorizationRequest> StartLinkAsync(string userId, string providerName, string publicBaseUrl)
    {
        return StartAsync(providerName, publicBaseUrl, OidcFlow.Link, userId, "/");
    }

    private async Task<OidcAuthorizationRequest> StartAsync(string providerName, string publicBaseUrl, OidcFlow flow, string? userId, string redirectPath)
    {
        var provider = await GetProviderOrThrow(providerName);
        var discovery = await GetDiscoveryDocumentAsync(provider);
        var authorizationEndpoint = SecureEndpoint(provider.AuthorizationEndpoint ?? discovery.AuthorizationEndpoint);
        var redirectUri = CallbackUri(publicBaseUrl, providerName, flow);

        var codeVerifier = GenerateToken();
        var state = GenerateToken();
        var nonce = GenerateToken();
        _cache.Set(
            PendingKey(state),
            new PendingAuthorization(codeVerifier, nonce, flow, userId, providerName, redirectUri, redirectPath),
            PendingAuthorizationLifetime);

        var query = new Dictionary<string, string>
        {
            ["client_id"] = provider.ClientId,
            ["response_type"] = "code",
            ["scope"] = provider.Scopes,
            ["redirect_uri"] = redirectUri,
            ["code_challenge"] = GenerateCodeChallenge(codeVerifier),
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["nonce"] = nonce,
        };

        var queryString = string.Join("&", query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        _logger.LogInformation("Started OIDC {Flow} flow for provider {Provider}", flow, providerName);

        return new OidcAuthorizationRequest($"{authorizationEndpoint}?{queryString}", state);
    }

    public async Task<OidcLoginResult> CompleteLoginAsync(string providerName, string code, string? state, string? browserState)
    {
        var provider = await GetProviderOrThrow(providerName);
        var pending = TakePendingAuthorization(state, browserState, OidcFlow.Login, providerName);
        var userInfo = await ExchangeCodeAndGetUserInfo(provider, code, pending);

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
            var existingUser = email != null ? await FindLocalUserByEmailAsync(email) : null;
            if (existingUser != null)
            {
                _logger.LogWarning("OIDC user with email {Email} matches existing local user {Username}; manual linking required", email, existingUser.UserName);
                throw new DomainException(Constants.Errors.OidcEmailAlreadyRegistered);
            }

            user = new ApplicationUser(
                username ?? $"{providerName}_{providerKey}",
                email ?? $"{providerKey}@{providerName}.external",
                displayName);

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to auto-provision OIDC user {Username}: {Errors}", username, string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new DomainException(Constants.Errors.OidcProvisioningFailed);
            }

            await AssignRolesFromClaims(user, provider, userInfo);
            _context.ExternalLogins.Add(new ExternalLogin(user.Id, providerName, providerKey, displayName));
            _logger.LogInformation("Auto-provisioned user {Username} from {Provider}", user.UserName, providerName);
        }
        else
        {
            _logger.LogWarning("OIDC login failed for {Provider}: user not found and auto-provisioning is disabled", providerName);
            throw new DomainException(Constants.Errors.OidcProvisioningDisabled);
        }

        user.UpdateLastLogin();
        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        return new OidcLoginResult(await IssueTokensAsync(user), pending.RedirectPath);
    }

    public async Task<OidcLinkResult> CompleteLinkAsync(string providerName, string code, string? state, string? browserState)
    {
        var provider = await GetProviderOrThrow(providerName);
        var pending = TakePendingAuthorization(state, browserState, OidcFlow.Link, providerName);
        var user = await _userManager.FindByIdAsync(pending.UserId!)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), pending.UserId!);
        var userInfo = await ExchangeCodeAndGetUserInfo(provider, code, pending);

        var providerKey = userInfo.GetProperty("sub").GetString()!;
        var displayName = GetClaimValue(userInfo, provider.DisplayNameClaimType ?? "name");

        var alreadyLinked = await _context.ExternalLogins
            .AnyAsync(x => x.Provider == providerName && x.ProviderKey == providerKey);
        if (alreadyLinked)
        {
            throw new DomainException(Constants.Errors.OidcAlreadyLinked);
        }

        _context.ExternalLogins.Add(new ExternalLogin(user.Id, providerName, providerKey, displayName));
        await _context.SaveChangesAsync();

        _logger.LogInformation("Linked OIDC provider {Provider} to user {Username}", providerName, user.UserName);
        return new OidcLinkResult(user.Id, providerName);
    }

    public async Task<OidcDiscoveryResultDto> TestDiscoveryAsync(string authority)
    {
        if (!SecureUrlPolicy.IsAcceptable(authority))
        {
            throw new ValidationException(Constants.Errors.InsecureAuthority);
        }

        var (issuer, document) = await FetchDiscoveryAsync(authority.TrimEnd('/'));
        return new OidcDiscoveryResultDto(
            issuer,
            document.AuthorizationEndpoint,
            document.TokenEndpoint,
            document.UserInfoEndpoint,
            IssuerMatches(issuer, authority));
    }

    public string CreateHandoff(LoginResponse login)
    {
        var key = GenerateToken();
        _cache.Set(HandoffKey(key), login, HandoffLifetime);
        return key;
    }

    public LoginResponse? TakeHandoff(string? handoffKey)
    {
        if (string.IsNullOrWhiteSpace(handoffKey))
        {
            return null;
        }

        var cacheKey = HandoffKey(handoffKey);
        var login = _cache.Get<LoginResponse>(cacheKey);
        _cache.Remove(cacheKey);
        return login;
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

    private async Task<LoginResponse> IssueTokensAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var jwt = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

        return new LoginResponse(
            jwt,
            refreshToken.PlainTextToken!,
            _tokenService.GetAccessTokenExpiry(),
            user.ToUserInfo(roles));
    }

    private PendingAuthorization TakePendingAuthorization(string? state, string? browserState, OidcFlow flow, string providerName)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ValidationException(Constants.Errors.InvalidAuthSession);
        }

        var cacheKey = PendingKey(state);
        var pending = _cache.Get<PendingAuthorization>(cacheKey);
        _cache.Remove(cacheKey);

        if (pending == null
            || pending.Flow != flow
            || pending.Provider != providerName
            || browserState == null
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(browserState), Encoding.UTF8.GetBytes(state)))
        {
            _logger.LogWarning("OIDC state for provider {Provider} did not match the pending {Flow} flow", providerName, flow);
            throw new ValidationException(Constants.Errors.InvalidAuthSession);
        }

        return pending;
    }

    private async Task<ApplicationUser?> FindLocalUserByEmailAsync(string email)
    {
        var normalizedEmail = _userManager.NormalizeEmail(email);
        return await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
    }

    private async Task<JsonElement> ExchangeCodeAndGetUserInfo(OidcProvider provider, string code, PendingAuthorization pending)
    {
        var discovery = await GetDiscoveryDocumentAsync(provider);
        var tokenEndpoint = SecureEndpoint(provider.TokenEndpoint ?? discovery.TokenEndpoint);
        var userInfoEndpoint = SecureEndpoint(provider.UserInfoEndpoint ?? discovery.UserInfoEndpoint);

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = pending.RedirectUri,
            ["client_id"] = provider.ClientId,
            ["code_verifier"] = pending.CodeVerifier,
        };

        if (!string.IsNullOrEmpty(provider.ClientSecret))
        {
            tokenRequest["client_secret"] = _secretProtector.Unprotect(provider.ClientSecret);
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OIDC token exchange failed for {Provider}: {StatusCode} {Error}", provider.Name, response.StatusCode, await response.Content.ReadAsStringAsync());
            throw new DomainException(Constants.Errors.OidcExchangeFailed);
        }

        var tokenResponse = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        if (!tokenResponse.TryGetProperty("access_token", out var accessTokenElement) || accessTokenElement.GetString() is not { Length: > 0 } accessToken)
        {
            _logger.LogError("OIDC token response from {Provider} carried no access token", provider.Name);
            throw new DomainException(Constants.Errors.OidcExchangeFailed);
        }

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var userInfoResponse = await client.SendAsync(userInfoRequest);
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch user info from {Provider}: {StatusCode} {Error}", provider.Name, userInfoResponse.StatusCode, await userInfoResponse.Content.ReadAsStringAsync());
            throw new DomainException(Constants.Errors.OidcUserInfoFailed);
        }

        var userInfo = await JsonSerializer.DeserializeAsync<JsonElement>(await userInfoResponse.Content.ReadAsStreamAsync());
        if (!userInfo.TryGetProperty("sub", out var sub) || sub.GetString() is not { Length: > 0 })
        {
            _logger.LogError("User info from {Provider} carried no sub claim", provider.Name);
            throw new DomainException(Constants.Errors.OidcUserInfoFailed);
        }

        return userInfo;
    }

    private async Task AssignRolesFromClaims(ApplicationUser user, OidcProvider provider, JsonElement userInfo)
    {
        var role = Constants.AuthRoles.User;

        if (!string.IsNullOrEmpty(provider.RolesClaimType) && !string.IsNullOrEmpty(provider.AdminGroupValue))
        {
            var groups = GetClaimValues(userInfo, provider.RolesClaimType);
            if (groups.Any(g => string.Equals(g, provider.AdminGroupValue, StringComparison.OrdinalIgnoreCase)))
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

        return provider ?? throw new EntityNotFoundException(nameof(OidcProvider), providerName);
    }

    private async Task<DiscoveryDocument> GetDiscoveryDocumentAsync(OidcProvider provider)
    {
        var cacheKey = $"oidc_discovery_{provider.Name}";
        if (_cache.TryGetValue<DiscoveryDocument>(cacheKey, out var cached))
        {
            return cached!;
        }

        if (!SecureUrlPolicy.IsAcceptable(provider.Authority))
        {
            throw new ValidationException(Constants.Errors.InsecureAuthority);
        }

        var authority = provider.Authority.TrimEnd('/');
        var (issuer, discovery) = await FetchDiscoveryAsync(authority);
        if (!IssuerMatches(issuer, authority))
        {
            _logger.LogError("OIDC discovery for {Provider} reports issuer {Issuer}, which does not match the configured authority", provider.Name, issuer);
            throw new DomainException(Constants.Errors.OidcIssuerMismatch);
        }

        _cache.Set(cacheKey, discovery, TimeSpan.FromHours(1));
        return discovery;
    }

    private async Task<(string Issuer, DiscoveryDocument Document)> FetchDiscoveryAsync(string authority)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"{authority}/.well-known/openid-configuration");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "OIDC discovery at {Authority} could not be reached", authority);
            throw new DomainException(Constants.Errors.OidcDiscoveryFailed);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("OIDC discovery at {Authority} failed with {StatusCode}", authority, response.StatusCode);
                throw new DomainException(Constants.Errors.OidcDiscoveryFailed);
            }

            JsonElement doc;
            try
            {
                doc = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "OIDC discovery at {Authority} returned something that is not a discovery document", authority);
                throw new DomainException(Constants.Errors.OidcDiscoveryFailed);
            }

            var issuer = doc.ValueKind == JsonValueKind.Object && doc.TryGetProperty("issuer", out var issuerElement) ? issuerElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new DomainException(Constants.Errors.OidcDiscoveryFailed);
            }

            return (issuer, new DiscoveryDocument
            {
                AuthorizationEndpoint = RequiredEndpoint(doc, "authorization_endpoint"),
                TokenEndpoint = RequiredEndpoint(doc, "token_endpoint"),
                UserInfoEndpoint = RequiredEndpoint(doc, "userinfo_endpoint"),
            });
        }
    }

    private static bool IssuerMatches(string issuer, string authority)
    {
        return string.Equals(issuer.TrimEnd('/'), authority.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }

    private static string RequiredEndpoint(JsonElement doc, string name)
    {
        return doc.TryGetProperty(name, out var value) && value.GetString() is { Length: > 0 } endpoint
            ? endpoint
            : throw new DomainException(Constants.Errors.OidcDiscoveryFailed);
    }

    private static string SecureEndpoint(string endpoint)
    {
        return SecureUrlPolicy.IsAcceptable(endpoint) ? endpoint : throw new ValidationException(Constants.Errors.InsecureAuthority);
    }

    private static string CallbackUri(string publicBaseUrl, string providerName, OidcFlow flow)
    {
        var action = flow == OidcFlow.Link ? "link-callback" : "callback";
        return $"{publicBaseUrl.TrimEnd('/')}/api/auth/oidc/{Uri.EscapeDataString(providerName)}/{action}";
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

    private static List<string> GetClaimValues(JsonElement userInfo, string claimType)
    {
        if (!userInfo.TryGetProperty(claimType, out var value))
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray()
                .Select(v => v.GetString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .ToList();
        }

        var scalar = value.GetString();
        if (string.IsNullOrWhiteSpace(scalar))
        {
            return [];
        }

        return scalar
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string PendingKey(string state) => $"oidc_pending_{state}";

    private static string HandoffKey(string key) => $"oidc_handoff_{key}";

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64Url(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        return Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private enum OidcFlow
    {
        Login,
        Link
    }

    private sealed record PendingAuthorization(
        string CodeVerifier,
        string Nonce,
        OidcFlow Flow,
        string? UserId,
        string Provider,
        string RedirectUri,
        string RedirectPath);

    private sealed class DiscoveryDocument
    {
        public string AuthorizationEndpoint { get; init; } = string.Empty;
        public string TokenEndpoint { get; init; } = string.Empty;
        public string UserInfoEndpoint { get; init; } = string.Empty;
    }
}
