using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerRepository: ICrudHelper<Player>
{
    Task<Game?> GetBestGame(int id);
    Task<List<Game>> GetMostPlayedGames(int playerId, int count);
    Task<double> GetPlayLengthInMinutes(int id);
    Task<int> GetDistinctGameCount(int id);
    Task<int> CountAsync();
    Task<int> GetTotalPlayCount(int id);
    Task<int> GetPlayCount(int playerId, int gameId);
    Task<int> GetWinCount(int id, int gameId);
    Task<int> GetTotalWinCount(int id);
}