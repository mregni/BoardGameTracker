using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerRepository
{
    Task<List<Player>> GetPlayers();
    Task CreatePlayer(Player player);
    Task<Player?> GetPlayerById(int id);
    Task DeletePlayer(Player player);
    Task<int> GetPlayCount(int id);
    Task<int> GetBestGameId(int id);
    Task<string?> GetFavoriteColor(int id);
    Task<int> GetTotalWinCount(int id);
    Task<double> GetPlayLengthInMinutes(int id);
    Task<List<Play>> GetPlaysForPlayer(int id);
}