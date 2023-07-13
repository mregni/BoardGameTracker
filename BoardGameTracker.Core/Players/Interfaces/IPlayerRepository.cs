using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerRepository
{
    Task<List<Player>> GetPlayers();
    Task CreatePlayer(Player player);
    Task<Player?> GetPlayerById(int id);
    Task DeletePlayer(Player player);
}