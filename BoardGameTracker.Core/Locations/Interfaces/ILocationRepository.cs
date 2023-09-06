using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Interfaces;

public interface ILocationRepository
{
    public Task<List<Location>> GetLocations();
    Task<Location> Create(Location location);
    Task Delete(int id);
    Task Update(Location location);
}