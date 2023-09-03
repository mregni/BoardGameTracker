using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Interfaces;

public interface ILocationRepository
{
    public Task<List<Location>> GetLocations();
}