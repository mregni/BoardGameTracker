using BoardGameTracker.Common;
using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Maintenance.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Maintenance;

public class MaintenanceSeeder : IMaintenanceSeeder
{
    private readonly IConfigRepository _configRepository;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<MaintenanceSeeder> _logger;

    public MaintenanceSeeder(
        IConfigRepository configRepository,
        IEnvironmentProvider environmentProvider,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<MaintenanceSeeder> logger)
    {
        _configRepository = configRepository;
        _environmentProvider = environmentProvider;
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task EnsureAdminPasswordAcceptedAsync(CancellationToken cancellationToken = default)
    {
        if (!_environmentProvider.AuthEnabled)
        {
            return;
        }

        var errors = await DbSeeder.GetAdminPasswordErrorsAsync(_userManager, _environmentProvider.AdminPassword);
        if (errors.Count > 0)
        {
            _logger.LogWarning("Factory reset refused: ADMIN_PASSWORD does not meet the password rules");
            throw new ValidationException(Constants.Errors.AdminPasswordRejected);
        }
    }

    public async Task ReseedDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await _configRepository.SeedConfigAsync(ConfigDefaults.All);
        if (_environmentProvider.AuthEnabled)
        {
            await DbSeeder.SeedAuthData(_roleManager, _userManager, _logger, _environmentProvider.AdminPassword);
        }
    }
}
