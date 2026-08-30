using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.ChangeDetection;

public class ChangeDetectionClient : IChangeDetectionClient
{
    public const string HttpClientName = "changedetection";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    private const string CacheKeyPrefix = "changedetection:watch:";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsService _settingsService;
    private readonly IMemoryCache _cache;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ChangeDetectionClient> _logger;

    public ChangeDetectionClient(
        IHttpClientFactory httpClientFactory,
        ISettingsService settingsService,
        IMemoryCache cache,
        IDateTimeProvider dateTimeProvider,
        ILogger<ChangeDetectionClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settingsService = settingsService;
        _cache = cache;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<ChangeDetectionResult> GetLatestAsync(
        string watchId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(watchId))
        {
            return ChangeDetectionResult.Unavailable();
        }

        if (!forceRefresh && TryGetCached(watchId, out var cached))
        {
            return cached;
        }

        var (baseUrl, apiKey) = await _settingsService.GetChangeDetectionSettingsAsync();
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            return ChangeDetectionResult.Unavailable();
        }

        var client = CreateClient(baseUrl, apiKey);
        return await FetchAndCacheAsync(client, watchId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, ChangeDetectionResult>> GetLatestAsync(
        IReadOnlyCollection<string> watchIds,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var distinctIds = watchIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (distinctIds.Count == 0)
        {
            return new Dictionary<string, ChangeDetectionResult>();
        }

        var resolved = new Dictionary<string, ChangeDetectionResult>();
        var toFetch = new List<string>();
        foreach (var watchId in distinctIds)
        {
            if (!forceRefresh && TryGetCached(watchId, out var cached))
            {
                resolved[watchId] = cached;
            }
            else
            {
                toFetch.Add(watchId);
            }
        }

        if (toFetch.Count == 0)
        {
            return resolved;
        }

        var (baseUrl, apiKey) = await _settingsService.GetChangeDetectionSettingsAsync();
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            foreach (var watchId in toFetch)
            {
                resolved[watchId] = ChangeDetectionResult.Unavailable();
            }

            return resolved;
        }

        var client = CreateClient(baseUrl, apiKey);
        var fetched = await Task.WhenAll(toFetch.Select(async watchId =>
            new KeyValuePair<string, ChangeDetectionResult>(
                watchId,
                await FetchAndCacheAsync(client, watchId, cancellationToken))));

        foreach (var (watchId, result) in fetched)
        {
            resolved[watchId] = result;
        }

        return resolved;
    }

    private bool TryGetCached(string watchId, out ChangeDetectionResult result)
    {
        if (_cache.TryGetValue(CacheKeyPrefix + watchId, out ChangeDetectionResult? cached) && cached != null)
        {
            result = cached;
            return true;
        }

        result = ChangeDetectionResult.Unavailable();
        return false;
    }

    private HttpClient CreateClient(string baseUrl, string apiKey)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Remove("x-api-key");
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        return client;
    }

    private async Task<ChangeDetectionResult> FetchAndCacheAsync(
        HttpClient client,
        string watchId,
        CancellationToken cancellationToken)
    {
        var result = await FetchAsync(client, watchId, cancellationToken);
        if (result.Available)
        {
            result.FetchedAt = _dateTimeProvider.UtcNow;
            _cache.Set(CacheKeyPrefix + watchId, result, CacheDuration);
        }

        return result;
    }

    private async Task<ChangeDetectionResult> FetchAsync(
        HttpClient client,
        string watchId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.GetAsync($"api/v1/watch/{watchId}/history/latest", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("changedetection.io returned {StatusCode} for watch {WatchId}",
                    response.StatusCode, watchId);
                return ChangeDetectionResult.Unavailable();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return ChangeDetectionSnapshotParser.Parse(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch changedetection.io data for watch {WatchId}", watchId);
            return ChangeDetectionResult.Unavailable();
        }
    }
}
