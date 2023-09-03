using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Interfaces;

public interface ILocationService
{
    public Task<List<Location>> GetLocations();
}