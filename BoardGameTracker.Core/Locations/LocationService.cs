using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;

namespace BoardGameTracker.Core.Locations;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LocationService(ILocationRepository locationRepository, IUnitOfWork unitOfWork)
    {
        _locationRepository = locationRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<List<Location>> GetLocations()
    {
        return _locationRepository.GetAllAsync();
    }

    public async Task<Location> Create(Location location)
    {
        await _locationRepository.CreateAsync(location);
        await _unitOfWork.SaveChangesAsync();
        
        return location;
    }

    public async Task Delete(int id)
    {
        await _locationRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task Update(Location location)
    {
        await _locationRepository.UpdateAsync(location);
        await _unitOfWork.SaveChangesAsync();
    }

    public Task<int> CountAsync()
    {
        return _locationRepository.CountAsync();
    }
}