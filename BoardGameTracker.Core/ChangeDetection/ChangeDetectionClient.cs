using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RecheckThrottle = TimeSpan.FromMinutes(10);
    private const string CacheKeyPrefix = "changedetection:watch:";
    private const string RecheckKeyPrefix = "changedetection:recheck:";

    private const int MaxConcurrentFetches = 4;
    private static readonly SemaphoreSlim FetchGate = new(MaxConcurrentFetches, MaxConcurrentFetches);

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
            return ChangeDetectionResult.Unavailable(ChangeDetectionStatus.NotConfigured);
        }

        if (!forceRefresh && TryGetCached(watchId, out var cached))
        {
            return cached;
        }

        var (client, status) = await TryCreateClientAsync();
        if (client == null)
        {
            return ChangeDetectionResult.Unavailable(status);
        }

        return await FetchAndCacheAsync(client, watchId, forceRefresh, cancellationToken);
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

        var (client, status) = await TryCreateClientAsync();
        if (client == null)
        {
            foreach (var watchId in toFetch)
            {
                resolved[watchId] = ChangeDetectionResult.Unavailable(status);
            }

            return resolved;
        }

        var fetched = await Task.WhenAll(toFetch.Select(async watchId =>
            new KeyValuePair<string, ChangeDetectionResult>(
                watchId,
                await FetchAndCacheAsync(client, watchId, forceRefresh, cancellationToken))));

        foreach (var (watchId, result) in fetched)
        {
            resolved[watchId] = result;
        }

        return resolved;
    }

    public async Task<(ChangeDetectionStatus Status, ChangeDetectionWatchInfo? Info)> GetWatchInfoAsync(
        string watchId,
        CancellationToken cancellationToken = default)
    {
        var (client, status) = await TryCreateClientAsync();
        if (client == null)
        {
            return (status, null);
        }

        return await GuardedAsync(async () =>
        {
            var (watchStatus, watch) = await FetchWatchAsync(client, watchId, cancellationToken);
            return watch == null ? (watchStatus, null) : (ChangeDetectionStatus.Ok, ToWatchInfo(watch.Value));
        }, (ChangeDetectionStatus.Unreachable, (ChangeDetectionWatchInfo?)null), "watch lookup", cancellationToken);
    }

    public async Task<(ChangeDetectionStatus Status, string? WatchId)> CreateWatchAsync(
        string url,
        string title,
        CancellationToken cancellationToken = default)
    {
        var (client, status) = await TryCreateClientAsync();
        if (client == null)
        {
            return (status, null);
        }

        return await GuardedAsync(async () =>
        {
            var payload = new
            {
                url,
                title,
                processor = "restock_diff",
                tag = "boardgametracker",
                time_between_check = new { hours = 24 }
            };

            var response = await client.PostAsJsonAsync("api/v1/watch", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("changedetection.io refused to create a watch for {Url}: {StatusCode} {Error}",
                    url, response.StatusCode, error.Length > 500 ? error[..500] : error);
                return (MapStatus(response.StatusCode), (string?)null);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var uuid = ExtractUuid(body);
            if (uuid == null)
            {
                _logger.LogWarning("changedetection.io created a watch for {Url} but returned no uuid", url);
                return (ChangeDetectionStatus.ParseError, null);
            }

            return (ChangeDetectionStatus.Ok, uuid);
        }, (ChangeDetectionStatus.Unreachable, (string?)null), "create watch", cancellationToken);
    }

    public async Task<(bool Ok, string? Version)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var (client, _) = await TryCreateClientAsync();
        if (client == null)
        {
            return (false, null);
        }

        return await GuardedAsync(async () =>
        {
            var response = await client.GetAsync("api/v1/systeminfo", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (false, (string?)null);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var info = JsonSerializer.Deserialize<JsonElement>(body);
            return (true, GetString(info, "version"));
        }, (false, (string?)null), "connection test", cancellationToken);
    }

    private bool TryGetCached(string watchId, out ChangeDetectionResult result)
    {
        if (_cache.TryGetValue(CacheKeyPrefix + watchId, out ChangeDetectionResult? cached) && cached != null)
        {
            result = cached;
            return true;
        }

        result = ChangeDetectionResult.Unavailable(ChangeDetectionStatus.NotConfigured);
        return false;
    }

    private async Task<(HttpClient? Client, ChangeDetectionStatus Status)> TryCreateClientAsync()
    {
        var (baseUrl, apiKey) = await _settingsService.GetChangeDetectionSettingsAsync();
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            return (null, ChangeDetectionStatus.NotConfigured);
        }

        var client = TryCreateClient(baseUrl, apiKey);
        return client == null
            ? (null, ChangeDetectionStatus.Misconfigured)
            : (client, ChangeDetectionStatus.Ok);
    }

    private HttpClient? TryCreateClient(string baseUrl, string apiKey)
    {
        if (!Uri.TryCreate(baseUrl.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogWarning("changedetection.io base URL is invalid: {BaseUrl}", baseUrl);
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            client.BaseAddress = uri;
            client.DefaultRequestHeaders.Remove("x-api-key");
            client.DefaultRequestHeaders.Add("x-api-key", apiKey.Trim());
            return client;
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "changedetection.io API key is invalid");
            return null;
        }
    }

    private async Task<ChangeDetectionResult> FetchAndCacheAsync(
        HttpClient client,
        string watchId,
        bool recheck,
        CancellationToken cancellationToken)
    {
        var result = await FetchAsync(client, watchId, recheck, cancellationToken);
        result.FetchedAt = _dateTimeProvider.UtcNow;

        var ttl = result.Available ? CacheDuration : FailureCacheDuration;
        _cache.Set(CacheKeyPrefix + watchId, result, ttl);

        return result;
    }

    private Task<ChangeDetectionResult> FetchAsync(
        HttpClient client,
        string watchId,
        bool recheck,
        CancellationToken cancellationToken)
    {
        return GuardedAsync(async () =>
        {
            var queueRecheck = recheck && await TryQueueRecheckAsync(client, watchId, cancellationToken);
            var (watchStatus, watch) = await FetchWatchAsync(client, watchId, cancellationToken);
            if (watch == null)
            {
                return ChangeDetectionResult.Unavailable(watchStatus);
            }

            var result = watch.Value.TryGetProperty("restock", out var restock) ? ParseRestock(restock) : null;
            if (result == null)
            {
                var response = await client.GetAsync($"api/v1/watch/{watchId}/history/latest", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("changedetection.io returned {StatusCode} for the latest snapshot of watch {WatchId}",
                        response.StatusCode, watchId);
                    var unavailable = ChangeDetectionResult.Unavailable(response.StatusCode == HttpStatusCode.NotFound
                        ? ChangeDetectionStatus.Pending
                        : MapStatus(response.StatusCode));
                    ApplyWatchMetadata(unavailable, watch.Value);
                    return unavailable;
                }

                result = ChangeDetectionSnapshotParser.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            }

            ApplyWatchMetadata(result, watch.Value);
            result.RecheckQueued = queueRecheck;
            return result;
        }, ChangeDetectionResult.Unavailable(ChangeDetectionStatus.Unreachable), $"fetch for watch {watchId}", cancellationToken);
    }

    private async Task<(ChangeDetectionStatus Status, JsonElement? Watch)> FetchWatchAsync(
        HttpClient client,
        string watchId,
        CancellationToken cancellationToken)
    {
        var path = $"api/v1/watch/{watchId}";
        var response = await client.GetAsync(path, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var status = MapStatus(response.StatusCode);
            if (status == ChangeDetectionStatus.Unreachable)
            {
                _logger.LogDebug("changedetection.io returned {StatusCode} for watch {WatchId}",
                    response.StatusCode, watchId);
            }
            else
            {
                _logger.LogWarning("changedetection.io returned {StatusCode} for watch {WatchId}",
                    response.StatusCode, watchId);
            }

            return (status, null);
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var watch = JsonSerializer.Deserialize<JsonElement>(body);
            return watch.ValueKind == JsonValueKind.Object
                ? (ChangeDetectionStatus.Ok, watch)
                : (ChangeDetectionStatus.ParseError, null);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "changedetection.io returned an unreadable watch object for {WatchId}", watchId);
            return (ChangeDetectionStatus.ParseError, null);
        }
    }

    private async Task<bool> TryQueueRecheckAsync(HttpClient client, string watchId, CancellationToken cancellationToken)
    {
        if (!TryClaimRecheck(watchId))
        {
            return false;
        }

        var response = await client.GetAsync($"api/v1/watch/{watchId}?recheck=1", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        _logger.LogWarning("changedetection.io refused to queue a recheck for watch {WatchId}: {StatusCode}",
            watchId, response.StatusCode);
        _cache.Remove(RecheckKeyPrefix + watchId);
        return false;
    }

    private bool TryClaimRecheck(string watchId)
    {
        var key = RecheckKeyPrefix + watchId;
        if (_cache.TryGetValue(key, out _))
        {
            return false;
        }

        _cache.Set(key, true, RecheckThrottle);
        return true;
    }

    private async Task<T> GuardedAsync<T>(Func<Task<T>> action, T fallback, string operation, CancellationToken cancellationToken)
    {
        await FetchGate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "changedetection.io {Operation} failed", operation);
            return fallback;
        }
        finally
        {
            FetchGate.Release();
        }
    }

    private static ChangeDetectionResult? ParseRestock(JsonElement restock)
    {
        if (restock.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        decimal? price = null;
        if (restock.TryGetProperty("price", out var priceElement))
        {
            if (priceElement.ValueKind == JsonValueKind.Number && priceElement.TryGetDecimal(out var numeric))
            {
                price = numeric;
            }
            else if (priceElement.ValueKind == JsonValueKind.String &&
                     decimal.TryParse(priceElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                price = parsed;
            }
        }

        bool? inStock = restock.TryGetProperty("in_stock", out var stockElement) &&
                        stockElement.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? stockElement.GetBoolean()
            : null;

        if (price == null && inStock == null)
        {
            return null;
        }

        return new ChangeDetectionResult
        {
            Status = ChangeDetectionStatus.Ok,
            Price = price,
            InStock = inStock,
            Currency = GetString(restock, "currency")
        };
    }

    private static void ApplyWatchMetadata(ChangeDetectionResult result, JsonElement watch)
    {
        result.SourceUrl = GetString(watch, "url");
        result.Title = GetString(watch, "title");
        result.CheckedAt = GetUnixTime(watch, "last_checked");

        if (result.Currency == null && watch.TryGetProperty("restock", out var restock))
        {
            result.Currency = GetString(restock, "currency");
        }
    }

    private static ChangeDetectionWatchInfo ToWatchInfo(JsonElement watch)
    {
        return new ChangeDetectionWatchInfo(
            GetString(watch, "url") ?? string.Empty,
            GetString(watch, "title"),
            GetUnixTime(watch, "last_checked"));
    }

    private static string? ExtractUuid(string body)
    {
        try
        {
            var element = JsonSerializer.Deserialize<JsonElement>(body);
            return element.ValueKind switch
            {
                JsonValueKind.Object => GetString(element, "uuid"),
                JsonValueKind.String => element.GetString(),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
        {
            return null;
        }

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static DateTime? GetUnixTime(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(property, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetDouble(out var seconds) ||
            seconds <= 0)
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds((long)seconds).UtcDateTime;
    }

    private static ChangeDetectionStatus MapStatus(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ChangeDetectionStatus.Unauthorized,
        HttpStatusCode.NotFound => ChangeDetectionStatus.WatchNotFound,
        _ when (int)statusCode is >= 300 and < 400 => ChangeDetectionStatus.Misconfigured,
        _ => ChangeDetectionStatus.Unreachable
    };
}
