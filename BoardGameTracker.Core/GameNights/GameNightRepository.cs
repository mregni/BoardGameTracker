using Ardalis.Specification.EntityFrameworkCore;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.GameNights.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.GameNights;

public class GameNightRepository : EfRepository<GameNight>, IGameNightRepository
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
        return SingleOrDefaultAsync(new GameNightByIdWithDetailsSpec(id));
    }

    public override Task<List<GameNight>> GetAllAsync()
    {
        return ListAsync(new GameNightsOverviewSpec());
    }

    public Task<GameNightRsvp?> GetRsvpByIdAsync(int rsvpId)
    {
        return _context.Set<GameNightRsvp>()
            .WithSpecification(new RsvpByIdSpec(rsvpId))
            .SingleOrDefaultAsync();
    }

    public Task<GameNightRsvp> UpdateRsvpAsync(GameNightRsvp rsvp)
    {
        _context.Set<GameNightRsvp>().Update(rsvp);
        return Task.FromResult(rsvp);
    }

    public Task<int> GetFutureGameNightsCountAsync()
    {
        return CountAsync(new FutureGameNightsSpec(_dateTimeProvider.UtcNow));
    }

    public Task<GameNightRsvp?> GetRsvpByPlayerAndGameAsync(int commandPlayerId, int commandGameNightId)
    {
        return _context.Set<GameNightRsvp>()
            .WithSpecification(new RsvpByPlayerAndGameNightSpec(commandPlayerId, commandGameNightId))
            .SingleOrDefaultAsync();
    }

    public Task<GameNight?> GetGameNightByLinkId(Guid linkId)
    {
        return SingleOrDefaultAsync(new GameNightByLinkIdSpec(linkId));
    }
}
