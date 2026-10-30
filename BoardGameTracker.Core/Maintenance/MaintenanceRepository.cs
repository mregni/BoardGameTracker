using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Maintenance.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Maintenance;

public class MaintenanceRepository : IMaintenanceRepository
{
    private readonly MainDbContext _context;

    public MaintenanceRepository(MainDbContext context)
    {
        _context = context;
    }

    public Task<List<string>> GetChangeDetectionWatchIdsAsync(CancellationToken cancellationToken = default)
    {
        return _context.Games
            .Where(g => g.ChangeDetectionWatchId != null)
            .Select(g => g.ChangeDetectionWatchId!)
            .ToListAsync(cancellationToken);
    }

    public async Task ClearUserDataAsync(CancellationToken cancellationToken = default)
    {
        await _context.Set<GameNightRsvp>().ExecuteDeleteAsync(cancellationToken);
        await _context.PlayerSessions.ExecuteDeleteAsync(cancellationToken);
        await _context.Loans.ExecuteDeleteAsync(cancellationToken);
        await _context.GameNights.ExecuteDeleteAsync(cancellationToken);
        await _context.Sessions.ExecuteDeleteAsync(cancellationToken);
        await _context.Expansions.ExecuteDeleteAsync(cancellationToken);
        await _context.GameAccessories.ExecuteDeleteAsync(cancellationToken);
        await _context.Manuals.ExecuteDeleteAsync(cancellationToken);
        await _context.Games.ExecuteDeleteAsync(cancellationToken);
        await _context.People.ExecuteDeleteAsync(cancellationToken);
        await _context.GameCategories.ExecuteDeleteAsync(cancellationToken);
        await _context.GameMechanics.ExecuteDeleteAsync(cancellationToken);
        await _context.Players.ExecuteDeleteAsync(cancellationToken);
        await _context.Locations.ExecuteDeleteAsync(cancellationToken);
        await _context.Users
            .Where(u => u.ProfileImage != null || u.ProfileImageSourceUrl != null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.ProfileImage, (string?)null)
                .SetProperty(u => u.ProfileImageSource, ProfileImageSource.None)
                .SetProperty(u => u.ProfileImageSourceUrl, (string?)null), cancellationToken);
    }

    public async Task ClearSettingsAndAuthAsync(CancellationToken cancellationToken = default)
    {
        await _context.OidcProviders.ExecuteDeleteAsync(cancellationToken);
        await _context.Config.ExecuteDeleteAsync(cancellationToken);
        await _context.Users.ExecuteDeleteAsync(cancellationToken);
        await _context.Roles.ExecuteDeleteAsync(cancellationToken);
    }
}
