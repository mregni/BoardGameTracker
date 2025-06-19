using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Sessions.Interfaces;

public interface ISessionRepository: ICrudHelper<Session>
{
    Task<int> CountAsync();
    Task<double> GetTotalPlayTime();
    Task<double> GetMeanPlayTime();
    Task<int> CountByPlayer(int playerId);
    Task<int> CountByPlayerAndGame(int playerId, int gameId);
    Task<List<Session>> GetByPlayer(int playerId, bool? won = null);
    Task<List<Session>> GetByPlayerAndGame(int playerId, int gameId);
}