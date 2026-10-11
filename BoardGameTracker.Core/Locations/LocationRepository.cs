using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Locations;

public class LocationRepository : EfRepository<Location>, ILocationRepository
{
    private readonly MainDbContext _dbContext;
    private readonly IReadRepository<Session> _sessionReadRepository;

    public LocationRepository(MainDbContext dbContext, IReadRepository<Session> sessionReadRepository) : base(dbContext)
    {
        _dbContext = dbContext;
        _sessionReadRepository = sessionReadRepository;
    }

    public Task<int> GetSessionCount(int id)
    {
        return _sessionReadRepository.CountAsync(new SessionsByLocationSpec(id));
    }

    public Task<int> GetDistinctGameCount(int id)
    {
        return _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.LocationId == id)
            .Select(x => x.GameId)
            .Distinct()
            .CountAsync();
    }

    public Task<int> GetDistinctPlayerCount(int id)
    {
        return _dbContext.PlayerSessions
            .AsNoTracking()
            .Where(x => x.Session.LocationId == id)
            .Select(x => x.PlayerId)
            .Distinct()
            .CountAsync();
    }

    public Task<double> GetPlayLengthInMinutes(int id)
    {
        return _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.LocationId == id)
            .SumAsync(x => (x.End - x.Start).TotalMinutes);
    }
}
