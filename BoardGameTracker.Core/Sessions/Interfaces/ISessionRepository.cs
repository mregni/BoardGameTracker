using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Sessions.Interfaces;

public interface ISessionRepository: IRepository<Session>
{
    Task<double> GetTotalPlayTime(CancellationToken cancellationToken = default);
    Task<double> GetMeanPlayTime(CancellationToken cancellationToken = default);
    Task<Dictionary<int, List<Session>>> GetByPlayerBatchAsync(IEnumerable<int> playerIds);
    Task<List<Session>> GetRecentSessions(int count, CancellationToken cancellationToken = default);
    Task<List<DateTime>> GetSessionStartTimes(CancellationToken cancellationToken = default);
    Task DeleteByPlayerIdAsync(int playerId);
}