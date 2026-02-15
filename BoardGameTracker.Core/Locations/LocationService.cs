using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Locations;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LocationService> _logger;

    public LocationService(ILocationRepository locationRepository, IUnitOfWork unitOfWork, ILogger<LocationService> logger)
    {
        _locationRepository = locationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<List<Location>> GetLocations()
    {
        _logger.LogDebug("Fetching all locations");
        return _locationRepository.GetAllAsync();
    }

    public Task<Location?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Fetching location {LocationId}", id);
        return _locationRepository.GetByIdAsync(id);
    }

    public async Task<Location> Create(CreateLocationCommand command)
    {
        _logger.LogDebug("Creating location {Name}", command.Name);
        var location = new Location(command.Name);
        await _locationRepository.CreateAsync(location);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Location {LocationId} ({Name}) created", location.Id, location.Name);

        return location;
    }

    public async Task Delete(int id)
    {
        _logger.LogDebug("Deleting location {LocationId}", id);
        await _locationRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Location {LocationId} deleted", id);
    }

    public async Task<Location> Update(UpdateLocationCommand command)
    {
        _logger.LogDebug("Updating location {LocationId}", command.Id);
        var location = await _locationRepository.GetByIdAsync(command.Id);
        if (location == null)
        {
            throw new EntityNotFoundException(nameof(Location), command.Id);
        }

        location.UpdateName(command.Name);
        await _unitOfWork.SaveChangesAsync();

        return location;
    }

    public Task<int> CountAsync()
    {
        return _locationRepository.CountAsync();
    }
}