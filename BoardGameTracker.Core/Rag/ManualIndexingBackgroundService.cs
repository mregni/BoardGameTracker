using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Rag;

public class ManualIndexingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IManualIndexingQueue _queue;
    private readonly ILogger<ManualIndexingBackgroundService> _logger;

    public ManualIndexingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IManualIndexingQueue queue,
        ILogger<ManualIndexingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BackfillAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            int manualId;
            try
            {
                manualId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var indexingService = scope.ServiceProvider.GetRequiredService<IManualIndexingService>();
                await indexingService.IndexAsync(manualId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while indexing manual {ManualId}", manualId);
            }
        }
    }

    private async Task BackfillAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var indexingService = scope.ServiceProvider.GetRequiredService<IManualIndexingService>();
            await indexingService.EnqueuePendingAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue pending manuals for indexing");
        }
    }
}
