using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Locations.Interfaces;

public interface ILocationRepository : IRepository<Location>
{
    Task<int> GetDistinctGameCount(int id);
    Task<int> GetDistinctPlayerCount(int id);
    Task<double> GetPlayLengthInMinutes(int id);
}
