using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Interfaces;

public interface ILocationService
{
    public Task<List<LocationDto>> GetLocations();
    Task<Location?> GetByIdAsync(int id);
    Task<LocationDto?> GetDetails(int id);
    Task<bool> ExistsAsync(int id);
    Task<LocationStatisticsDto> GetStatistics(int id);
    Task<List<Session>> GetSessions(int id);
    Task<Location> Create(CreateLocationCommand command);
    Task Delete(int id);
    Task<Location> Update(UpdateLocationCommand command);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}