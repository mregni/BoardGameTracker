using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerRepository
{
    Task<List<Player>> GetList();
    Task Create(Player player);
    Task<Player?> GetById(int id);
    Task DeletePlayer(Player player);
    Task<int> GetPlayCount(int id);
    Task<int> GetBestGameId(int id);
    Task<string?> GetFavoriteColor(int id);
    Task<int> GetTotalWinCount(int id);
    Task<double> GetPlayLengthInMinutes(int id);
    Task<List<Play>> GetPlaysForPlayer(int id);
    Task Update(Player player);
}