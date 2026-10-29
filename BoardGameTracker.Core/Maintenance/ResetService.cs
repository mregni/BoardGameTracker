using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Maintenance.Interfaces;
using BoardGameTracker.Core.Manuals.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Maintenance;

public class ResetService : IResetService
{
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageService _imageService;
    private readonly IManualService _manualService;
    private readonly IMaintenanceSeeder _maintenanceSeeder;
    private readonly IChangeDetectionClient _changeDetectionClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ResetService> _logger;

    public ResetService(
        IMaintenanceRepository maintenanceRepository,
        IUnitOfWork unitOfWork,
        IImageService imageService,
        IManualService manualService,
        IMaintenanceSeeder maintenanceSeeder,
        IChangeDetectionClient changeDetectionClient,
        ISettingsService settingsService,
        ILogger<ResetService> logger)
    {
        _maintenanceRepository = maintenanceRepository;
        _unitOfWork = unitOfWork;
        _imageService = imageService;
        _manualService = manualService;
        _maintenanceSeeder = maintenanceSeeder;
        _changeDetectionClient = changeDetectionClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task ResetDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting data reset");
        var watches = await GetWatchesAsync(cancellationToken);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _maintenanceRepository.ClearUserDataAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _imageService.ClearAllImages();
        _manualService.ClearAllManuals();
        await DeleteWatchesQuietlyAsync(watches);

        _logger.LogInformation("Data reset completed");
    }

    public async Task FactoryResetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting factory reset");
        await _maintenanceSeeder.EnsureAdminPasswordAcceptedAsync(cancellationToken);
        var watches = await GetWatchesAsync(cancellationToken);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _maintenanceRepository.ClearUserDataAsync(cancellationToken);
        await _maintenanceRepository.ClearSettingsAndAuthAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _imageService.ClearAllImages();
        _manualService.ClearAllManuals();

        await _maintenanceSeeder.ReseedDefaultsAsync(cancellationToken);
        await DeleteWatchesQuietlyAsync(watches);

        _logger.LogInformation("Factory reset completed");
    }

    private async Task<(List<string> Ids, string? BaseUrl, string? ApiKey)> GetWatchesAsync(CancellationToken cancellationToken)
    {
        var ids = await _maintenanceRepository.GetChangeDetectionWatchIdsAsync(cancellationToken);
        if (ids.Count == 0)
        {
            return (ids, null, null);
        }

        var (baseUrl, apiKey) = await _settingsService.GetChangeDetectionSettingsAsync();
        return (ids, baseUrl, apiKey);
    }

    private async Task DeleteWatchesQuietlyAsync((List<string> Ids, string? BaseUrl, string? ApiKey) watches)
    {
        foreach (var watchId in watches.Ids)
        {
            try
            {
                var status = await _changeDetectionClient.DeleteWatchAsync(watchId, watches.BaseUrl, watches.ApiKey);
                if (status != ChangeDetectionStatus.Ok)
                {
                    _logger.LogWarning("Could not remove price watch {WatchId}: {Status}", watchId, status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not remove price watch {WatchId}", watchId);
            }
        }
    }
}
