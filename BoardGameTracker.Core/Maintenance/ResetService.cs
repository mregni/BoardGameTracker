using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Maintenance.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Maintenance;

public class ResetService : IResetService
{
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageService _imageService;
    private readonly IMaintenanceSeeder _maintenanceSeeder;
    private readonly ILogger<ResetService> _logger;

    public ResetService(
        IMaintenanceRepository maintenanceRepository,
        IUnitOfWork unitOfWork,
        IImageService imageService,
        IMaintenanceSeeder maintenanceSeeder,
        ILogger<ResetService> logger)
    {
        _maintenanceRepository = maintenanceRepository;
        _unitOfWork = unitOfWork;
        _imageService = imageService;
        _maintenanceSeeder = maintenanceSeeder;
        _logger = logger;
    }

    public async Task ResetDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting data reset");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _maintenanceRepository.ClearUserDataAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _imageService.ClearAllImages();

        _logger.LogInformation("Data reset completed");
    }

    public async Task FactoryResetAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting factory reset");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        await _maintenanceRepository.ClearUserDataAsync(cancellationToken);
        await _maintenanceRepository.ClearSettingsAndAuthAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _imageService.ClearAllImages();

        await _maintenanceSeeder.ReseedDefaultsAsync(cancellationToken);

        _logger.LogInformation("Factory reset completed");
    }
}
