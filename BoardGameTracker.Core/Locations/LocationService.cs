using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Locations.Interfaces;

namespace BoardGameTracker.Core.Locations;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;

    public LocationService(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public Task<List<Location>> GetLocations()
    {
        return _locationRepository.GetLocations();
    }
}