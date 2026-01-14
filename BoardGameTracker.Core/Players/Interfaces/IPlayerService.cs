using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerService
{
    Task<List<Player>> GetList();
    Task<Player> Create(Player player);
    Task<Player?> Get(int id);
    Task Delete(int id);
    Task<PlayerStatistics> GetStats(int id);
    Task<int> GetTotalPlayCount(int id);
    Task<Player?> Update(UpdatePlayerCommand command);
    Task<int> CountAsync();
    Task<List<Session>> GetSessions(int id, int? count);
}