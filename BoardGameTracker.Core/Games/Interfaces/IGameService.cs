using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;
using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IGameService
{
    Task<Game> SearchOnBgg(BggGame rawGame, BggSearch search);
    Task<Game?> GetGameByBggId(int bggId);
    Task<List<Game>> GetGames();
    Task<Game?> GetGameById(int id);
    Task Delete(int id);
    Task<int> CountAsync();
    Task<BggGame?> SearchGame(int searchBggId);
    Task<BggLink[]> SearchExpansionsForGame(int id);
    Task<List<TopPlayerDto>> GetTopPlayers(int id);
    Task<Dictionary<SessionFlag, int?>> GetPlayFlags(int id);
    Task<int> GetTotalPlayCount(int id);
    Task<IEnumerable<PlayByDay>> GetPlayByDayChart(int id);
    Task<IEnumerable<PlayerCount>> GetPlayerCountChart(int id);
    Task<Dictionary<DateTime, XValue[]>?> GetPlayerScoringChart(int id);
    Task<List<ScoreRank>> GetScoringRankedChart(int id);
    Task<Game> CreateGame(Game game);
    Task<Game> CreateGameFromCommand(CreateGameCommand command);
    Task<List<Session>> GetSessionsForGame(int id, int? count);
    Task<Game> UpdateGame(Game game);
    Task<List<Expansion>> UpdateGameExpansions(int gameId, int[] expansionIds);
    Task<List<Expansion>> GetGameExpansions(List<int> expansionIds);
    Task DeleteExpansion(int gameId, int expansionId);
    Task<BggImportResult?> ImportBggCollection(string userName);
    Task ImportList(IList<ImportGame> games);
}