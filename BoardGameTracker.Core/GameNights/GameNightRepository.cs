using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.GameNights.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.GameNights;

public class GameNightRepository : CrudHelper<GameNight>, IGameNightRepository
{
    private readonly MainDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GameNightRepository(MainDbContext context, IDateTimeProvider dateTimeProvider) : base(context)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public override Task<GameNight?> GetByIdAsync(int id)
    {
        return _context.GameNights
            .Include(x => x.Host)
            .Include(x => x.Location)
            .Include(x => x.SuggestedGames)
            .Include(x => x.InvitedPlayers)
                .ThenInclude(x => x.Player)
            .SingleOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<GameNight>> GetFutureGameNightsAsync()
    {
        return _context.GameNights
            .AsNoTracking()
            .Include(x => x.Host)
            .Include(x => x.Location)
            .Include(x => x.SuggestedGames)
            .Include(x => x.InvitedPlayers)
                .ThenInclude(x => x.Player)
            .Where(x => x.StartDate >= _dateTimeProvider.UtcNow)
            .OrderBy(x => x.StartDate)
            .ToListAsync();
    }

    public Task<List<GameNight>> GetPastGameNightsAsync()
    {
        return _context.GameNights
            .AsNoTracking()
            .Include(x => x.Host)
            .Include(x => x.Location)
            .Include(x => x.SuggestedGames)
            .Include(x => x.InvitedPlayers)
                .ThenInclude(x => x.Player)
            .Where(x => x.StartDate < _dateTimeProvider.UtcNow)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();
    }

    public Task<GameNightRsvp?> GetRsvpByIdAsync(int rsvpId)
    {
        return _context.Set<GameNightRsvp>()
            .Include(x => x.Player)
            .SingleOrDefaultAsync(x => x.Id == rsvpId);
    }

    public Task<GameNightRsvp> UpdateRsvpAsync(GameNightRsvp rsvp)
    {
        _context.Set<GameNightRsvp>().Update(rsvp);
        return Task.FromResult(rsvp);
    }

    public Task<int> GetFutureGameNightsCountAsync()
    {
        return _context.GameNights
            .AsNoTracking()
            .Where(x => x.StartDate >= _dateTimeProvider.UtcNow)
            .CountAsync();
    }
}
