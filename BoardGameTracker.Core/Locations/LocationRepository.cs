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
        return _context.Locations
            .Include(x => x.Plays)
            .ToListAsync();
    }

    public async Task Create(Location location)
    {
        await _context.Locations.AddAsync(location);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        var location = await _context.Locations.FirstOrDefaultAsync(x => x.Id == id);
        if (location == null)
        {
            return;
        }
        
        _context.Remove(location);
        await _context.SaveChangesAsync();
    }

    public async Task Update(Location location)
    {
        var dbLocation = await _context.Locations
            .SingleOrDefaultAsync(x => x.Id == location.Id);
        if (dbLocation != null)
        {
            dbLocation.Name = location.Name;
            await _context.SaveChangesAsync();
        }
    }
}