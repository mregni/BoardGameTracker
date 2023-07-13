using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.ViewModels;

namespace BoardGameTracker.Core.Players.Interfaces;

public interface IPlayerService
{
    Task<List<Player>> GetPlayers();
    Task CreatePlayer(Player player);
    Task<Player?> GetPlayer(int id);
    Task Delete(int id);
}