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
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

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
    private readonly IOidcConfigurationProvider _configurations;
    private readonly IOidcIdTokenValidator _idTokenValidator;
    private readonly IOidcRoleReconciler _roleReconciler;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<OidcService> _logger;

    public OidcService(
        MainDbContext context,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ISecretProtector secretProtector,
        IOidcConfigurationProvider configurations,
        IOidcIdTokenValidator idTokenValidator,
        IOidcRoleReconciler roleReconciler,
        IOptions<JwtOptions> jwtOptions,
        ILogger<OidcService> logger)
    {
        _context = context;
        _userManager = userManager;
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _secretProtector = secretProtector;
        _configurations = configurations;
        _idTokenValidator = idTokenValidator;
        _roleReconciler = roleReconciler;
        _jwtOptions = jwtOptions.Value;
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
        var metadata = await GetMetadataAsync(provider);
        var authorizationEndpoint = SecureEndpoint(provider.AuthorizationEndpoint ?? RequiredEndpoint(metadata.AuthorizationEndpoint));
        var redirectUri = CallbackUri(publicBaseUrl, providerName, flow);

        var codeVerifier = GenerateToken();
        var state = GenerateToken();
        var nonce = GenerateToken();
        SetSingleUse(
            PendingKey(state),
            new PendingAuthorization(codeVerifier, nonce, flow, userId, providerName, redirectUri, redirectPath),
            PendingAuthorizationLifetime);

        var query = new Dictionary<string, string>
        {
            ["client_id"] = provider.ClientId,
            ["response_type"] = "code",
            ["scope"] = WithOpenIdScope(provider.Scopes),
            ["redirect_uri"] = redirectUri,
            ["code_challenge"] = GenerateCodeChallenge(codeVerifier),
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["nonce"] = nonce,
        };

        var separator = authorizationEndpoint.Contains('?') ? "&" : "?";
        var queryString = separator + string.Join("&", query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        _logger.LogInformation("Started OIDC {Flow} flow for provider {Provider}", flow, providerName);

        return new OidcAuthorizationRequest(authorizationEndpoint + queryString, state);
    }

    public async Task<OidcLoginResult> CompleteLoginAsync(string providerName, string code, string? state, string? browserState, string? issuer)
    {
        var provider = await GetProviderOrThrow(providerName);
        var pending = TakePendingAuthorization(state, browserState, OidcFlow.Login, providerName);
        var identity = await AuthenticateAsync(provider, code, issuer, pending);

        var email = identity.Claims.GetValue(provider.EmailClaimType ?? "email");
        var username = identity.Claims.GetValue(provider.UsernameClaimType ?? "preferred_username") ?? email;
        var displayName = identity.Claims.GetValue(provider.DisplayNameClaimType ?? "name");

        var externalLogin = await FindExternalLoginAsync(provider, identity);

        ApplicationUser user;
        if (externalLogin != null)
        {
            user = externalLogin.User!;
            if (user.IsDisabled)
            {
                _logger.LogWarning("Disabled user {UserId} tried to sign in via {Provider}", user.Id, providerName);
                throw new DomainException(Constants.Errors.AccountDisabled);
            }

            externalLogin.UpdateLastUsed();
            if (externalLogin.Provider != provider.Name)
            {
                externalLogin.UpdateProvider(provider.Name);
            }

            await _roleReconciler.ReconcileAsync(user, provider, identity.Claims);
            _logger.LogInformation("Existing OIDC user {UserId} logged in via {Provider}", user.Id, providerName);
        }
        else if (provider.AutoProvisionUsers)
        {
            user = await ProvisionAsync(provider, identity, username, email, displayName);
        }
        else
        {
            _logger.LogWarning("OIDC login failed for {Provider}: user not found and auto-provisioning is disabled", providerName);
            throw new DomainException(Constants.Errors.OidcProvisioningDisabled);
        }

        user.UpdateLastLogin();
        await _userManager.UpdateAsync(user);
        var sessionKey = StartSession(user, provider, identity);
        await _context.SaveChangesAsync();

        return new OidcLoginResult(await IssueTokensAsync(user), pending.RedirectPath, sessionKey);
    }

    public async Task<OidcLinkResult> CompleteLinkAsync(string providerName, string code, string? state, string? browserState, string? issuer)
    {
        var provider = await GetProviderOrThrow(providerName);
        var pending = TakePendingAuthorization(state, browserState, OidcFlow.Link, providerName);
        var user = await _userManager.FindByIdAsync(pending.UserId!)
            ?? throw new EntityNotFoundException(nameof(ApplicationUser), pending.UserId!);
        var identity = await AuthenticateAsync(provider, code, issuer, pending);
        var displayName = identity.Claims.GetValue(provider.DisplayNameClaimType ?? "name");

        if (await FindExternalLoginAsync(provider, identity) != null)
        {
            throw new DomainException(Constants.Errors.OidcAlreadyLinked);
        }

        _context.ExternalLogins.Add(new ExternalLogin(user.Id, providerName, identity.Issuer, identity.Subject, displayName));
        await _context.SaveChangesAsync();

        _logger.LogInformation("Linked OIDC provider {Provider} to user {UserId}", providerName, user.Id);
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
            document.EndSessionEndpoint,
            OidcIssuer.Matches(issuer, authority));
    }

    public string CreateHandoff(LoginResponse login)
    {
        var key = GenerateToken();
        SetSingleUse(HandoffKey(key), login, HandoffLifetime);
        return key;
    }

    public LoginResponse? TakeHandoff(string? handoffKey)
    {
        if (string.IsNullOrWhiteSpace(handoffKey))
        {
            return null;
        }

        return TakeSingleUse<LoginResponse>(HandoffKey(handoffKey));
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

        var hasPassword = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.PasswordHash != null)
            .FirstOrDefaultAsync();
        var hasOtherLogin = await _context.ExternalLogins.AnyAsync(x => x.UserId == userId && x.Id != externalLoginId);
        if (!hasPassword && !hasOtherLogin)
        {
            throw new ValidationException(Constants.Errors.LastSignInMethod);
        }

        _context.ExternalLogins.Remove(externalLogin);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Unlinked external login {LoginId} (provider: {Provider}) from user {UserId}",
            externalLoginId, externalLogin.Provider, userId);
    }

    public async Task<string?> EndSessionAsync(string? sessionKey, string publicBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(sessionKey))
        {
            return null;
        }

        var keyHash = HashKey(sessionKey);
        var session = await _context.OidcSessions.FirstOrDefaultAsync(x => x.KeyHash == keyHash);
        if (session == null)
        {
            return null;
        }

        _context.OidcSessions.Remove(session);
        await _context.SaveChangesAsync();

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        var provider = await _context.OidcProviders.FirstOrDefaultAsync(p => p.Id == session.ProviderId && p.Enabled);
        if (provider == null)
        {
            return null;
        }

        try
        {
            var metadata = await GetMetadataAsync(provider);
            if (string.IsNullOrEmpty(metadata.EndSessionEndpoint) || !SecureUrlPolicy.IsAcceptable(metadata.EndSessionEndpoint))
            {
                return null;
            }

            var idToken = _secretProtector.Unprotect(session.IdToken);
            _logger.LogInformation("Ending the identity provider session of user {UserId} at {Provider}", session.UserId, provider.Name);
            return OidcLogoutUrl.Build(metadata.EndSessionEndpoint, idToken, provider.ClientId, publicBaseUrl);
        }
        catch (Exception ex) when (ex is DomainException or ValidationException or CryptographicException)
        {
            _logger.LogWarning(ex, "Could not build the identity provider sign-out address for {Provider}", provider.Name);
            return null;
        }
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = await _context.OidcSessions.Where(x => x.ExpiresAt < now).ToListAsync();
        if (expired.Count > 0)
        {
            _context.OidcSessions.RemoveRange(expired);
            await _context.SaveChangesAsync();
        }
    }

    private async Task<ApplicationUser> ProvisionAsync(OidcProvider provider, OidcIdentity identity, string? username, string? email, string? displayName)
    {
        var userName = username ?? $"{provider.Name}_{identity.Subject}";
        if (await _userManager.FindByNameAsync(userName) != null)
        {
            _logger.LogWarning("A new user from {Provider} has the username of an existing account that is not linked; manual linking required", provider.Name);
            throw new DomainException(Constants.Errors.OidcUsernameTaken);
        }

        var existingUser = email != null ? await FindLocalUserByEmailAsync(email) : null;
        if (existingUser != null)
        {
            _logger.LogWarning("A new user from {Provider} shares the email of existing user {UserId}; manual linking required", provider.Name, existingUser.Id);
            throw new DomainException(Constants.Errors.OidcEmailAlreadyRegistered);
        }

        var user = new ApplicationUser(
            userName,
            email ?? $"{identity.Subject}@{provider.Name}.external",
            displayName);

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to auto-provision a user from {Provider}: {Errors}", provider.Name, string.Join(", ", result.Errors.Select(e => e.Code)));
            throw new DomainException(Constants.Errors.OidcProvisioningFailed);
        }

        await _roleReconciler.AssignInitialRoleAsync(user, provider, identity.Claims);
        _context.ExternalLogins.Add(new ExternalLogin(user.Id, provider.Name, identity.Issuer, identity.Subject, displayName));
        _logger.LogInformation("Auto-provisioned user {UserId} from {Provider}", user.Id, provider.Name);
        return user;
    }

    private async Task<ExternalLogin?> FindExternalLoginAsync(OidcProvider provider, OidcIdentity identity)
    {
        var login = await _context.ExternalLogins
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Issuer == identity.Issuer && x.ProviderKey == identity.Subject);
        if (login != null)
        {
            return login;
        }

        var legacyIssuer = OidcIssuer.Normalize(provider.Authority);
        if (string.Equals(legacyIssuer, identity.Issuer, StringComparison.Ordinal))
        {
            return null;
        }

        login = await _context.ExternalLogins
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Issuer == legacyIssuer && x.ProviderKey == identity.Subject);
        if (login != null)
        {
            login.UpdateIssuer(identity.Issuer);
            _logger.LogInformation("Re-keyed external login {LoginId} on the issuer reported by {Provider}", login.Id, provider.Name);
        }

        return login;
    }

    private string? StartSession(ApplicationUser user, OidcProvider provider, OidcIdentity identity)
    {
        if (string.IsNullOrEmpty(identity.EndSessionEndpoint))
        {
            return null;
        }

        var key = GenerateToken();
        _context.OidcSessions.Add(new OidcSession(
            HashKey(key),
            user.Id,
            provider.Id,
            _secretProtector.Protect(identity.IdToken),
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays)));
        return key;
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

        var pending = TakeSingleUse<PendingAuthorization>(PendingKey(state));

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

    private async Task<OidcIdentity> AuthenticateAsync(OidcProvider provider, string code, string? issuer, PendingAuthorization pending)
    {
        var metadata = await GetMetadataAsync(provider);
        EnsureIssuerParameter(metadata, issuer, provider.Name);
        var tokenEndpoint = SecureEndpoint(provider.TokenEndpoint ?? RequiredEndpoint(metadata.TokenEndpoint));

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
        if (TokenValue(tokenResponse, "id_token") is not { } idToken)
        {
            _logger.LogError("OIDC token response from {Provider} carried no id_token", provider.Name);
            throw new DomainException(Constants.Errors.OidcIdTokenInvalid);
        }

        var validated = await _idTokenValidator.ValidateAsync(provider.Authority, metadata, provider.ClientId, idToken, pending.Nonce);
        var claims = validated.Claims;

        var userInfoEndpoint = provider.UserInfoEndpoint ?? metadata.UserInfoEndpoint;
        if (!string.IsNullOrEmpty(userInfoEndpoint) && TokenValue(tokenResponse, "access_token") is { } accessToken)
        {
            var userInfo = await FetchUserInfoAsync(client, provider, SecureEndpoint(userInfoEndpoint), accessToken);
            if (!string.Equals(userInfo.Subject, validated.Subject, StringComparison.Ordinal))
            {
                _logger.LogError("User info from {Provider} describes a different subject than the id_token", provider.Name);
                throw new DomainException(Constants.Errors.OidcIdTokenInvalid);
            }

            claims = claims.MergeWith(userInfo);
        }

        return new OidcIdentity(OidcIssuer.Normalize(metadata.Issuer), validated.Subject, claims, idToken, metadata.EndSessionEndpoint);
    }

    private async Task<OidcClaims> FetchUserInfoAsync(HttpClient client, OidcProvider provider, string userInfoEndpoint, string accessToken)
    {
        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, userInfoEndpoint);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var userInfoResponse = await client.SendAsync(userInfoRequest);
        if (!userInfoResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch user info from {Provider}: {StatusCode} {Error}", provider.Name, userInfoResponse.StatusCode, await userInfoResponse.Content.ReadAsStringAsync());
            throw new DomainException(Constants.Errors.OidcUserInfoFailed);
        }

        var userInfo = OidcClaims.From(await JsonSerializer.DeserializeAsync<JsonElement>(await userInfoResponse.Content.ReadAsStreamAsync()));
        if (userInfo.Subject is not { Length: > 0 })
        {
            _logger.LogError("User info from {Provider} carried no sub claim", provider.Name);
            throw new DomainException(Constants.Errors.OidcUserInfoFailed);
        }

        return userInfo;
    }

    private void EnsureIssuerParameter(OpenIdConnectConfiguration metadata, string? issuer, string providerName)
    {
        if (issuer == null)
        {
            if (metadata.AuthorizationResponseIssParameterSupported)
            {
                _logger.LogWarning("The callback from {Provider} carried no iss parameter although the provider advertises it", providerName);
                throw new DomainException(Constants.Errors.OidcIssuerMismatch);
            }

            return;
        }

        if (!string.Equals(issuer, metadata.Issuer, StringComparison.Ordinal))
        {
            _logger.LogWarning("The callback for {Provider} carried the iss parameter {Issuer}, which is not the provider's issuer", providerName, issuer);
            throw new DomainException(Constants.Errors.OidcIssuerMismatch);
        }
    }

    private static string? TokenValue(JsonElement tokenResponse, string name)
    {
        return tokenResponse.ValueKind == JsonValueKind.Object
               && tokenResponse.TryGetProperty(name, out var element)
               && element.ValueKind == JsonValueKind.String
               && element.GetString() is { Length: > 0 } value
            ? value
            : null;
    }

    private async Task<OidcProvider> GetProviderOrThrow(string providerName)
    {
        var provider = await _context.OidcProviders
            .FirstOrDefaultAsync(p => p.Name == providerName && p.Enabled);

        return provider ?? throw new EntityNotFoundException(nameof(OidcProvider), providerName);
    }

    private async Task<OpenIdConnectConfiguration> GetMetadataAsync(OidcProvider provider)
    {
        if (!SecureUrlPolicy.IsAcceptable(provider.Authority))
        {
            throw new ValidationException(Constants.Errors.InsecureAuthority);
        }

        var metadata = await _configurations.GetAsync(provider.Authority);
        if (string.IsNullOrWhiteSpace(metadata.Issuer))
        {
            throw new DomainException(Constants.Errors.OidcDiscoveryFailed);
        }

        if (!OidcIssuer.Matches(metadata.Issuer, provider.Authority))
        {
            _logger.LogError("OIDC discovery for {Provider} reports issuer {Issuer}, which does not match the configured authority", provider.Name, metadata.Issuer);
            throw new DomainException(Constants.Errors.OidcIssuerMismatch);
        }

        return metadata;
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

            return (issuer, new DiscoveryDocument(
                RequiredEndpoint(doc, "authorization_endpoint"),
                RequiredEndpoint(doc, "token_endpoint"),
                OptionalEndpoint(doc, "userinfo_endpoint"),
                OptionalEndpoint(doc, "end_session_endpoint")));
        }
    }

    private static string RequiredEndpoint(JsonElement doc, string name)
    {
        return OptionalEndpoint(doc, name) ?? throw new DomainException(Constants.Errors.OidcDiscoveryFailed);
    }

    private static string? OptionalEndpoint(JsonElement doc, string name)
    {
        return doc.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } endpoint
            ? endpoint
            : null;
    }

    private static string RequiredEndpoint(string? endpoint)
    {
        return string.IsNullOrWhiteSpace(endpoint) ? throw new DomainException(Constants.Errors.OidcDiscoveryFailed) : endpoint;
    }

    private static string SecureEndpoint(string endpoint)
    {
        return SecureUrlPolicy.IsAcceptable(endpoint) ? endpoint : throw new ValidationException(Constants.Errors.InsecureAuthority);
    }

    private static string WithOpenIdScope(string scopes)
    {
        var parts = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (!parts.Contains("openid", StringComparer.Ordinal))
        {
            parts.Insert(0, "openid");
        }

        return string.Join(' ', parts);
    }

    private static string CallbackUri(string publicBaseUrl, string providerName, OidcFlow flow)
    {
        var action = flow == OidcFlow.Link ? "link-callback" : "callback";
        return $"{publicBaseUrl.TrimEnd('/')}/api/auth/oidc/{Uri.EscapeDataString(providerName)}/{action}";
    }

    private void SetSingleUse<T>(string key, T value, TimeSpan lifetime) where T : class
    {
        _cache.Set(key, new SingleUse<T>(value), lifetime);
    }

    private T? TakeSingleUse<T>(string key) where T : class
    {
        var entry = _cache.Get<SingleUse<T>>(key);
        _cache.Remove(key);
        return entry?.Take();
    }

    private static string PendingKey(string state) => $"oidc_pending_{state}";

    private static string HandoffKey(string key) => $"oidc_handoff_{key}";

    public static string HashKey(string key) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

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

    private sealed class SingleUse<T>(T value) where T : class
    {
        private int _taken;

        public T? Take() => Interlocked.Exchange(ref _taken, 1) == 0 ? value : null;
    }

    private sealed record PendingAuthorization(
        string CodeVerifier,
        string Nonce,
        OidcFlow Flow,
        string? UserId,
        string Provider,
        string RedirectUri,
        string RedirectPath);

    private sealed record OidcIdentity(string Issuer, string Subject, OidcClaims Claims, string IdToken, string? EndSessionEndpoint);

    private sealed record DiscoveryDocument(string AuthorizationEndpoint, string TokenEndpoint, string? UserInfoEndpoint, string? EndSessionEndpoint);
}
