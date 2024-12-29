using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Locations.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Locations;

public class LocationRepository : CrudHelper<Location>, ILocationRepository
{
    private readonly MainDbContext _context;

    public LocationRepository(MainDbContext context): base(context)
    {
        _context = context;
    }

    public override Task<List<Location>> GetAllAsync()
    {
        return _context.Locations
            .Include(x => x.Sessions)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public Task<int> CountAsync()
    {
        return _context.Locations.CountAsync();
    }
}