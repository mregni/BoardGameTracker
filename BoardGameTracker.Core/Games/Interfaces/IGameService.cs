using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameService
{
    Task<List<Game>> GetGames();
    Task<Game?> GetGameById(int id);
    Task Delete(int id);
    Task<int> CountAsync();
    Task<Game> CreateGameFromCommand(CreateGameCommand command);
    Task<List<Session>> GetSessionsForGame(int id, int? count);
    Task<Game> UpdateGame(UpdateGameCommand command);
    Task<ExpansionData[]> SearchExpansionsForGame(int id);
    Task<List<Expansion>> UpdateGameExpansions(int gameId, int[] expansionIds);
    Task<List<Expansion>> GetGameExpansions(List<int> expansionIds);
    Task DeleteExpansion(int gameId, int expansionId);
}
