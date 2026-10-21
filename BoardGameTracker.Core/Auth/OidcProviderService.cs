using System.Text.RegularExpressions;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public partial class OidcProviderService : IOidcProviderService
{
    private readonly MainDbContext _context;
    private readonly IOidcConfigurationProvider _configurations;
    private readonly ISecretProtector _secretProtector;
    private readonly ILogger<OidcProviderService> _logger;

    public OidcProviderService(MainDbContext context, IOidcConfigurationProvider configurations, ISecretProtector secretProtector, ILogger<OidcProviderService> logger)
    {
        _context = context;
        _configurations = configurations;
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
        string? iconUrl, string? buttonColor, string? defaultRole = null, bool enabled = true)
    {
        var existingCount = await _context.OidcProviders.CountAsync();
        if (existingCount > 0)
        {
            throw new DomainException("Only one OIDC provider is supported. Delete the existing provider first.");
        }

        EnsureSecureAuthority(authority);
        var provider = new OidcProvider(name, displayName, authority, clientId);
        provider.Update(displayName, authority, clientId, ProtectSecret(clientSecret), enabled, scopes, autoProvisionUsers,
            authorizationEndpoint, tokenEndpoint, userInfoEndpoint,
            usernameClaimType, emailClaimType, displayNameClaimType,
            rolesClaimType, adminGroupValue,
            iconUrl, buttonColor);
        provider.SetDefaultRole(ValidDefaultRole(defaultRole) ?? Constants.AuthRoles.User);

        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync();

        _configurations.Invalidate(authority);

        _logger.LogInformation("OIDC provider {ProviderName} created", provider.Name);

        return provider;
    }

    public async Task<OidcProvider> UpdateAsync(int id, string displayName, string authority, string clientId,
        string? clientSecret, bool enabled, string scopes, bool autoProvisionUsers,
        string? authorizationEndpoint, string? tokenEndpoint, string? userInfoEndpoint,
        string? usernameClaimType, string? emailClaimType, string? displayNameClaimType,
        string? rolesClaimType, string? adminGroupValue,
        string? iconUrl, string? buttonColor, string? defaultRole = null, string? name = null)
    {
        var provider = await _context.OidcProviders.FindAsync(id)
            ?? throw new EntityNotFoundException(nameof(OidcProvider), id);

        EnsureSecureAuthority(authority);
        var previousName = provider.Name;
        var previousAuthority = provider.Authority;
        var newName = string.IsNullOrWhiteSpace(name) ? previousName : name.Trim();
        if (newName != previousName)
        {
            await EnsureValidNewNameAsync(newName, provider.Id);
        }

        var authorityChanged = !string.Equals(NormalizeAuthority(previousAuthority), NormalizeAuthority(authority), StringComparison.OrdinalIgnoreCase);
        provider.Update(displayName, authority, clientId, ProtectSecret(clientSecret), enabled, scopes, autoProvisionUsers,
            authorizationEndpoint, tokenEndpoint, userInfoEndpoint,
            usernameClaimType, emailClaimType, displayNameClaimType,
            rolesClaimType, adminGroupValue,
            iconUrl, buttonColor);
        if (ValidDefaultRole(defaultRole) is { } role)
        {
            provider.SetDefaultRole(role);
        }

        var externalLogins = await LinksOfAsync(previousAuthority, previousName);
        if (authorityChanged)
        {
            _context.ExternalLogins.RemoveRange(externalLogins);
            _context.OidcSessions.RemoveRange(await _context.OidcSessions.Where(x => x.ProviderId == provider.Id).ToListAsync());
            _logger.LogWarning("OIDC provider {ProviderName} now points at a different authority; {Count} linked accounts must link it again",
                previousName, externalLogins.Count);
        }
        else if (newName != previousName)
        {
            foreach (var login in externalLogins)
            {
                login.UpdateProvider(newName);
            }
        }

        if (newName != previousName)
        {
            provider.Rename(newName);
            _logger.LogInformation("OIDC provider {PreviousName} renamed to {ProviderName}", previousName, newName);
        }

        await _context.SaveChangesAsync();

        _configurations.Invalidate(previousAuthority);
        _configurations.Invalidate(authority);

        _logger.LogInformation("OIDC provider {ProviderName} updated", provider.Name);

        return provider;
    }

    public async Task DeleteAsync(int id)
    {
        var provider = await _context.OidcProviders.FindAsync(id)
            ?? throw new EntityNotFoundException(nameof(OidcProvider), id);

        var externalLogins = await LinksOfAsync(provider.Authority, provider.Name);
        _context.ExternalLogins.RemoveRange(externalLogins);
        _context.OidcSessions.RemoveRange(await _context.OidcSessions.Where(x => x.ProviderId == provider.Id).ToListAsync());

        _context.OidcProviders.Remove(provider);
        await _context.SaveChangesAsync();

        _configurations.Invalidate(provider.Authority);

        _logger.LogInformation("OIDC provider {ProviderName} deleted with {Count} associated external logins removed",
            provider.Name, externalLogins.Count);
    }

    private async Task<List<ExternalLogin>> LinksOfAsync(string authority, string providerName)
    {
        var candidates = await _context.ExternalLogins
            .Where(x => x.Issuer != null || x.Provider == providerName)
            .ToListAsync();
        return candidates
            .Where(x => x.Issuer == null || OidcIssuer.Matches(x.Issuer, authority))
            .ToList();
    }

    private async Task EnsureValidNewNameAsync(string name, int providerId)
    {
        if (name.Length > 100 || !ProviderNamePattern().IsMatch(name))
        {
            throw new ValidationException(Constants.Errors.OidcProviderNameInvalid);
        }

        if (await _context.OidcProviders.AnyAsync(p => p.Name == name && p.Id != providerId))
        {
            throw new ValidationException(Constants.Errors.OidcProviderNameInvalid);
        }
    }

    private static string? ValidDefaultRole(string? defaultRole)
    {
        if (string.IsNullOrWhiteSpace(defaultRole))
        {
            return null;
        }

        return OidcRoleReconciler.DefaultRoles.Contains(defaultRole)
            ? defaultRole
            : throw new ValidationException(Constants.Errors.InvalidRole);
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]*$")]
    private static partial Regex ProviderNamePattern();

    private static string NormalizeAuthority(string authority) => authority.Trim().TrimEnd('/');

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
