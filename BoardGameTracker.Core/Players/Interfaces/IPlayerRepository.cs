using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Datastore.Interfaces;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerRepository: ICrudHelper<Player>
{
    Task<int> GetPlayCount(int id);
    Task<Game?> GetBestGame(int id);
    Task<int> GetTotalWinCount(int id);
    Task<double> GetPlayLengthInMinutes(int id);
    Task<int> GetDistinctGameCount(int id);
    Task<int> CountAsync();
    Task<int> GetTotalPlayCount(int id);
    Task<int> GetWinCount(int id, int gameId);
    Task<int> GetWinCount(int id);
}