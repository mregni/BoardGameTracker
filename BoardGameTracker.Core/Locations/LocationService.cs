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
        return _locationRepository.GetAllAsync();
    }

    public Task<Location> Create(Location location)
    {
        return _locationRepository.CreateAsync(location);
    }

    public Task Delete(int id)
    {
        return _locationRepository.DeleteAsync(id);
    }

    public Task Update(Location location)
    {
        return _locationRepository.UpdateAsync(location);
    }

    public Task<int> CountAsync()
    {
        return _locationRepository.CountAsync();
    }
}