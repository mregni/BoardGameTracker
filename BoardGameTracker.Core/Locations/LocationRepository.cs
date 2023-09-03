using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Locations.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Locations;

public class LocationRepository : ILocationRepository
{
    private readonly MainDbContext _context;

    public LocationRepository(MainDbContext context)
    {
        _context = context;
    }
    
    public Task<List<Location>> GetLocations()
    {
        return _context.Locations.ToListAsync();
    }
}