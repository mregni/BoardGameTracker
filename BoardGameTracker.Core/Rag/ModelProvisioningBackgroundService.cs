using BoardGameTracker.Core.Rag.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Rag;

public class ModelProvisioningBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ModelProvisioningBackgroundService> _logger;

    public ModelProvisioningBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ModelProvisioningBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected virtual TimeSpan RetryDelay => TimeSpan.FromSeconds(15);
    protected virtual TimeSpan MaxRetryDelay => TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = RetryDelay;
        for (var attempt = 1; !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var aiClientFactory = scope.ServiceProvider.GetRequiredService<IAiClientFactory>();
                await aiClientFactory.EnsureModelsAvailableAsync(stoppingToken);
                _logger.LogInformation("AI models are available");
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Could not ensure AI models are available (attempt {Attempt}), retrying in {Delay}",
                    attempt, delay);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            delay = delay * 2 > MaxRetryDelay ? MaxRetryDelay : delay * 2;
        }
    }
}
