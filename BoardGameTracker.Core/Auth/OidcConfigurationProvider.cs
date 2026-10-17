using System.Collections.Concurrent;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Common;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace BoardGameTracker.Core.Auth;

public class OidcConfigurationProvider : IOidcConfigurationProvider
{
    private readonly ConcurrentDictionary<string, CachedConfiguration> _configurations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _refreshLock = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OidcConfigurationProvider> _logger;

    public OidcConfigurationProvider(IHttpClientFactory httpClientFactory, ILogger<OidcConfigurationProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public TimeSpan MinimumRefreshInterval { get; init; } = TimeSpan.FromSeconds(30);

    public TimeSpan AutomaticRefreshInterval { get; init; } = TimeSpan.FromHours(1);

    public Task<OpenIdConnectConfiguration> GetAsync(string authority, CancellationToken cancellationToken = default)
    {
        var key = OidcIssuer.Normalize(authority);
        return LoadAsync(key, _configurations.GetOrAdd(key, Create), cancellationToken);
    }

    public Task<OpenIdConnectConfiguration> RefreshAsync(string authority, CancellationToken cancellationToken = default)
    {
        var key = OidcIssuer.Normalize(authority);
        CachedConfiguration entry;
        lock (_refreshLock)
        {
            entry = _configurations.GetOrAdd(key, Create);
            if (DateTimeOffset.UtcNow - entry.CreatedAt >= MinimumRefreshInterval)
            {
                _logger.LogInformation("Refetching the OIDC configuration and signing keys of {Authority}", key);
                entry = Create(key);
                _configurations[key] = entry;
            }
        }

        return LoadAsync(key, entry, cancellationToken);
    }

    public void Invalidate(string authority)
    {
        _configurations.TryRemove(OidcIssuer.Normalize(authority), out _);
    }

    private async Task<OpenIdConnectConfiguration> LoadAsync(string key, CachedConfiguration entry, CancellationToken cancellationToken)
    {
        try
        {
            return await entry.Manager.GetConfigurationAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _configurations.TryRemove(new KeyValuePair<string, CachedConfiguration>(key, entry));
            _logger.LogError(ex, "The OIDC configuration of {Authority} could not be loaded", key);
            throw new DomainException(Constants.Errors.OidcDiscoveryFailed);
        }
    }

    private CachedConfiguration Create(string key)
    {
        var manager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{key}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new GuardedDocumentRetriever(_httpClientFactory))
        {
            AutomaticRefreshInterval = AutomaticRefreshInterval,
        };

        return new CachedConfiguration(manager, DateTimeOffset.UtcNow);
    }

    private sealed record CachedConfiguration(ConfigurationManager<OpenIdConnectConfiguration> Manager, DateTimeOffset CreatedAt);

    private sealed class GuardedDocumentRetriever(IHttpClientFactory httpClientFactory) : IDocumentRetriever
    {
        public async Task<string> GetDocumentAsync(string address, CancellationToken cancel)
        {
            if (!SecureUrlPolicy.IsAcceptable(address))
            {
                throw new InvalidOperationException($"Refusing to fetch OIDC metadata over an insecure address: {address}");
            }

            var client = httpClientFactory.CreateClient(OidcService.HttpClientName);
            using var response = await client.GetAsync(address, cancel);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancel);
        }
    }
}
