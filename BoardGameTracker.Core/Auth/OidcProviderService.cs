using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public class OidcProviderService : IOidcProviderService
{
    private readonly MainDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ISecretProtector _secretProtector;
    private readonly ILogger<OidcProviderService> _logger;

    public OidcProviderService(MainDbContext context, IMemoryCache cache, ISecretProtector secretProtector, ILogger<OidcProviderService> logger)
    {
        _context = context;
        _cache = cache;
        _secretProtector = secretProtector;
        _logger = logger;
    }

    public async Task<List<OidcProvider>> GetAllAsync()
    {
        return await _context.OidcProviders.ToListAsync();
    }

    public async Task<OidcProvider> GetByIdAsync(int id)
    {
        return await _context.OidcProviders.FindAsync(id)
            ?? throw new EntityNotFoundException(nameof(OidcProvider), id);
    }

    public async Task<OidcProvider> CreateAsync(string name, string displayName, string authority, string clientId,
        string? clientSecret, string scopes, bool autoProvisionUsers,
        string? authorizationEndpoint, string? tokenEndpoint, string? userInfoEndpoint,
        string? usernameClaimType, string? emailClaimType, string? displayNameClaimType,
        string? rolesClaimType, string? adminGroupValue,
        string? iconUrl, string? buttonColor)
    {
        var existingCount = await _context.OidcProviders.CountAsync();
        if (existingCount > 0)
        {
            throw new DomainException("Only one OIDC provider is supported. Delete the existing provider first.");
        }

        EnsureSecureAuthority(authority);
        var provider = new OidcProvider(name, displayName, authority, clientId);
        provider.Update(displayName, authority, clientId, ProtectSecret(clientSecret), true, scopes, autoProvisionUsers,
            authorizationEndpoint, tokenEndpoint, userInfoEndpoint,
            usernameClaimType, emailClaimType, displayNameClaimType,
            rolesClaimType, adminGroupValue,
            iconUrl, buttonColor);

        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync();

        _logger.LogInformation("OIDC provider {ProviderName} created", provider.Name);

        return provider;
    }

    public async Task<OidcProvider> UpdateAsync(int id, string displayName, string authority, string clientId,
        string? clientSecret, bool enabled, string scopes, bool autoProvisionUsers,
        string? authorizationEndpoint, string? tokenEndpoint, string? userInfoEndpoint,
        string? usernameClaimType, string? emailClaimType, string? displayNameClaimType,
        string? rolesClaimType, string? adminGroupValue,
        string? iconUrl, string? buttonColor)
    {
        var provider = await _context.OidcProviders.FindAsync(id)
            ?? throw new EntityNotFoundException(nameof(OidcProvider), id);

        EnsureSecureAuthority(authority);
        provider.Update(displayName, authority, clientId, ProtectSecret(clientSecret), enabled, scopes, autoProvisionUsers,
            authorizationEndpoint, tokenEndpoint, userInfoEndpoint,
            usernameClaimType, emailClaimType, displayNameClaimType,
            rolesClaimType, adminGroupValue,
            iconUrl, buttonColor);

        await _context.SaveChangesAsync();

        // Invalidate cached discovery document since authority may have changed
        _cache.Remove($"oidc_discovery_{provider.Name}");

        _logger.LogInformation("OIDC provider {ProviderName} updated", provider.Name);

        return provider;
    }

    public async Task DeleteAsync(int id)
    {
        var provider = await _context.OidcProviders.FindAsync(id)
            ?? throw new EntityNotFoundException(nameof(OidcProvider), id);

        var externalLogins = await _context.ExternalLogins
            .Where(x => x.Provider == provider.Name)
            .ToListAsync();
        _context.ExternalLogins.RemoveRange(externalLogins);

        _context.OidcProviders.Remove(provider);
        await _context.SaveChangesAsync();

        _cache.Remove($"oidc_discovery_{provider.Name}");

        _logger.LogInformation("OIDC provider {ProviderName} deleted with {Count} associated external logins removed",
            provider.Name, externalLogins.Count);
    }

    private string? ProtectSecret(string? clientSecret)
    {
        return string.IsNullOrEmpty(clientSecret) ? clientSecret : _secretProtector.Protect(clientSecret);
    }

    private static void EnsureSecureAuthority(string authority)
    {
        if (!SecureUrlPolicy.IsAcceptable(authority))
        {
            throw new ValidationException(Constants.Errors.InsecureAuthority);
        }
    }
}
