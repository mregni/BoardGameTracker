using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameService
{
    Task<Game> ProcessBggGameData(BggGame rawGame, GameState gameState);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGames();
    Task<Game?> GetGameById(int id, bool includePlays);
    Task Delete(int id);
}