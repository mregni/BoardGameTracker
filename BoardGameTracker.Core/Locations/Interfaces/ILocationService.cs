using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Locations.Interfaces;

public interface ILocationService
{
    public Task<List<Location>> GetLocations();
    Task<Location?> GetByIdAsync(int id);
    Task<Location> Create(CreateLocationCommand command);
    Task Delete(int id);
    Task<Location> Update(UpdateLocationCommand command);
    Task<int> CountAsync();
}