using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerService
{
    Task<List<Player>> GetPlayers();
    Task CreatePlayer(Player player);
    Task<Player?> GetPlayer(int id);
    Task Delete(int id);
    Task<PlayerStatistics> GetStats(int id);
}