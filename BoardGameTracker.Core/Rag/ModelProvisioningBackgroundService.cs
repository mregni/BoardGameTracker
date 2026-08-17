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
    protected virtual int MaxAttempts => 40;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
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
                    "Could not ensure AI models are available (attempt {Attempt}/{MaxAttempts})",
                    attempt, MaxAttempts);
            }

            try
            {
                await Task.Delay(RetryDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        _logger.LogError("Gave up ensuring AI models are available after {MaxAttempts} attempts", MaxAttempts);
    }
}
