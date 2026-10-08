using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameService
{
    Task<List<Game>> GetGames();
    Task<Game?> GetGameById(int id);
    Task<bool> ExistsAsync(int id);
    Task Delete(int id);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<Game> CreateGameFromCommand(CreateGameCommand command);
    Task<List<Session>> GetSessionsForGame(int id, int? count);
    Task<Game> UpdateGame(UpdateGameCommand command);
    Task<ExpansionData[]> SearchExpansionsForGame(int id);
    Task<List<Expansion>> UpdateGameExpansions(int gameId, int[] expansionIds);
    Task<Expansion> AddManualExpansion(int gameId, string title);
    Task<List<Expansion>> GetGameExpansions(int gameId, List<int> expansionIds);
    Task DeleteExpansion(int gameId, int expansionId);
    Task<GamePriceDto?> GetGamePriceAsync(int gameId, bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<List<GamePriceDto>> GetTrackedPricesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    Task<Game> CreateWatchForGame(int gameId, string url, CancellationToken cancellationToken = default);
}
