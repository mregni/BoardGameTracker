using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameSessionRepository
{
    Task<List<Session>> GetSessions(int gameId, int skip, int? take);
    Task<List<Session>> GetSessions(int gameId, int dayCount);
    Task<List<Session>> GetSessionsByGameId(int gameId, int? count);
    Task<List<Session>> GetSessionsByPlayerId(int playerId, int? count);
    Task<int> GetPlayCount(int gameId);
    Task<double> GetTotalPlayedTime(int gameId);
    Task<DateTime?> GetLastPlayedDateTime(int gameId);
    Task<int?> GetShortestPlay(int gameId);
    Task<int?> GetLongestPlay(int gameId);
}
