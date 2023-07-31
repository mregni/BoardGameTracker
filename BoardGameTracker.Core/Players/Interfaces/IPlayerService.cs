using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerService
{
    Task<List<Player>> GetPlayers();
    Task CreatePlayer(Player player);
    Task<Player?> GetPlayer(int id);
    Task Delete(int id);
}