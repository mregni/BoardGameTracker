using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Locations.Interfaces;

public interface ILocationRepository : ICrudHelper<Location>
{
    Task<int> CountAsync();
}