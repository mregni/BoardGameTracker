using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Integrations.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BoardGameTracker.Core.Integrations;

public class IntegrationStatusService : IIntegrationStatusService
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "integration-status:";

    private readonly List<IIntegrationTester> _testers;
    private readonly IMemoryCache _cache;
    private readonly IDateTimeProvider _dateTimeProvider;

    public IntegrationStatusService(
        IEnumerable<IIntegrationTester> testers,
        IMemoryCache cache,
        IDateTimeProvider dateTimeProvider)
    {
        _testers = testers.OrderBy(tester => tester.Integration).ToList();
        _cache = cache;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<IntegrationStatusDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var statuses = new List<IntegrationStatusDto>();
        foreach (var tester in _testers)
        {
            if (_cache.TryGetValue(CacheKey(tester.Integration), out IntegrationStatusDto? cached) && cached != null)
            {
                statuses.Add(cached);
            }
            else
            {
                statuses.Add(await RunAsync(tester, cancellationToken));
            }
        }

        return statuses;
    }

    public Task<IntegrationStatusDto> TestAsync(IntegrationKind integration, CancellationToken cancellationToken = default)
    {
        var tester = _testers.First(candidate => candidate.Integration == integration);
        return RunAsync(tester, cancellationToken);
    }

    public void Invalidate()
    {
        foreach (var integration in Enum.GetValues<IntegrationKind>())
        {
            _cache.Remove(CacheKey(integration));
        }
    }

    private async Task<IntegrationStatusDto> RunAsync(IIntegrationTester tester, CancellationToken cancellationToken)
    {
        var result = await tester.TestAsync(cancellationToken);
        var status = new IntegrationStatusDto
        {
            Integration = tester.Integration,
            Status = result.Status,
            MessageKey = result.MessageKey,
            CheckedAt = _dateTimeProvider.UtcNow
        };

        _cache.Set(CacheKey(tester.Integration), status, CacheDuration);
        return status;
    }

    private static string CacheKey(IntegrationKind integration) => CacheKeyPrefix + integration;
}
