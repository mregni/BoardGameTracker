using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Locations.Specifications;
using BoardGameTracker.Core.Sessions.Specifications;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Locations;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IReadRepository<Session> _sessionRepository;
    private readonly IReadRepository<GameNight> _gameNightRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LocationService> _logger;

    public LocationService(
        ILocationRepository locationRepository,
        IReadRepository<Session> sessionRepository,
        IReadRepository<GameNight> gameNightRepository,
        IUnitOfWork unitOfWork,
        ILogger<LocationService> logger)
    {
        _locationRepository = locationRepository;
        _sessionRepository = sessionRepository;
        _gameNightRepository = gameNightRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<List<LocationDto>> GetLocations()
    {
        _logger.LogDebug("Fetching all locations");
        return _locationRepository.ListAsync(new LocationsOverviewSpec());
    }

    public Task<Location?> GetByIdAsync(int id)
    {
        _logger.LogDebug("Fetching location {LocationId}", id);
        return _locationRepository.GetByIdAsync(id);
    }

    public Task<LocationDto?> GetDetails(int id)
    {
        _logger.LogDebug("Fetching details for location {LocationId}", id);
        return _locationRepository.FirstOrDefaultAsync(new LocationDetailsSpec(id));
    }

    public Task<bool> ExistsAsync(int id)
    {
        return _locationRepository.AnyAsync(new LocationByIdSpec(id));
    }

    public async Task<LocationStatisticsDto> GetStatistics(int id)
    {
        _logger.LogDebug("Calculating statistics for location {LocationId}", id);
        return new LocationStatisticsDto
        {
            SessionCount = await _locationRepository.GetSessionCount(id),
            DistinctGameCount = await _locationRepository.GetDistinctGameCount(id),
            DistinctPlayerCount = await _locationRepository.GetDistinctPlayerCount(id),
            TotalPlayedTime = await _locationRepository.GetPlayLengthInMinutes(id)
        };
    }

    public Task<List<Session>> GetSessions(int id, int? count)
    {
        _logger.LogDebug("Fetching sessions for location {LocationId}", id);
        return _sessionRepository.ListAsync(new SessionsByLocationRecentFirstSpec(id, count));
    }

    public async Task<Location> Create(CreateLocationCommand command)
    {
        _logger.LogDebug("Creating location {Name}", command.Name);
        var name = await EnsureUniqueName(command.Name, null);
        var location = new Location(name);
        await _locationRepository.CreateAsync(location);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Location {LocationId} ({Name}) created", location.Id, location.Name);

        return location;
    }

    public async Task Delete(int id)
    {
        _logger.LogDebug("Deleting location {LocationId}", id);
        if (await _gameNightRepository.AnyAsync(new GameNightsAtLocationSpec(id)))
        {
            throw new DomainException(Constants.Errors.LocationUsedByGameNights);
        }

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

        location.UpdateName(await EnsureUniqueName(command.Name, command.Id));
        await _unitOfWork.SaveChangesAsync();

        return location;
    }

    private async Task<string> EnsureUniqueName(string? name, int? excludedId)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new ValidationException(nameof(CreateLocationCommand.Name), Constants.Errors.LocationNameRequired);
        }

        if (await _locationRepository.AnyAsync(new LocationWithNameSpec(trimmed, excludedId)))
        {
            throw new ValidationException(nameof(CreateLocationCommand.Name), Constants.Errors.LocationNameAlreadyExists);
        }

        return trimmed;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return _locationRepository.CountAsync(cancellationToken);
    }
}