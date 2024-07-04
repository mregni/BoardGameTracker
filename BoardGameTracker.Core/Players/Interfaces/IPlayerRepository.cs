using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerRepository
{
    Task<List<Player>> GetList();
    Task<Player> Create(Player player);
    Task<Player?> GetById(int id);
    Task DeletePlayer(Player player);
    Task<int> GetPlayCount(int id);
    Task<int> GetBestGameId(int id);
    Task<int> GetTotalWinCount(int id);
    Task<double> GetPlayLengthInMinutes(int id);
    Task<List<Play>> GetPlaysForPlayer(int id, int skip, int? take);
    Task<Player> Update(Player player);
    Task<int> GetDistinctGameCount(int id);
    Task<int> CountAsync();
    Task<int> GetTotalPlayCount(int id);
    Task<int> GetWinCount(int id, int gameId);
}