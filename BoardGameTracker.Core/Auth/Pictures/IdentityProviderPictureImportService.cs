using BoardGameTracker.Core.Auth.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth.Pictures;

public class IdentityProviderPictureImportService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IIdentityProviderPictureQueue _queue;
    private readonly ILogger<IdentityProviderPictureImportService> _logger;

    public IdentityProviderPictureImportService(
        IServiceScopeFactory scopeFactory,
        IIdentityProviderPictureQueue queue,
        ILogger<IdentityProviderPictureImportService> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IdentityProviderPictureImport import;
            try
            {
                import = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ImportAsync(import, stoppingToken);
        }
    }

    public async Task ImportAsync(IdentityProviderPictureImport import, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var images = scope.ServiceProvider.GetRequiredService<IUserProfileImageService>();
            await images.ImportFromIdentityProviderAsync(import, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Stopped importing the identity provider picture of user {UserId} during shutdown", import.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not import the identity provider picture of user {UserId}", import.UserId);
        }
    }
}
