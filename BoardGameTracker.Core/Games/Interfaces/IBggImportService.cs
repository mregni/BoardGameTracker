using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Common.Models.Bgg;

namespace BoardGameTracker.Core.Games.Interfaces;

public interface IBggImportService
{
    Task<BggGame?> SearchGame(int searchBggId);
    Task<Game> SearchOnBgg(BggGame rawGame, BggSearch search);
    Task<Game?> GetGameByBggId(int bggId);
    Task<BggImportResult?> ImportBggCollection(string userName);
    Task ImportList(IList<ImportGame> games);
}
