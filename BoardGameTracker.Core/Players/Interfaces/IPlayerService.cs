using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerService
{
    Task<List<Player>> GetList();
    Task Create(Player player);
    Task<Player?> Get(int id);
    Task Delete(int id);
    Task<PlayerStatistics> GetStats(int id);
    Task<List<Play>> GetPlays(int id);
    Task Update(Player player);
}