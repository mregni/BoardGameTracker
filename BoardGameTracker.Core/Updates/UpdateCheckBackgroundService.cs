using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static BoardGameTracker.Common.Constants;

namespace BoardGameTracker.Core.Updates;

public class UpdateCheckBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateCheckBackgroundService> _logger;
    private readonly TimeSpan _defaultInterval = TimeSpan.FromHours(24);

    public UpdateCheckBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<UpdateCheckBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Update Check Background Service started");

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformUpdateCheckAsync();

                var interval = await GetCheckIntervalAsync();
                _logger.LogInformation("Next update check in {Hours} hours", interval.TotalHours);
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Update Check Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Update Check Background Service");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task PerformUpdateCheckAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var updateService = scope.ServiceProvider.GetRequiredService<IUpdateService>();

        var repository = scope.ServiceProvider.GetRequiredService<IUpdateRepository>();
        var enabled = await repository.GetConfigValueAsync(UpdateConfig.CheckEnabled, true);

        if (!enabled)
        {
            _logger.LogInformation("Update checks are disabled");
            return;
        }

        await updateService.CheckForUpdatesAsync();
    }

    private async Task<TimeSpan> GetCheckIntervalAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUpdateRepository>();

        var hours = await repository.GetConfigValueAsync<int>(UpdateConfig.CheckIntervalHours);

        if (hours > 0)
        {
            return TimeSpan.FromHours(hours);
        }

        await repository.SetConfigValueAsync(UpdateConfig.CheckIntervalHours, 24);
        return _defaultInterval;
    }
}
