using BoardGameTracker.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Settings;

public class IntegrationKeyMigrator : IIntegrationKeyMigrator
{
    public static readonly IReadOnlyList<string> ProtectedSettings =
    [
        Constants.BggConfig.ApiKey,
        Constants.ChangeDetectionConfig.ApiKey
    ];

    private readonly IConfigRepository _configRepository;
    private readonly IIntegrationKeyProtector _protector;
    private readonly ILogger<IntegrationKeyMigrator> _logger;

    public IntegrationKeyMigrator(
        IConfigRepository configRepository,
        IIntegrationKeyProtector protector,
        ILogger<IntegrationKeyMigrator> logger)
    {
        _configRepository = configRepository;
        _protector = protector;
        _logger = logger;
    }

    public async Task<int> ProtectLegacyKeysAsync()
    {
        var configs = await _configRepository.GetAllConfigsAsync();
        var migrated = 0;
        foreach (var setting in ProtectedSettings)
        {
            if (!configs.TryGetValue(setting, out var stored) ||
                string.IsNullOrWhiteSpace(stored) ||
                _protector.IsProtected(stored))
            {
                continue;
            }

            var protectedValue = _protector.Protect(stored);
            if (_protector.Reveal(setting, protectedValue) != stored)
            {
                _logger.LogWarning("The stored {Setting} was left unencrypted because the encrypted value could not be read back", setting);
                continue;
            }

            if (await _configRepository.ReplaceConfigValueAsync(setting, stored, protectedValue))
            {
                _logger.LogInformation("Encrypted the stored {Setting} at rest", setting);
                migrated++;
            }
        }

        return migrated;
    }
}
