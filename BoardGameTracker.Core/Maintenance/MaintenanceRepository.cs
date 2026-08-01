using BoardGameTracker.Common.Entities;
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

    public async Task ClearUserDataAsync(CancellationToken cancellationToken = default)
    {
        await _context.Set<Image>().ExecuteDeleteAsync(cancellationToken);
        await _context.Set<GameNightRsvp>().ExecuteDeleteAsync(cancellationToken);
        await _context.PlayerSessions.ExecuteDeleteAsync(cancellationToken);
        await _context.Loans.ExecuteDeleteAsync(cancellationToken);
        await _context.GameNights.ExecuteDeleteAsync(cancellationToken);
        await _context.Sessions.ExecuteDeleteAsync(cancellationToken);
        await _context.Expansions.ExecuteDeleteAsync(cancellationToken);
        await _context.GameAccessories.ExecuteDeleteAsync(cancellationToken);
        await _context.Games.ExecuteDeleteAsync(cancellationToken);
        await _context.People.ExecuteDeleteAsync(cancellationToken);
        await _context.GameCategories.ExecuteDeleteAsync(cancellationToken);
        await _context.GameMechanics.ExecuteDeleteAsync(cancellationToken);
        await _context.Players.ExecuteDeleteAsync(cancellationToken);
        await _context.Locations.ExecuteDeleteAsync(cancellationToken);
    }

    public async Task ClearSettingsAndAuthAsync(CancellationToken cancellationToken = default)
    {
        await _context.OidcProviders.ExecuteDeleteAsync(cancellationToken);
        await _context.Config.ExecuteDeleteAsync(cancellationToken);
        await _context.Users.ExecuteDeleteAsync(cancellationToken);
        await _context.Roles.ExecuteDeleteAsync(cancellationToken);
    }
}
