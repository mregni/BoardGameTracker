using BoardGameTracker.Core.Updates.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        var enabled = await repository.GetConfigValueAsync("update_check_enabled");

        if (enabled == "false")
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

        var intervalStr = await repository.GetConfigValueAsync("update_check_interval_hours");

        if (int.TryParse(intervalStr, out var hours) && hours > 0)
        {
            return TimeSpan.FromHours(hours);
        }

        await repository.SetConfigValueAsync("update_check_interval_hours", "24");
        return _defaultInterval;
    }
}
